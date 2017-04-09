# coding: utf-8
# ほげ

$LOAD_PATH << __dir__
require 'tmx'
begin
  require 'master'
  require 'game'
rescue LoadError
  nil
end

#===================================================
# 定数
#===================================================

OUTPUT = 'Output'
TEMP = 'Temp'

if RUBY_PLATFORM.match /darwin/
  CFSCTL = '../Tools/Bin/cfsctl'
  PROTOC= 'protoc'
else
  CFSCTL = '../Tools/Bin/cfsctl.exe'
  PROTOC= '../Tools/Bin/protoc.exe' # './Tools/Bin/protoc'
end

mkdir_p [OUTPUT, TEMP], verbose: false

#===================================================
# 初期化処理
#===================================================

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

#===================================================
# タスクリスト
#===================================================

task :default => :map

task :clean do
  rm_rf OUTPUT
end

task :map => ['Output/hoge-Stage.pb', 'Output/hoge-CharacterTemplate.pb']

desc 'protoファイルからプロトコルを作成する'
task :proto do
  ['master','game', 'game_log'].each do |proto|
    sh PROTOC, '--ruby_out=../Tools/Converter', '--proto_path='+ __dir__, __dir__+"/#{proto}.proto"
    sh PROTOC, '--plugin=../Tools/Bin/protoc-gen-dfcsharp', '--dfcsharp_out=../Client/Assets/Scripts/AutoGenerated/', '--proto_path='+ __dir__, __dir__+"/#{proto}.proto"
  end
end

file 'Output/hoge-Stage.pb' => 'hoge.tmx' do |s|
  map = Tmx.new(s.source)
  IO.binwrite(s.name, map.dump_pb)
end

file 'Output/hoge-CharacterTemplate.pb' => 'Master/Character.xls' do |s|
  logger.info "Creating #{s.name}"
  model_name = 'CharacterTemplate'
  model_sheet = 'CharacterTemplate'
  model_class = Master.const_get(model_name.classify)
  PbConvert.conv_master(s.name, s.source, model_sheet, model_class)
end

desc 'アップロード'
task :upload do
  sh "#{CFSCTL} upload -t tb-dev -b client.bucket Output"
  client_hash = IO.read('client.bucket.hash')

  host = 'http://133.242.235.150:7000'
  tag = 'tb-dev'
  require 'net/http'
  res = Net::HTTP.post_form(URI.parse("#{host}/tags/#{tag}"),
                            {'val'=>client_hash})
  raise "upload failed! #{res}" unless res.is_a? Net::HTTPSuccess
  

=begin      
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
=end
  
end
