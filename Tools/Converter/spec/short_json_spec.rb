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
    expect(ShortJson.parse('x(a:1, b:2)')).to eq(type: "x", a: 1, b: 2)
  end

  it 'object with value list' do
    expect(ShortJson.parse('x(a, b)')).to eq(type: "x", 0 => "a", 1 => "b")
  end

  it 'object with value list and key-value list' do
    data = { type: "x", 0 => "a", 1 => "b", c: 1, d: 2 }
    expect(ShortJson.parse('x(a, b, c:1, d:2)')).to eq(data)
  end

  it 'combined object' do
    expect(ShortJson.parse('x(1, y(2), 3)')).to eq(type: "x", 0 => 1, 1 => { type: "y", 0 => 2 }, 2 => 3)
  end

  it 'new line as comma between objects' do
    expect(ShortJson.parse("x(1),x(2)", true)).to eq(ShortJson.parse("x(1)\nx(2)", true))
  end

  it 'new line as comma between objects and not after comma' do
    expect(ShortJson.parse("x(1),x(2)", true)).to eq(ShortJson.parse("x(1),\nx(2)", true))
  end

  it 'dont new line as comma not between objects' do
    expect { ShortJson.parse("x(1\n2)") }.to raise_error(RuntimeError)
  end
end
