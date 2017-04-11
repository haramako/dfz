#!/usr/bin/env ruby

require 'sinatra'

set :public_folder, __dir__ + '/public'

get '/' do
  'Hello!'
end

get '/tags/:tag' do
  IO.read("./tags/#{params[:tag]}")
end

post '/tags/:tag' do
  IO.write("./tags/#{params[:tag]}", params[:val])
  'OK'
end
