# ドラゴンファングZ

## 各フォルダの説明

- Auto: DLLの自動コンパイル用
- Client: クライアント
- Data: データ、コンバーター
- Server: サーバー

## ビルド方法

基本的には、ダウンロードしたままUnityで Client をひらけば良い

.protoファイルなどを変更した場合は、ビルドをする前に、下記コマンドで必要な変換を行う

    $ rake prebuild
	
## ビルドに必要なもののインストール

    $ brew install ruby protobuf qt5            # qt5は protoc-gen-doc に必要

## 初回のセットアップ

    $ ruby setup.rb

## その他のタスク

下記コマンドでタスクの内容が確認できる
    $ rake -T
