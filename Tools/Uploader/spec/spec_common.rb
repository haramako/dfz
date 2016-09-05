require 'pp'

system 'protoc --ruby_out=. spec/test.proto'
system 'protoc --ruby_out=. lib/master.proto'
require 'test.rb'
require 'master.rb'

