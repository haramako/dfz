# coding: utf-8
#
# Rakefile
#

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
