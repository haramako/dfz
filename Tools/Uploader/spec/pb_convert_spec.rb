# coding: utf-8
require 'spec_common'
require 'pb_convert'
  
describe PbConvert do
  include PbConvert

  describe 'conv_message' do
    it 'convert map' do
      m = conv_message(Test::MapTest, {f: {'hoge'=>1, 'fuga'=> 2}})
      conv_message(Test::MapTest, {f: {hoge: 1, fuga: 2}})
    end

    it 'convert array' do
      x = Test::ArrayTest.new
      m = conv_message(Test::ArrayTest, {f: [1,2,3]})
      expect{ conv_message(Test::ArrayTest, {f: ['1','2','3']}) }.to raise_error(TypeError)
    end
    
  end

  describe 'chunk list' do
    it 'pack and unpack chunk list' do
      src = ['HEADER', ['hoge','fu','ga']]
      expect(unpack_chunk_list(pack_chunk_list(*src))).to eq(src)
    end
  end

  describe 'pb list' do
    it do
      src = [Test::ArrayTest.new(f:[1]), Test::ArrayTest.new(f:[2])]
      expect(unpack_pb_list(pack_pb_list(src))).to eq(src)
    end
  end

  describe 'pbx' do
    it do
      src = {1=>Test::Simple.new(f: 1), 3=>Test::Simple.new(f: 1), "x"=>Test::Simple.new(f: 1)}
      expect(unpack_pbx(pack_pbx(src))).to eq(src)
    end
  end
end
