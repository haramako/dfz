# coding: utf-8
# 
module Model

  class PassiveSkill
    include Base

    def initialize(*args)
      super

      if @is_json and @is_json.to_sym == :yes
        p, bp = Hash.new, Hash.new

        if not @type.empty?
          p[@type] = JSON.parse(@param)
          bp[@type] = JSON.parse(@burst_param)
        end
        if @type2
          p[@type2] = @param2 if @param2
          bp[@type2] = @burst_param2 if @burst_param2
        end
        if @type3
          p[@type3] = @param3 if @param3
          bp[@type3] = @burst_param3 if @burst_param3
        end
        if @type4
          p[@type4] = @param4 if @param4
          bp[@type4] = @burst_param4 if @burst_param4
        end
        @param = p
        @burst_param = bp
      end
    end
  end

end
