# coding: utf-8

require 'rexml/document'
require 'pp'
require 'pb_convert'

# モンキーパッチ
class REXML::Element
  def a
    attributes
  end
end

class Tmx
  Obj = Struct.new(:x, :y, :name, :char, :speed)

  attr_reader :filename, :width, :height, :layers, :tiles

  def initialize(tmx)
    @filename = tmx
    @doc = REXML::Document.new(IO.read(tmx))
    map = @doc.elements['map']
    @width = map.a['width'].to_i
    @height = map.a['height'].to_i

    @layers = []
    map.each_element('layer') do |l|
      @layers << l.elements['data'].text.split(/,/).map(&:to_i).each_slice(@width).to_a.reverse
    end

    make_tile

    @objects = []
    map.each_element('objectgroup') do |group|
      group.each_element('object') do |obj|
        x = obj.a['x'].to_i / 32
        y = @height - obj.a['y'].to_i / 32
        name = obj.a['name']
        char = nil
        speed = nil
        props = obj.elements['properties']
        if props
          props.each_element('property') do |prop|
            if prop.a['name'] == 'Char'
              char = prop.a['value'].to_i
            elsif prop.a['name'] == 'Speed'
              char = prop.a['speed'].to_i
            end
          end
        end
        @objects << Obj.new(x, y, name, char || 0, speed || 100)
      end
    end
  end

  def make_tile
    @tiles = @layers[0]
  end

  def dump_pb
    characters = @objects.map do |obj|
      # p obj.to_h
      Master::StageCharacter.new(obj.to_h)
    end
    require 'digest/md5'
    id = Digest::MD5.hexdigest(@filename)[0, 8].to_i(16)
    pb = Master::Stage.new(
      id: id,
      symbol: File.basename(@filename, '.tmx'),
      width: @width,
      height: @height,
      tiles: @tiles.flatten,
      characters: characters
    )
    PbConvert.pack_pb_list([pb])
  end
end
