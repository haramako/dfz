# coding: utf-8

require 'google/protobuf'
require 'excel'
require 'stringio'
require 'active_support/inflector'
require 'zlib'
Dir[File.expand_path("../model/", __FILE__) << '/*.rb'].each { |file| require file }

# Protobuf へのモンキーパッチ
module Google::Protobuf
  class FieldDescriptor
    def map_entry?
      label == :repeated and type == :message and subtype.name.include? '_MapEntry_'
    end
  end
end

class String
  def self.encode(x)
    x
  end

  def self.decode(x)
    x
  end
end
module PbConvert
  module_function

  def clear_excel_cache
    @excel_cache = {}
  end

  def excel_cache(path)
    @excel_cache = {} unless @excel_cache
    unless @excel_cache[path]
      logger.debug "エクセルファイル読み込み #{path}"
      @excel_cache[path] = Spreadsheet.open(StringIO.new(IO.binread(path)))
    end
    @excel_cache[path]
  end

  def parse_pb(bin)
    if bin[0, 20] =~ /PBX/
      unpack_pbx(bin)
    else
      unpack_pb_list(bin)
    end
  rescue
    puts $ERROR_INFO
    []
  end

  def pack_pbx(dict)
    type_name = (dict.values[0].class.descriptor.name.encode('ASCII') rescue 'String')
    pos = 0
    index = Master::PbxHeader.new

    list_bin = dict.map do |kv|
      if kv[0].is_a? Numeric
        index.int_index[kv[0]] = pos
      else
        index.string_index[kv[0].to_s] = pos
      end
      bin = Zlib::Deflate.deflate(kv[1].class.encode(kv[1]))
      pos += 4 + bin.size
      bin
    end

    index_bin = Master::PbxHeader.encode(index)
    pack_chunk_list("PBX/#{type_name}", [index_bin] + list_bin)
  end

  def unpack_pbx(bin)
    header, list = unpack_chunk_list(bin)
    mo = header.match(%r{^PBX/(.*)$})
    raise "invalid pbx header '#{header}'" unless mo

    # rubocop:disable Security/Eval
    type = eval(mo[1].gsub(/\./) { '::' })
    # rubocop:enable Security/Eval

    index_bin = list.shift
    index = Master::PbxHeader.decode(index_bin)
    header_index_size = 1 + 1 + header.size + 4 + index_bin.size

    r = {}
    (index.int_index.to_a + index.string_index.to_a).each do |kv|
      pos = header_index_size + kv[1]
      len = bin[pos, 4].unpack('L')[0]
      obj = type.decode(Zlib::Inflate.inflate(bin[pos + 4, len]))
      r[kv[0]] = obj
    end
    r
  end

  def pack_pb_list(list)
    type_name = list[0].class.descriptor.name.encode('ASCII')
    pack_chunk_list(type_name, list.map { |x| x.class.encode(x) })
  end

  def unpack_pb_list(s)
    type_name, list = unpack_chunk_list(s)
    # rubocop:disable Security/Eval
    type = eval(type_name.gsub(/\./) { '::' })
    # rubocop:enable Security/Eval
    list.map { |bin| type.decode(bin) }
  end

  # チャンクリストをパックする
  def pack_chunk_list(header, list)
    s = StringIO.new

    s.write ['C', header.size, header].pack('aCa*')

    list.each do |x|
      s.write [x.size, x].pack('La*')
    end

    s.string
  end

  # チャンクリストをアンパックする
  def unpack_chunk_list(s)
    s = StringIO.new(s) if s.is_a? String

    magic, header_size = s.read(2).unpack('aC')
    raise "not a chunk list" if magic != 'C'
    header = s.read(header_size)

    list = []
    until s.eof?
      size = s.read(4).unpack('L')[0]
      list << s.read(size)
    end

    [header, list]
  end

  def conv_sheet(src, sheet, item_type)
    data = Excel.read_from_file(src, excel_cache(src), sheet)
    items = data.map do |row|
      PbConvert.conv_message(item_type, row)
    end
    items
  end

  # マスターデータを.pbファイルにコンバートする
  def conv_master(dest, src, sheet, item_type)
    items = conv_sheet(src, sheet, item_type)
    unless items.empty?
      bin = pack_pb_list(items)
      IO.binwrite(dest, bin)
    end
    nil
  end

  def conv_message(_class, data)
    m = _class.new

    data.each do |k, v|
      begin
        k, v = conv_field_name(_class, k, v)
        k = k.to_s

        desc = _class.descriptor.lookup(k)
        if desc
          conv_field(m, desc, k, v)
        else
          logger.warn "#{_class.name} に #{k} が存在しません"
        end
      rescue
        logger.warn "コンバートできません #{_class}.#{k} = #{v}"
        logger.warn data
        raise
      end
    end
    m
  end

  def conv_field(m, desc, k, v)
    if desc.map_entry?
      f = m.__send__(k)
      v.each do |k2, v2|
        k2 = conv_type(desc.subtype.lookup('key'), k2)
        v2 = conv_type(desc.subtype.lookup('value'), v2)
        f[k2] = v2
      end
    elsif desc.label == :repeated
      f = m.__send__(k)
      v.each { |v2| f << conv_type(desc, v2) }
    else
      desc.set m, conv_type(desc, v)
    end
  end

  def conv_type(desc, v)
    case desc.type
    when :enum
      v.classify.to_sym
    when :bool
      v
    when :string
      if v.is_a? Symbol
        v.to_s
      else
        v
      end
    when :message
      conv_message(desc.subtype.msgclass, v)
    else
      v
    end
  end

  def conv_field_name(_class, k, v)
    [k, v]
  end

  # pbのデータの概要をレポートする
  def report_chunk_lists(files)
    puts '-' * 80
    puts('%-36s %-20s %4s %8s %8s        %6s' % ['filename', 'sheet', 'ext', 'size', 'gz-size', 'items'])
    puts '-' * 80
    total_size = 0
    total_gz_size = 0
    files.each do |f|
      data = IO.binread(f)
      gz_size = Zlib::Deflate.deflate(data).size
      size = data.size
      comp = 100 * gz_size / size
      begin
        _, list = PbConvert.unpack_chunk_list(data)
      rescue
        list = []
      end
      fname = File.basename(f).gsub(/.pbx?$/, '').split(/-/)
      puts('%-36s %-20s %-4s %8d %8d (%3d%%) %6d' % [fname[0], fname[1], File.extname(f), size, gz_size, comp, list.size])
      total_size += size
      total_gz_size += gz_size
    end
    puts '-' * 80
    puts "トータルサイズ    : #{total_size}"
    puts "圧縮後サイズ      : #{total_gz_size}"
    puts "圧縮率            : #{100 * total_gz_size / total_size}%"
  end
end
# rubocop:enable Metrics/ModuleLength
