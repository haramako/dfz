#!/bin/env rake
# coding: utf-8

require 'find'
require 'pp'
require 'json'
require 'logger'
require 'zlib'
require 'active_support/inflector'
require 'pry'
require 'net/http'
begin
  require 'master'
  require 'townpage'
rescue LoadError
  puts $!
end
require 'pb_convert'

def logger
  unless $_logger
    $_logger = Logger.new(STDOUT)
    $_logger.level = Logger::DEBUG if ENV['DEBUG']
    $_logger.formatter = proc { |severity, datetime, progname, msg|
      "#{'%-5s'%[severity]}: #{datetime.strftime('%H:%m:%S')} #{msg}\n"
    }
  end
  $_logger
end

if RUBY_PLATFORM.match /darwin/
  CFSCTL = './Tools/Bin/cfsctl'
  PROTOC= 'protoc'
else
  CFSCTL = './Tools/Bin/cfsctl.exe'
  PROTOC= './Tools/Bin/protoc.exe' # './Tools/Bin/protoc'
end

TMP_DIR = './Temp'
EXCEL_DIR = './Assets/DataExcel'
STAGE_DIR = './Assets/DataStage'
LUA_DIR = './Assets/DataLua'
SOUND_DIR = './Assets/DataSound'
STORY_DIR = 'Assets/DataStory'
OUT_DIR = './UploadFiles'
LIB = './Tools/Uploader/lib'
CONVERTER_EXE = './Tools/Bin/Converter.exe'

VERSION = 1

####################################################################
# 依存関係の更新
####################################################################

# Inflectorに単語登録
ActiveSupport::Inflector.inflections do |inf|
  inf.singular 'bonus', 'bonus'
  inf.singular 'data', 'data'
end

# モデル名のリスト
# master.proto の message <この部分> をモデル名として取得 ( ただし、入れ子になっているものは無視する )
# StackItemTemplate ->  stack_item_template に変換される
Encoding.default_external = 'utf-8'
models = []
File.foreach("#{LIB}/master.proto") do |line|
  if line =~ /^message\s(\w*)\s/
    model = $1
    if model =~ /[a-z][A-Z]/
      models << model.gsub!(/(?<s1>[a-z])(?<s2>[A-Z])/, '\k<s1>_\k<s2>')
    else
      models << model
    end
  end
end
MODEL_NAMES = models.compact.each(&:downcase!)
# モデル名からシート名への変換
MODEL_NAME_TO_SHEET_NAME = {
  'stack_item_template'=>'stack_item',
  'fang_template'=>'fang',
  'quest_template'=>'quest',
  'user_stat_template'=>'user_stat',
  'special_cutin_option'=>'special_cutin',
  'enemy'=>'Enemy',
  'item'=>'Item',
  'trap'=>'Trap',
  'solo_duel'=>'SoloDuel',
  'area_data'=>'area',
  'dungeon_data'=>'dungeon',
  'stage_data'=>'stage',
  'room_data'=>'room',
  'sound_list'=>'SoundList',
}

def make_depends
  depends = {xls_pb: [], map_pbx: [], xls_pbx: [], townpage_pbx: []}
  logger.info 'make depends...'

  xls_files = Dir.glob("#{EXCEL_DIR}/master*.xls").to_a +
              Dir.glob("#{EXCEL_DIR}/Enemys*.xls").to_a +
              Dir.glob("#{EXCEL_DIR}/SoundList*.xls").to_a +
              Dir.glob("#{EXCEL_DIR}/Items.xls").to_a +
              Dir.glob("#{EXCEL_DIR}/Traps.xls").to_a +
              Dir.glob("#{EXCEL_DIR}/EnemyAnimation.xls").to_a +
              Dir.glob("#{EXCEL_DIR}/SoloDuel_stage.xls").to_a +
              Dir.glob("#{EXCEL_DIR}/Arms.xls").to_a +
              Dir.glob("#{EXCEL_DIR}/GoalSetData.xls").to_a +
              Dir.glob("#{STAGE_DIR}/StageTable*.xls").to_a
  xls_files.each do |xls|
    book = PbConvert.excel_cache(xls)
    sheets = book.worksheets.map{|s| s.name }

    MODEL_NAMES.each do |model_name|
      model_sheet = MODEL_NAME_TO_SHEET_NAME[model_name] || model_name
      if sheets.include? model_sheet
        if ['stage_data', 'enemy'].include? model_name
          depends[:xls_pbx] << ["#{OUT_DIR}/#{File.basename(xls,'.xls')}-#{model_name}.pbx", xls, model_name]
        else
          depends[:xls_pb] << ["#{OUT_DIR}/#{File.basename(xls,'.xls')}-#{model_name}.pb", xls, model_name]
        end
      end
    end
  end

  depends[:map_pbx] = Hash.new{|h,k| h[k] = []}
  Find.find(STAGE_DIR) do |f|
    next unless File.extname(f) == '.tmx'
    dirname = File.basename(File.dirname(f))
    next if dirname == 'animation' or dirname == 'mapchip' or File.basename(f) == 'test_rule.tmx'
    fname = File.basename(f).gsub(/\.tmx/, '')
    depends[:map_pbx]["#{OUT_DIR}/map-#{fname}.pbx"] << f
  end

  depends[:townpage_pbx] = []
  Find.find(EXCEL_DIR) do |f|
    next unless File.basename(f).match /^Townpage.+\.xls/
    depends[:townpage_pbx] << [f, "#{OUT_DIR}/#{File.basename(f,'.xls')}-townpage.pbx"]
  end
=begin
  depends[:lua] = []
  Find.find(LUA_DIR) do |f|
    next unless File.basename(f).match /^(.+)\.lua$/
    depends[:lua] << [f, "#{OUT_DIR}/#{File.basename(f)}.txt"]
  end
=end
  IO.binwrite('.depends', JSON.dump(depends))
end

make_depends unless File.exists?('.depends') # 依存関係ファイルを作成する
depends = {xls_pb: [], map_pbx: [], xls_pbx: [], townpage_pbx: [], lua: []}
begin
  depends = depends.merge(JSON.parse(IO.read('.depends'), symbolize_names: true))
rescue
  # DO NOTHING
end

####################################################################
# master*.xlsの処理
####################################################################
PB_FILES = []

depends[:xls_pb].each do |pb, xls, model_name|
  PB_FILES << pb
  file pb => xls do |s|
    logger.info "Creating #{pb}"
    model_sheet = MODEL_NAME_TO_SHEET_NAME[model_name] || model_name
    model_class = Master.const_get(model_name.classify)
    PbConvert.conv_master(s.name, s.source, model_sheet, model_class)
  end
end

depends[:xls_pbx].each do |pbx, xls, model_name|
  PB_FILES << pbx
  file pbx => xls do |s|
    logger.info "Creating #{pbx}"
    model_sheet = MODEL_NAME_TO_SHEET_NAME[model_name] || model_name
    model_class = Master.const_get(model_name.classify)
    items = PbConvert.conv_sheet(s.source, model_sheet, model_class)
    if items.size > 0
      dict = Hash[items.map{|i| [i.id, i]}]
      bin = PbConvert.pack_pbx(dict)
      IO.binwrite(s.name, bin)
    end
  end
end

# StringData*.xlsのファイルの比較をする
task :compare_string_data do
  data = []
  ["#{EXCEL_DIR}/ExStringDatas.xls", "../df5/Assets/StringManager/XLS/StringDatas.xls"].each.with_index do |f,i|
    fdata = {}
    %w(StringData Tips System S Start SO Fairy Tutorial).each do |sheet|
      Excel.read_from_file(f, PbConvert.excel_cache(f), sheet).each do |row|
        fdata[sheet + ':' + row[:id]] = row[:message]
      end
    end
    data[i] = fdata
  end
  keys = data.map { |d| d.keys.sort }
  (keys[1] - keys[0]).each do |k|
    puts "#{k} #{data[1][k]}"
  end
end

Dir.glob("#{EXCEL_DIR}/StringDatas*.xls").each do |f|
  outfile = "#{OUT_DIR}/#{File.basename(f,'.xls')}-string_data.pb"
  PB_FILES << outfile
  file outfile => f do |s|
    logger.info "#{File.basename(s.name)}をコンバート"
    items = []
    %w(StringData Tips System S Start SO Fairy Tutorial).each do |sheet|
      sheet_id = if sheet == 'StringData' then '' else sheet + ':' end
      begin
        items.concat PbConvert.conv_sheet(s.source, sheet, Master::StringData).map{|sd| sd.id = sheet_id+ sd.id; sd }
      rescue
        ## DO NOTHING
      end
    end

    IO.binwrite(s.name, PbConvert.pack_pb_list(items))
  end
end

####################################################################
# マップデータ
####################################################################

def run_converter(*args)
  if RUBY_PLATFORM.match(/darwin/)
    sh( *(['mono', CONVERTER_EXE]+args))
  else
    sh( *([CONVERTER_EXE]+args))
  end
end

task :copy_converter do
  cp './Tools/Converter/Converter/bin/Release/Converter.exe', CONVERTER_EXE
end

task :tmx do
  run_converter 'tmx', './Assets/DataStage/TMX'
end

# マップのpbxを作成する
depends[:map_pbx].each do |k,v|
  file k.to_s => v do |s|
    logger.info "#{s.name} を作成しています"
    map_dict = {}
    s.sources.map do |f|
      dirname = File.basename(File.dirname(f))
      basename = File.basename(f,'.tmx')
      bin = IO.binread("#{TMP_DIR}/Tmx/#{dirname}/#{basename}.map")
      map_dict[basename] = bin
    end
    pbx_bin = PbConvert.pack_pbx(map_dict)
    IO.binwrite s.name, pbx_bin
  end
end

task :map_pbx => depends[:map_pbx].keys.map(&:to_s)

depends[:townpage_pbx].each do |townpage, pbx|
  file pbx => townpage do |s|
    logger.info "#{s.name} を作成しています"
    tp = Townpage.parse(s.source)
    pbx_bin = PbConvert.pack_pbx(tp)
    IO.binwrite s.name, pbx_bin
  end
end

task :townpage_pbx => depends[:townpage_pbx].map{|x| x[1]}

# アニメーションファイルを作成する
MAP_ANIM_PB = "#{OUT_DIR}/map_animation.pb"
file MAP_ANIM_PB => [:tmx] do
  logger.info "Creating #{MAP_ANIM_PB}"
  list = []
  Find.find("#{TMP_DIR}/Anim") do |f|
    next unless /\.anim$/ === f
    data = JSON.parse(IO.read(f), symbolize_names: true)
    data.map do |k,v|
      anim_list = v.map do |anim|
        [anim[0], Master::MapAnimation::Anim.new(items: anim[1..-1])]
      end
      data[k] = Hash[anim_list]
    end
    data[:id] = File.basename(f,'.anim')
    list << Master::MapAnimation.new(data)
  end
  IO.binwrite(MAP_ANIM_PB, PbConvert.pack_pb_list(list))
end

# アニメーションファイルを作成する
ROOM_PB = "#{OUT_DIR}/room-room.pb"
file ROOM_PB => :tmx do
  logger.debug "Creating #{ROOM_PB}"
  list = []
  Find.find("#{TMP_DIR}/Tmx") do |f|
    next unless /\.index$/ === f
    d = IO.read(f).split(/\t/).map{|s| s.to_i}
    list << Master::RoomInfo.new(id: File.basename(f,'.index'), width: d[0], height: d[1], attribute: d[2], direction: d[3])
  end
  IO.binwrite(ROOM_PB, PbConvert.pack_pb_list(list))
end

task :map => [:tmx, MAP_ANIM_PB, ROOM_PB, :map_pbx, :townpage_pbx]

####################################################################
# lua
####################################################################

desc 'luaファイルを変換'
task :lua => depends[:lua].map{|x| x[1] }

depends[:lua].each do |x|
  file x[1] => x[0] do |s|
    logger.info "#{s.name} を作成しています"
    txt = IO.binread(x[0]).gsub(/\r\n/,"\n")
    IO.binwrite(s.name, txt)
  end
end


####################################################################
# storydat
####################################################################

desc 'storydatファイルを追加'
task :storydat do
	story_dat = Dir.glob("#{STORY_DIR}/*")
	FileUtils.cp(story_dat, OUT_DIR)
end

####################################################################
# その他
####################################################################

# Rakeを再起動しない場合に問題になるキャッシュの削除など
task :reset do
  PbConvert.clear_excel_cache
end

task :make_test_pb do
  data  =[Master::StringData.new(id: 'h', text: 'hoge'),
          Master::StringData.new(id: 'f', text: 'fuga')]
  puts '// ' + JSON.dump(data.map{|x| x.to_hash })
  p PbConvert.pack_pb_list(data).each_byte.to_a

  data  = {
    1 => Master::StringData.new(id: 'h', text: 'hoge'),
    2 => Master::StringData.new(id: 'f', text: 'fuga'),
    'x' => Master::StringData.new(id: 'f', text: 'hage'),
  }
  puts '// ' + JSON.dump(data.map{|k,v| [k,v.to_hash] })
  bin = PbConvert.pack_pbx(data)
  pp bin.each_byte.to_a.each_slice(10).to_a

  pp PbConvert.unpack_pbx(bin)

end

desc '.pbファイルのレポート'
task :report do
  files = []
  Find.find(OUT_DIR) do |f|
    next unless f.match /\.pb(x)?$/
    files << f
  end
  PbConvert.report_chunk_lists(files)

  puts '-' * 80
  puts "%-40s  %8s %8s"%['filename', 'size', 'gz-size']
  puts '-' * 80
  total_size = 0
  total_gz_size = 0
  Find.find(SOUND_DIR) do |f|
    next unless f.match /\.wav$/
    data = IO.read(f)
    size = data.size
    gz_size = Zlib::Deflate.deflate(data).size
    gz_rate = 100 * gz_size / size
    total_size += size
    total_gz_size += gz_size
    puts "%-40s  %8d %8d (%3d%%)"%[File.basename(f), size, gz_size, gz_rate]
  end
  puts '-' * 80
  puts "トータルサイズ: #{total_size}"
  puts "圧縮サイズ    : #{total_gz_size}"
  puts "圧縮率        : #{100 * total_gz_size / total_size}%"
  puts
end

desc '.pbファイルのYAML化'
task :to_yaml do
  require 'yaml'
  mkdir_p "#{TMP_DIR}/ChunkDump/"
  Find.find(OUT_DIR) do |f|
    next unless f.match /\.pb(x?)$/
    logger.info "YAMLへコンバート中 #{File.basename(f)}"
    yml = PbConvert.parse_pb(IO.binread(f)).map do |item|
      if Array === item
        if String === item[1]
          data = item
        else
          data = JSON.parse(item[1].to_json)
        end
      else
        data = JSON.parse(item.to_json)
      end
      YAML.dump(data)
    end.join("\n")
    IO.binwrite("#{TMP_DIR}/ChunkDump/#{File.basename(f)}.yml", yml)
  end
end

####################################################################
# 検証
####################################################################

desc '検証'
task :verify do
  require 'verify'
end

desc 'コンバートして検証'
task :default_and_verify => [:default, :verify]

task :create_validate_list do
  require 'create_validate_list'
end

####################################################################
# 全体タスク
####################################################################

directory OUT_DIR

desc 'マスターデータを作成'
task :master => PB_FILES

file "#{LIB}/master.rb" => "#{LIB}/master.proto" do
  sh "#{PROTOC} --ruby_out=. #{LIB}/master.proto"
  require 'master' rescue nil
end

desc 'ProtocolBufferのコンパイル'
task :proto => "#{LIB}/master.rb"

desc 'ProtocolBufferのC#へのコンパイル'
task :proto_cs do
  sh "#{PROTOC} --plugin=Tools/Bin/protoc-gen-dfcsharp --dfcsharp_out=../df5/Assets/Common #{LIB}/master.proto"
end

desc '作成したファイルをすべてを削除'
task :distclean => :clean do
  rm_rf "#{LIB}/master.rb"
end

desc '作成したファイルを削除'
task :clean do
  rm_rf ['.depends', OUT_DIR, './Temp']
end

desc 'N3DS用のデータ作成'
task :n3ds do
  N3DS_OUT = 'Temp/StreamingAssets'
  # N3DS_OUT'../dfd5/Assets/StreamingAssets'
  # N3DS_OUT = '../dragonfang/Assets/StreamingAssets'
  require 'zip'
  rm_f "n3ds.zip"
  Zip::File.open("n3ds.zip", Zip::File::CREATE) do |zip|
    bucket = []
    ["UploadFiles/*"].each do |pat|
      Dir.glob(pat).each do |f|
        bucket << File.basename(f)
        zip.add("StreamingAssets/#{File.basename(f)}", f)
      end
    end
    Dir.glob("Assets/AssetBundles/N3DS/*.ab").each do |f|
      bucket << "N3DS/#{File.basename(f)}"
      zip.add("StreamingAssets/N3DS/#{File.basename(f)}", f)
    end

    puts "#{bucket.size} files archived to n3ds.zip"
    bucket_file =  bucket.map{|f| [f,f,0,0,0,0,0].join("\t") }.join("\n")
    zip.get_output_stream('StreamingAssets/CfsOffline.txt'){|fs| fs.write bucket_file }
  end
end

task :n3ds_copy => :n3ds do
  output = "../dragonfang/Assets"
  Zip::InputStream.open("n3ds.zip", 0) do |input|
    while (entry = input.get_next_entry)
      save_path = File.join(output, entry.name)
      puts save_path
      mkdir_p File.dirname(save_path), verbose: false
      IO.binwrite( save_path, input.read )
    end
  end  
end

desc 'アップロード'
task :upload do
  sh "#{CFSCTL} upload -b client.bucket UploadFiles Assets/AssetBundles"
  client_hash = IO.read('client.bucket.hash')

  sh "#{CFSCTL} upload -b server.bucket Assets/DataExcel Assets/DataStage"
  server_hash = IO.read('server.bucket.hash')

  if ENV['SITE_URL']
    begin
      site = ENV['SITE_URL']
      param = {cfs_hash_client: client_hash, cfs_hash_server: server_hash}
      res = Net::HTTP.post_form( URI.parse(site) + '/update_cfs', param)
      logger.info "upload to '#{site}', hash=#{client_hash}, sv_hash=#{server_hash}"
      if res.body.include?("error")
        logger.error "response: #{res.body}"
      else
        logger.info "response: #{res.body}"
      end
    rescue Errno::ECONNREFUSED
      logger.error "サーバに接続できません。URL='#{site}'"
    end
  else
    logger.warn 'environment variable "SITE_URL" is not specified! server setting is not updated.'
    logger.warn 'example, $ SITE_URL=http://localhost:3000/ rake upload'
  end

end

desc '依存関係の更新'
task :depend do
  make_depends
end

desc 'すべてビルド'
task :default => [:proto, OUT_DIR, :master, :map, :lua, :storydat]

desc 'すべてビルド&アップロード'
task :default_and_upload => [:reset, :default, :upload]

desc 'サーバーを開始する'
task :start do
  # ちょっと待ってブラウザを起動する
  Thread.start do
    sleep 3
    sh 'open http://localhost:3010'
  end
  sh './WebUi'
end

desc 'ドキュメントを生成する'
task :doc do
  mkdir_p 'Doc/Html'

  begin
    (Dir.glob('Doc/*.md') + Dir.glob('*.md')).each do |f|
      sh "pandoc #{f} -c github.css -o Doc/Html/#{File.basename(f,'.md')+'.html'}"
    end
  rescue
    puts "cannot create html document, please install 'pandoc'"
  end

  begin
    sh "protoc --doc_out=html,Proto.html:Doc/Html Tools/Uploader/lib/master.proto"
  rescue
    puts "cannot find protoc and protoc-doc-gen, please install 'protoc', 'protoc-doc-gen'"
  end

  begin
    tmp=ENV['TMPDIR']+'luadoc/'
    rm_rf tmp
    mkdir_p tmp
    Dir.glob(LUA_DIR+'/*.lua.txt') do |f|
      next if f.match(/Story|boss|dkjson\.lua|pp\.lua|url\.lua|muses\.lua/i)
      cp f, tmp+File.basename(f).gsub(/\.txt$/,'')
    end
    sh "ldoc -p ドラゴンファング -a #{tmp} -d Doc/Html/luadoc -f markdown"
  rescue
    puts "cannot find ldoc, please install 'ldoc'"
  end

  begin
    sh "scp -r Doc/Html elke:/Library/WebServer/Documents/"
  rescue
    puts "cannot upload"
  end
end

desc 'テストを行う'
task :test do
  chdir LIB+'/..' do
    sh "rspec spec"
  end
end
