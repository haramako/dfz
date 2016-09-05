# coding: utf-8
require 'spreadsheet'
require 'stringio'
require 'pp'
require 'json'
require_relative 'master'

class Master::StageSetItem
  def to_s
    "#{id}:#{num}"
  end
  alias inspect to_s
end

class Master::StageSetList
  def to_s
    "[S #{total}, #{items.to_s}]"
  end
  alias inspect to_s
end


# 敵の出現情報をマトリックス化した表
module Townpage
  
  module_function

  SHEET_NAMES = {
    'モンスター'=>'enemy_sets',
    'アイテム'=>'item_sets',
    'トラップ'=>'trap_sets',
  }

  def parse(path)
    book = Spreadsheet.open( StringIO.new(IO.binread(path)) )
    data = Hash.new do|h,k|
      d = Master::StageSetData.new(
        id: k,
        enemy_sets: Array.new(10){ Master::StageSetList.new },
        item_sets: Array.new(10){ Master::StageSetList.new },
        trap_sets: Array.new(10){ Master::StageSetList.new },
      )
      h[k] = d
    end
    SHEET_NAMES.map do |sheet_name, key|
      sheet = book.worksheet(sheet_name)
      raise "sheet '#{sheet_name_or_index}' not found" unless sheet
      parse_sheet(data, key, sheet)
    end
    data
  end
  
  def parse_sheet(data, key, sheet)
    header = nil
    sheet.each_with_index do |row,row_num|
      if row_num == 0
        header = row.map do |c|
          if Float === c
            c.to_i.to_s # なぜか "99" => "99.0" 担ってしまう問題対策
          else
            c.to_s
          end
        end
      else
        next unless row and row[0]
        next if row[0].to_s[0] == '#'
        next if row[0].to_i == 0
        
        stage_id, set_alphabet = row[0].to_s.split(/:/)
        stage_id = stage_id.to_i
        if set_alphabet
          set_idx = set_alphabet.ord - 'A'.ord
        else
          set_idx = 0
        end
        
        row_data = []
        total = row[1].to_i
        header.each.with_index do |id,i|
          next if i <= 2
          next if id == 0 or id == "" or row[i].to_i == 0
          id = id.strip
          row_data << Master::StageSetItem.new(id: id, num: row[i].to_i)
        end
        # p [stage_id, set_idx, {total: total, items: row_data}]
        data[stage_id].__send__("#{key}")[set_idx] = Master::StageSetList.new(total: total, items: row_data)
      end
    end
  end

end

if $0 == __FILE__
  ARGV.each do |f|
    puts f
    pp Townpage.parse(f).map{|k,v| JSON.parse(v.to_json)}
    puts
  end
end
