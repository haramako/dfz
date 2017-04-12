require 'spec_helper'
require 'short_json'

describe ShortJson do
  it 'symple literal' do
    expect(ShortJson.parse('x')).to eq("x")
  end

  it 'empty object' do
    expect(ShortJson.parse('x()')).to eq(type: 'x')
  end

  it 'object with key-value list' do
    expect(ShortJson.parse('x(a:1, b:2)')).to eq(type: "x", a: "1", b: "2")
  end

  it 'object with value list' do
    expect(ShortJson.parse('x(a, b)')).to eq(type: "x", 0 => "a", 1 => "b")
  end

  it 'combined object' do
    expect(ShortJson.parse('x(1, y(2), 3)')).to eq(type: "x", 0 => "1", 1 => { type: "y", 0 => "2" }, 2 => "3")
  end
end
