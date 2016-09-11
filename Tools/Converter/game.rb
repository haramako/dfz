# Generated by the protocol buffer compiler.  DO NOT EDIT!
# source: game.proto

require 'google/protobuf'

require 'master'
Google::Protobuf::DescriptorPool.generated_pool.build do
  add_message "Game.Character" do
    optional :id, :int32, 1
    optional :hp, :int32, 2
    optional :x, :int32, 13
    optional :y, :int32, 14
    optional :saved_dir, :int32, 15
    optional :type, :enum, 18, "Game.CharacterType"
    optional :name, :string, 16
    optional :team, :int32, 17
    optional :atlas_id, :int32, 19
    optional :attack, :int32, 3
    optional :defense, :int32, 4
    optional :max_hp, :int32, 12
    optional :move, :int32, 5
    optional :jump_height, :int32, 6
    optional :attribute, :int32, 7
    optional :race, :int32, 8
    optional :speed, :int32, 20
    optional :Active, :bool, 21
    optional :attack_skill, :message, 9, "Master.Skill"
    repeated :skills, :message, 10, "Master.Skill"
    repeated :abilities, :message, 11, "Master.Ability"
  end
  add_enum "Game.CharacterType" do
    value :Player, 0
    value :Enemy, 1
  end
end

module Game
  Character = Google::Protobuf::DescriptorPool.generated_pool.lookup("Game.Character").msgclass
  CharacterType = Google::Protobuf::DescriptorPool.generated_pool.lookup("Game.CharacterType").enummodule
end