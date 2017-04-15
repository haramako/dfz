# coding: utf-8

# coding: utf-8

# ほげ

require 'active_support/inflector'
require 'pathname'

$LOAD_PATH << __dir__
require 'tmx'
begin
  require 'autogen/master_pb'
  require 'augogen/game_pb'
rescue LoadError
  nil
end

#===================================================
# 定数
#===================================================

PROJECT_ROOT = Pathname.new('..')
DATA_DIR = Pathname.new('.')
OUTPUT = DATA_DIR + 'Output'
TEMP = DATA_DIR + 'Temp'
PROTO_DIR = PROJECT_ROOT + 'Tools/Protocols'

if RUBY_PLATFORM =~ /darwin/
  CFSCTL = '../Tools/Bin/cfsctl'
  PROTOC = 'protoc'
else
  CFSCTL = '../Tools/Bin/cfsctl.exe'
  PROTOC = '../Tools/Bin/protoc.exe' # './Tools/Bin/protoc'
end

mkdir_p [OUTPUT, TEMP], verbose: false

#===================================================
# 便利関数
#===================================================

# String#pathmap にもとづいて、タスクを作成して、出力ファイルのリストを取得する
#
# 使用例:
# OUT = map_task(['a.txt', 'b.txt'], 'out/%n.bin') do |t|
#   puts t.source
# end
# OUT # => ['out/a.bin', 'out/b.bin']
def pathmap_task(filelist, pathmap_pattern)
  raise "pathmap_pattern must be String but #{pathmap_pattern.class}" unless pathmap_pattern.is_a? String
  filelist.map do |src|
    out = src.pathmap(pathmap_pattern)
    file out => src do |t|
      yield t
    end
    out
  end
end

# rubocop:disable Style/GlobalVars
def logger
  unless $_logger
    $_logger = Logger.new(STDOUT)
    $_logger.level = Logger::INFO
  end
  $_logger
end
# rubocop:enable Style/GlobalVars

# Inflectorに単語登録
ActiveSupport::Inflector.inflections do |inf|
  inf.singular 'bonus', 'bonus'
  inf.singular 'data', 'data'
end

#===================================================
# タスクリスト
#===================================================

task :default => [:map, :master]

task :clean do
  rm_rf [OUTPUT, TEMP]
end

desc 'protoファイルからプロトコルを作成する'
task :proto do
  mkdir_p __dir__ + "/autogen"
  
  sh PROTOC,
     "--ruby_out=#{PROJECT_ROOT}/Tools/Converter/autogen",
     "--proto_path=#{PROTO_DIR}",
     *FileList[PROTO_DIR + '*.proto']

  sh PROTOC,
     "--plugin=#{PROJECT_ROOT}/Tools/Bin/protoc-gen-dfcsharp",
     "--dfcsharp_out=#{PROJECT_ROOT}/Client/Assets/Scripts/AutoGenerated/",
     "--proto_path=#{PROTO_DIR}",
     *FileList[PROTO_DIR + '*.proto']

  begin
    mkdir_p PROJECT_ROOT + 'Doc/html'
    chdir PROTO_DIR do
      sh 'protoc',
         "--plugin=../Bin/protoc-gen-doc",
         "--doc_out=html,Proto.html:../../Doc/Html",
         "--proto_path=.",
         'master.proto', 'game.proto', 'game_log.proto'
    end
  rescue
    puts "WARN: cannot find protoc and protoc-doc-gen, please install 'protoc', 'protoc-doc-gen'"
  end
end

OUT_MAPS = pathmap_task(FileList[DATA_DIR.to_s + '/**/*.tmx'], OUTPUT.to_s + '/%n-Stage.pb') do |t|
  logger.info "マスターコンバート中 #{t.source}"
  map = Tmx.new(t.source)
  IO.binwrite(t.name, map.dump_pb)
end

desc 'マップファイルの変換'
task :map => OUT_MAPS

OUT_EXLS = pathmap_task(FileList[DATA_DIR + 'Master/**/*.xls'], OUTPUT.to_s + '/%n.touch') do |t|
  logger.info "Converting #{t.source}"
  IO.write(t.name, t.source)

  book = PbConvert.excel_cache(t.source)
  book.worksheets.each do |sheet|
    model_class = Master.const_get(sheet.name)
    next unless model_class
    logger.info "sheet = #{sheet.name}"
    pb_name = t.name.gsub('.touch', '_' + sheet.name + '.pb')
    PbConvert.conv_master(pb_name, t.source, sheet.name, model_class)
  end
end

desc 'マスターファイルの変換'
task :master => OUT_EXLS

desc 'アップロード'
task :upload do
  require 'net/http'

  sh "#{CFSCTL} upload -t tb-dev -b client.bucket Output"
  client_hash = IO.read('client.bucket.hash')

  host = 'http://133.242.235.150:7000'
  tag = 'tb-dev'
  res = Net::HTTP.post_form(URI.parse("#{host}/tags/#{tag}"),
                            'val' => client_hash)
  raise "upload failed! #{res}" unless res.is_a? Net::HTTPSuccess
end

desc '.pbファイルのYAML化'
task :to_yaml do
  require 'yaml'
  mkdir_p TEMP + 'ChunkDump'
  FileList[OUTPUT.to_s + '/*.pb'].each do |f|
    logger.info "YAMLへコンバート中 #{File.basename(f)}"
    yml = PbConvert.parse_pb(IO.binread(f)).map do |item|
      if item.is_a? Array
        if item[1].is_a? String
          data = item
        else
          data = JSON.parse(item[1].to_json)
        end
      else
        data = JSON.parse(item.to_json)
      end
      YAML.dump(data)
    end.join("\n")
    IO.binwrite(TEMP + 'ChunkDump' + "#{File.basename(f)}.yml", yml)
  end
end

#===================================================
# その他、こまごまとしたタスク
#===================================================

desc 'rspecテストを行う'
task :spec do
  chdir __dir__ do
    sh 'rspec', '-I', '.', 'spec'
  end
end

desc 'racc'
task :racc do
  chdir __dir__ do
    sh 'racc', '-O', 'racc.output', 'short_json.y'
  end
end

desc 'プログラム開発用の全てをやり直すタスク'
task :dev do
  sh 'rake', 'racc', 'proto', 'clean'
  sh 'rake', 'default', 'upload', 'to_yaml'
end
