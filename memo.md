# TODO

* テストステージの読み込み
 * 起動時に選択できるようにする

* 自動振り返り
* 方向転換
 * 自分クリック？
 
* スキル時暗転演出
* スキル時マス目強調演出

* Threadのやりとりを堅牢にする
 - かならず同時に１スレッドしか動かないようにする

* テスト用のデータをどうするか考える

# TODO

# キャラクタシステム

スプライト < 状態方向絵 < アニメーション < スキルエフェクト
                                         < カットシーン

- 状態方向絵
 - ８方向x状態数
 - 武器を持つポイント
 - 中心点/大きさ
 - オーラなどのポイント
 - 状態
  -
- アニメーション
 - 絵


# スキルシステムでやりたいこと

- エフェクト
 - 物が直線で飛ぶ
 - Wave
 - 物が放物線で飛ぶ
 - ランダムにあたる
 - サイクロン
 - 子供エフェクト
 
- 子供スキル（爆弾をなげて、爆発とか）

- 条件で発動
 - 死んだ奴に当たらない
  if(cond: 'samurai', straight(

- ダメージなどをまとめる
 - ふきとばしなどは特別扱い
 
    (straight(1), [attack(pow: 100), poison(turn: 3)])
    (self(), wait(10))
    (straight(1), [attack(pow: 100), poison(turn: 3)])
	
   - Deferred な処理を別に作る
   
- 記述方式をわかりやすく

    (straight(1), [attack(pow: 100), poison(turn: 3)])

    straight(1)
      attack(pow: 100)
      poison(turn: 3)])

- 思考ルーチンに組み込みやすく
 - straight(N), straight(1), self() は特別扱い

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

