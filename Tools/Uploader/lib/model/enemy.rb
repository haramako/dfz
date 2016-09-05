# coding: utf-8
# 
module Model

  class Enemy
    include Base

    def initialize(*args)
      super

      @driverParam << ",NotRESPAWN" if /ROCK|TRAPGOLD|MIMETIC|INVISIBLE/ =~ @driverParam
    end
  end

end
