require 'rubygems'
require 'active_hash'
require 'active_record'
require 'active_model'
require 'logger'
require 'pb_convert'
require 'custom_validators'

module Df

  def logger
    unless $_logger
      $_logger = Logger.new(STDOUT)
      $_logger.level = Logger::DEBUG if ENV['DEBUG']
      $_logger.formatter = proc { |severity, datetime, progname, msg|
        "#{'%-5s'%[severity]}: #{datetime.strftime('%H:%m:%S')} #{msg}\n"
      }
    end
    $_logger
  end

  # マスター
  class MasterBase < ActiveHash::Base
    include ActiveHash::Associations, ActiveModel::Validations

    class << self

      def pb_data(files, *need_col)
        model_name ||= self.name
        rows = []
        files.each do |f|
          puts "loading #{f}"
          pb_rows = PbConvert.parse_pb(IO.binread(f))
          pb_rows = pb_rows.map do |row|
            json = JSON.parse(row.to_json, symbolize_names: true)
            if need_col.present?
              json if ( (need_col + json.keys).uniq - json.keys ).blank? 
            else
              json
            end
          end
          pb_rows.compact!
          rows.concat pb_rows
        end
        self.data = rows
      end

      def pbx_data(files)
        model_name ||= self.name
        rows = []
        files.each do |f|
          puts "loading #{f}"
          pbx_rows = PbConvert.parse_pb(IO.binread(f))
          pbx_rows = pbx_rows.map do |k,row|
            JSON.parse(row.to_json, symbolize_names: true)
          end
          rows.concat pbx_rows
        end
        self.data = rows
      end
      
      def pb_files(pattern)
        Dir.glob("./UploadFiles/*-#{pattern}.pb")
      end

      def pbx_files(pattern)
        Dir.glob("./UploadFiles/*-#{pattern}.pbx")
      end

      def townpage_files
        Dir.glob("./UploadFiles/Townpage_*.pbx")
      end

      def validate_all
        errors = []
        self.all.each{ |f| errors << f if f.invalid? }
        errors
      end

      def validate_attributes
        @validate_attributes ||= []
      end
 
      # モンキーパッチ
      def validates(*attributes)
        @validate_attributes ||= []
        defaults = attributes.extract_options!.dup
        validations = defaults.slice!(*_validates_default_keys)

        raise ArgumentError, "You need to supply at least one attribute" if attributes.empty?
        raise ArgumentError, "You need to supply at least one validation" if validations.empty?

        defaults[:attributes] = attributes
        @validate_attributes += attributes

        validations.each do |key, options|
          next unless options
          key = "#{key.to_s.camelize}Validator"

          begin
            validator = key.include?('::'.freeze) ? key.constantize : const_get(key)
          rescue NameError
            raise ArgumentError, "Unknown validator: '#{key}'"
          end

          validates_with(validator, defaults.merge(_parse_validates_options(options)))
        end
      end
    end
  end

  class ActionSkill < MasterBase
    pb_data pb_files( 'action_skill' )

    validates :param, presence: true, skill_json_formation: true
    validates :burst_param, presence: true, skill_json_formation: true
  end

  class PassiveSkill < MasterBase
    pb_data pb_files( 'passive_skill' )

    validates :param, presence: true, skill_json_formation: true
    validates :burst_param, presence: true, skill_json_formation: true
  end

  class PvpSkill < MasterBase
    pb_data pb_files( 'pvp_skill' )

    validates :param, presence: true, skill_json_formation: true
  end

  class PvpSoul < MasterBase
    pb_data pb_files( 'pvp_soul' )
  end

  class FangTemplate < MasterBase
    pb_data pb_files( 'fang_template' )

    action_skill_ids = ActionSkill.all.map(&:id)
    passive_skill_ids = PassiveSkill.all.map(&:id)
    # if: :present?では値がある場合に判定するにならない
    validates :skill_id, inclusion: { in: action_skill_ids, if: "skill_id.present?" }
    validates :pskill_id, inclusion: { in: passive_skill_ids, if: "pskill_id.present?" }
  end

  class StackItemTemplate < MasterBase
    pb_data pb_files( 'stack_item_template' )

  end

  class PresentTemplate < MasterBase
    pb_data pb_files( 'present_template' )

    validates :action, json_formation: true
  end

  class UserStatTemplate < MasterBase
    pb_data pb_files( 'user_stat_template' )

  end

  class QuestTemplate < MasterBase
    pb_data pb_files( 'quest_template' )

    user_stat_names = UserStatTemplate.all.map(&:name)
    validates :cond_stat, inclusion: { in: user_stat_names, if: "cond_stat.present?" }
  end

  class Enemy < MasterBase
    pbx_data pbx_files( 'enemy' )

    validates :skill_attack_command, skill_json_formation: true
  end

  class Item < MasterBase
    pb_data pb_files( 'item' )

  end

  class AbilityTemplate < MasterBase
    pb_data pb_files( 'ability_template' )

  end

  # 0が引っかかる
  class ArmamentTemplate < MasterBase
    pb_data( pb_files( 'armament_template' ), :id )

    ability_template_ids = AbilityTemplate.all.map(&:id)
    validates :soul_pattern, multi_inclusion: { in: ability_template_ids, if: "soul_pattern.present?" }
  end

  class TownpageData < MasterBase
    pbx_data townpage_files
  end

  class GoalSetData < MasterBase
    pb_data pb_files( 'goal_set_data' )

  end

  class RoomData < MasterBase
    pb_data pb_files( 'room_data' )

  end

  class StageData < MasterBase
    pbx_data pbx_files( 'stage_data' )

    room_data_ids = RoomData.all.map(&:group_id).uniq
    validates :fixed_room_group_id, multi_inclusion: { in: room_data_ids, if: "fixed_room_group_id.present?" }
    validates :random_room_group_id, multi_inclusion: { in: room_data_ids, if: "random_room_group_id.present?" }
=begin
    # 出現数が多すぎるため、隔離
    enemy_set_data_ids = TownpageData.all.map{|d| d.id if d.enemy_sets.map{|i| i[:items].present?}.include?(true) }.uniq
    item_set_data_ids = TownpageData.all.map{|d| d.id if d.item_sets.map{|i| i[:items].present?}.include?(true) }.uniq
    trap_set_data_ids = TownpageData.all.map{|d| d.id if d.trap_sets.map{|i| i[:items].present?}.include?(true) }.uniq
    goal_set_data_ids = GoalSetData.all.map(&:id)
    validates :enemy_set_id, inclusion: { in: enemy_set_data_ids, if: "enemy_set_id.present?" }
    validates :item_set_id, inclusion: { in: item_set_data_ids, if: "item_set_id.present?" }
    validates :trap_set_id, inclusion: { in: trap_set_data_ids, if: "trap_set_id.present?" }
    validates :goal_set_id, inclusion: { in: goal_set_data_ids, if: "goal_set_id.present?" }
=end
  end

  class DungeonData < MasterBase
    pb_data pb_files( 'dungeon_data' )

    stage_data_ids = StageData.all.map(&:id)
    validates :start_stage_id, inclusion: { in: stage_data_ids, if: "start_stage_id.present?" }
  end

  class AreaData < MasterBase
    pb_data pb_files( 'area_data' )

    dungeon_data_ids = DungeonData.all.map(&:id)
    validates :start_dungeon_id, inclusion: { in: dungeon_data_ids, if: "start_dungeon_id.present?" }
  end

  MasterBase.subclasses.each do |subclass|
    puts "#{subclass}, #{subclass.count} items"
    valid_cols_str = subclass.validate_attributes.map(&:to_s).join(", ")
    puts "検証対象: #{valid_cols_str}" if valid_cols_str.present?
    subclass.validate_all.map do |f|
      f.errors.map{|k,v| logger.error "[ID: #{f.id}] #{k} #{v}"} 
    end
  end

end