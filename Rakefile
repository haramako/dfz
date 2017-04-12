# coding: utf-8

#
# Rakefile
#

if RUBY_PLATFORM =~ /darwin/
  ASTYLE = './Tools/Bin/AStyle'
else
  ASTYLE = './Tools/Bin/AStyle.exe'
end

desc 'ドキュメントの作成をする'
task :doc do
  rm_rf 'Doc'
  mkdir_p 'Doc'
  sh 'doxygen'
end

desc 'プログラムに必要な変換を行う'
task :prebuild do
  chdir 'Data' do
    sh 'rake proto'
  end
end

task :format => [:format_cs, :format_ruby]

desc '*.csファイルを整形する'
task :format_cs do
  srcs = FileList['Client/Assets/Scripts/**/*.cs']
         .exclude('**/AutoGenerated/**')
         .exclude('**/Vendor/**')
  sh ASTYLE, '--options=.astyle', *srcs
end

desc '*.rbファイルの整形チェックをする'
task :rubocop do
  sh 'rubocop', 'Tools', 'Rakefile'
end

desc '*.rbファイルを整形する'
task :format_ruby do
  sh 'rubocop', '-a', 'Tools', 'Rakefile'
end
