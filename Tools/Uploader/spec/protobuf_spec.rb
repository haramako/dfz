# coding: utf-8
require 'spec_common'

=begin

Protobufの挙動確認用

Descriptor
- each
- lookup
- add_field
- add_oneof
- each_onoof
- lookup_oneof
- name
- msgclass

FieldDescriptor
- name
- type
- label
- submsg_name
- subtype
- get
- set

Map
- each
- keys
- values
- []
- []=
- has_key?
- delete
- clear
- length
- dup
- ==
- hash
- inspect
- merge

RepeatedField
- each
- []
- []=
- push
- <<
- size
- to_ary
=end

# Google::Protobuf の挙動確認
describe Google::Protobuf do

  it 'message descriptor' do
    model_desc = Test::ArrayTest.descriptor
    expect( model_desc).to be_an_instance_of(Google::Protobuf::Descriptor)
  end
  
  it 'map descriptor' do
    expect( Test::MapTest.new(f: {'hoge'=> 1, 'fuga'=> 2}) ).to be_an_instance_of(Test::MapTest)
    expect{ Test::MapTest.new(f: {hoge: 1, fuga: 2}) }.to raise_error(TypeError)
    
    d = Test::MapTest.descriptor.lookup('f')
    
    expect( d ).to be_an_instance_of(Google::Protobuf::FieldDescriptor)
    expect( d.type ).to eq :message
    expect( d.label ).to eq :repeated
    expect( d.subtype ).to be_an_instance_of(Google::Protobuf::Descriptor)
    expect( d.subtype.name ).to eq "Test.MapTest_MapEntry_f"
    expect( d.map_entry? ).to be_truthy

    # p m.f.class.instance_methods(false)
    # p m.f['hoge'] = 1
  end

  it 'array descriptor' do
    expect( Test::ArrayTest.new(f: [1,2,3]) ).to be_an_instance_of(Test::ArrayTest)
    expect{ Test::ArrayTest.new(f: ['1','2','3']) }.to raise_error(TypeError)
    
    d = Test::ArrayTest.descriptor.lookup('f')
    
    expect( d).to be_an_instance_of(Google::Protobuf::FieldDescriptor)
    expect( d.type ).to eq :int32
    expect( d.label ).to eq :repeated
    expect( d.subtype ).to be_nil
    expect( d.map_entry? ).to be_falsey

  end
  
end
