#!/usr/bin/env ruby
require 'find'
require 'fileutils'

table = {
  'BC'=>0,
  'BL'=>1,
  'ML'=>2,
  'TC'=>4,
  'TL'=>3,
}

Find.find('Assets/Gardens/Characters').to_a.each do |f|
  next unless f.match(/(BC|BL|ML|TC|TL).+\.png$/)

  f2 = f.gsub(/(BC|BL|ML|TC|TL)/){|x| table[x] }
  FileUtils.mv f, f2
  puts f,f2
end
