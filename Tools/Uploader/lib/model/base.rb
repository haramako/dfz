# coding: utf-8

module Model

  # 基本刑
  module Base

    def initialize(src)
      if src.is_a? Hash
        src.each{|k,v| instance_variable_set("@#{k}",v) }
      else
        raise "error base module"
      end
    end

    def to_hash
      d = Hash.new
      instance_variables.each do |k|
        key = k.to_s.delete("@").to_sym
        val = instance_variable_get k
        val = case val
        when ""; nil
        when /^[0-9]+$/; val.to_i
        else val
        end
        d[key] = val
      end
      d
    end
  end

end
