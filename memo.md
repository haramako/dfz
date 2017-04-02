# TODO

# ターン処理

- ターン開始
 - 状態異常(満腹も)のターン数消化/消滅

- プレイヤーターン
 - ターンを使わない行動
 - 攻撃/アイテム/スキル
 - 移動
  - 階段
 

- 敵ターン
 - 反撃処理
 - 移動思考 + 移動(プレイヤー移動と一緒に動く)
  - 倍速処理はどうする？(２歩まとめて移動している）
 - 攻撃思考 + 攻撃/スキル

- ターン終了

- スキル
 - 範囲計算
 - ダメージ
 - 吹き飛ばし等移動処理だけは特別にしたほうがよいかも？
 - 予約行動(リアクション)の消化(スキルの繰り返し)

- スキルのダメージをまとめて表示

# 思考ルーチン

- 探索モード
 - 探索
- 狙いモード
 - 特定のキャラクタを狙う
 - KEEP_DISTANCEなども
- ランダム

# 基盤ライブラリ

* アプリケーション層
* システム層
 * UI(ScreenLock/Modal/Window)
 * Widgets
 * CFS
 * ResourceCache
 * デバッグ(スクリーンショット/ロギング/デバッグメニュー)
* 最下層
 * Promise( UniTask? )
 * Configure / DI
 * SLua
 * アセバン作成
 * ProgocolBuffer
 * WebView
 * 課金
 * ロギング
 * フォルダ構成標準

# 思考ルーチンコード


## lua
B.Target = B.FindTarget() 
B.UseSkill(Cooldown=3) or B.AttackTarget(B.Target) or B.GotoTarget(B.Target)

## 独自言語
FindPlayer(Len=10) 

: (UseSkill(Cooldown=3) | AttackTarget() | GotoTarget())
: GotoRandom()

## Lisp

(or
  (if (FindPlayer Len 10)
   (or (AttackTarget) (GotoTarget))
  )
  (GotoRandom)
)


## XML

<Any>
  <FindPlayer Len=10>
    <AttackTarget/>
    <GotoTarget/>
  </FindPlayer>
  <GotoRandom/>
</Any>

## DriverParam

SkillCooldown=3
UseSkill

# コンストラクト

If( Cond:Hoge(), Then:Or(Fuga(100, Piyo()), Hage()) Else:())

(if (hoge) (or (fuga id:100 target:(piyo)) (hage)))

