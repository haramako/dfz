# -*- coding: utf-8 -*-

require 'spreadsheet'

module Excel
  
  # エクセルデータをHashの配列に変換する.
  #
  # ----test.xls----
  # $KEY id, name, age
  # $TYPE int, string, int
  # # ID, 名前, 年齢
  # 1, kikuko, 17
  # 2, shokotan, 29
  # ----------------
  # Excel.read_from_file('test.xls')
  # # => [{ id:"1", name:"kikuko", age:"17" }, { id:"2", name:"shokotan", age:"29" }]
  # 
  # '$KEY'で始まる行は、Hashのキーとして使われる
  # '$TYPE' で始まる行は、
  # '#'で始まる行は、コメントとして無視される
  #
  def self.read_from_file(path, book, sheet_name_or_index=0)
    sheet = book.worksheet sheet_name_or_index
    raise "sheet '#{sheet_name_or_index}' not found" unless sheet
    header = nil
    types = nil
    data = []
    row_num = 0
    sheet.each do |row|
      row_num += 1
      next unless row[0]
      if row[0].is_a? String
        case row[0]
        when /^\$KEY/i
          row[0] = row[0][4..-1]
          header = row.delete_if(&:nil?).map{|col| col.strip.to_sym }
          next
        when /^\$TYPE/i
          row[0] = row[0][5..-1]
          types = row.delete_if(&:nil?).map{|col| col.strip.to_sym }
          next
        when /^#/
          # comment
          next
        end
      end
      
      raise "no $KEY header defined in #{path} : #{sheet_name_or_index}" unless header
      row_hash = {}
      header.zip(row,types) do |h,col,type|
        begin
          if Spreadsheet::Formula === col
            col = col.value
          end
          
          case type
          when :string
            if Float === col
              v = col.to_i.to_s # たまに文字列がFloatになって "99" => "99.0" のように変わってしまうのの対策
            else
              v = (''+col.to_s).strip
            end
          when :"string[]"
            v = (''+col.to_s).strip.split(/,/).map{|x| x.strip}
          when :int
            v = col.to_i
          when :float
            v = col.to_f
          when :symbol
            v = col && (''+col.to_s).strip.to_sym
          when :json
            v = col && JSON.parse(col.to_s, symbolize_names: true)
          when :bool
            v = col.to_s.upcase == 'TRUE'
          when :option
            if col
              row_hash.merge! JSON.parse(col.to_s, symbolize_names: true)
            end
          when :none, :''
            v = nil
          else
            raise "unkonwn type #{type}"
          end
          
          if h.to_s.end_with? '[]'
            h = h.to_s[0..-3].to_sym
            row_hash[h] ||= []
            row_hash[h] << v
          else
            row_hash[h] = v
          end
        rescue
          raise "エラー: #{$!}, ファイル名=#{File.basename(path)} シート=#{sheet_name_or_index} 行番号=#{row_num}, カラム名=#{h}, 内容=#{col}"
        end
      end
      data << row_hash
    end
    data
  end
  # エクセルデータをJSONの配列に変換する.
  #
  # ----test.xls----
  # $KEY id, name, age
  # $TYPE int, string, int
  # # ID, 名前, 年齢
  # 1, kikuko, 17
  # 2, shokotan, 29
  # ----------------
  # Excel.read_from_file('test.xls')
  # # => [{ id:"1", name:"kikuko", age:"17" }, { id:"2", name:"shokotan", age:"29" }]
  # 
  # '$KEY'で始まる行は、Hashのキーとして使われる
  # '$TYPE' で始まる行は、
  # '#'で始まる行は、コメントとして無視される
  #
  def self.read_from_file_simple(book, sheet_name_or_index=0)
    sheet = book.worksheet sheet_name_or_index
    raise "sheet '#{sheet_name_or_index}' not found" unless sheet
    header = nil
    data = []
    sheet.each do |row|
      next unless row[0]
      unless header
        # header
        header = row.map{|col| col.strip.to_sym }
      else
        # data
        next if row[0].to_s[0] == '#'
        row_hash = {}
        header.zip(row).each do |h,col|
          begin
            row_hash[h] = col.to_s
          rescue
            raise "#{$!} in h=#{h}, col=#{col}"
          end
        end
        data << row_hash
      end
    end
    data
  end

end
