# カスタムバリデータ

# json構文チェック
class JsonFormationValidator < ActiveModel::EachValidator
  def validate_each(record, attribute, value)
    begin
      values = JSON.parse(value)
    rescue
      record.errors[attribute] << "json構文に問題があります。"
    end
  end
end

# スキルjsonを検証
class SkillJsonFormationValidator < ActiveModel::EachValidator
  def inspect (record, attribute, data)
    # TODO: 頻繁に追加が無いため、直接記述
    check_scope = %w[ self player direct_attack straight threeway fiveway eightway cross around room floor beam ]
    check_type = %w[
                    acid add_item add_arrow antidote artemis_arrow attack bind blind blowback bow
                    brave_up breakdownwallexplosion charge clear_status confuse confuse_lobi convene
                    cure_status cutoff emperor_of_thunder explosion explosion2 fang_charge_skill
                    fang_fast_burst fenrir_whip fire forced_teleport goldup great_fire hallucinatory
                    haste heal hpdrain invincible invisible jabber_punch keep_brave mix multislash
                    maxhpup nighteye none poison powerup protect resurrection rust satiety silence
                    slash sleep sleep_paralysis slow spawn_enemy steal_item strong_attack suicide
                    summon_enemy swap swap2 throw teleport thunder view_enemy view_item view_map
                    warp warp_by whip_attack charge_skill discharge_skill burst disburst banish
                    direct_teleport double_teleport effect_only fang_message forced_event_teleport
                    lua spawn_rock step_back value water_laser water_laser_ready viewmap amplify guard
                    emergency additem view_trap view_mimic shot_arrow add_arrow_user_stat disprotect serif_oneshot
                    serif attack_canon throw_bomb release_dragon_power maxhpdrain steal_gold
                    float_value fang_charge_skill_and_fast_burst multislash2 disburst2 clear_status2
                   ]
    # ****_force系
    check_type += %w[ acid_force bind_force blind_force confuse_force hallucinatory_force silence_force sleep_force sleep_paralysis_force slow_force poison_force ]
    # on_****系
    check_type += %w[ on_brave_chain on_brave_zone on_counter on_floor on_burst on_enchant on_new_room on_start on_revenge on_kill on_trap on_damage on_dead on_brave_down on_attack on_protect on_critical_check on_get_wand on_pick_gold on_heal on_under_attack on_walk_recover on_guard on_bow on_bow_kind on_bow_power ]

    # ここまで
    record.errors[attribute] << "記入に問題があります。 scope: #{data[:scope]}, json: #{value}" if data[:scope].present? and not check_scope.include?(data[:scope])
    record.errors[attribute] << "記入に問題があります。 type: #{data[:type]}, json: #{value}" if data[:type].present? and not check_type.include?(data[:type])
    # スペシャルのある場合
    if data.include?(:special)
      special = data[:special]
      # スペシャルが配列の場合
      if special.kind_of?(Array)
        special.each do |d|
          record.errors[attribute] << "special内の記入に問題があります。 type: #{d[:type]}, json: #{value}" if not check_type.include?(d[:type]) or d[:type].nil?
          record.errors[attribute] << "special内にnextが含まれています。 json: #{value}" if d[:next].present?
        end
      else
        record.errors[attribute] << "special内の記入に問題があります。 type: #{special[:type]}, json: #{value}" if not check_type.include?(special[:type]) or special[:type].nil?
        record.errors[attribute] << "special内にnextが含まれています。 json: #{value}" if special[:next].present?
      end
    end
    inspect(record, attribute, data[:next]) if data.include?(:next)
  end

  def validate_each(record, attribute, value)
    if value.present? and value =~ /^\{/
      begin
        data = JSON.parse(value.downcase, symbolize_names: true)
        inspect record, attribute, data
      rescue
        record.errors[attribute] << "json構文に問題があります。 json: #{value}"
      end
    end
  end
end

# 複数値が入力されている場合のInclusion
class MultiInclusionValidator < ActiveModel::EachValidator
  def validate_each(record, attribute, value)
    values = value.split(/:|,/).map{|d| d.to_i if d =~ /^[0-9]+$/}.compact
    values.each do |v|
      record.errors[attribute] << "is not included in the list" unless options[:in].include?(v)
    end
  end
end