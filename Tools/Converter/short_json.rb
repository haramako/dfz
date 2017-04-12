# -*- coding: utf-8 -*-

require_relative 'short_json.tab.rb'

module ShortJson
  class Parser
    def initialize(src)
      @scanner = StringScanner.new(src)
    end

    def next_token
      # コメントと空白を飛ばす
      while @scanner.scan(%r{\s+ | //.+?\n | /\*.+?\*/ }mx)
      end

      if @scanner.eos?
        r = nil
      elsif @scanner.scan(/\(|\)|:|,/)
        # 記号
        r = [@scanner[0], @scanner[0]]
      elsif @scanner.scan(/\w+/)
        r = [:STRING, @scanner[0]]
      else
        # :nocov:
        raise "invalid token at #{@scanner.pos}"
        # :nocov:
      end
      r
    end

    def make_args(obj, vals)
      vals[0].each_with_index do |v, i|
        obj[i] = v
      end
      vals[1].each do |k, v|
        obj[k.to_sym] = v
      end
    end

    def parse
      ast = do_parse
      ast
    rescue Racc::ParseError
      raise "Error #{$ERROR_INFO} at #{@scanner.pos}"
    end
  end

  module_function

  # ShortJson.parse('hoge(fuga:1,piyo:2)') # => {"type":"hoge", "fuga":1, "piyo":2}
  def parse(src)
    Parser.new(src).parse
  end

  def parse_array(str)
    str.split(/,/).map { |element| parse(element) }
  end
end
