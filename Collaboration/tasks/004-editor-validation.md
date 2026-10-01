# 004 — 新正本のEditor検証

目的: 段階2の最初の検証として、既に通常Hubで開いたC:/deathback/deathback-exp（Unity6000.3.15f1）でPrepare/Configure/Validateと短いPlayを確認する。

担当: Sol。unity:unity-cliスキルを読み、正本cwdでCLIのコマンド一覧・schemaを取得する。既存Editorへ接続し、二重起動しない。既存の未保存編集の保護はPrepareの保存確認を尊重する。

許可範囲: 既存DemoSetupのメニュー実行に伴うシーン/XR設定/プロジェクト設定の生成、モデル検証、短いPlay/Stop。ソース変更、依存更新、実機合格判定、ユーザーの保存確認の代行、長時間セッション操作は範囲外。ユーザーが新たにPlay中なら操作を競合させず報告する。

受入: 正本の接続先/PID/Unity版を確認、実在メニューを実行、設定検証とモデルのログ証拠を取得。Play状態/生成オブジェクト/例外を取得しStopで戻す。HMDなしのReady確認を実際の死亡/コントローラー検証として扱わない。

証跡: 実行したCLI/状態/ログ要点/変更対象とSHAを保存。原文の認証情報、Library、PackageCache、生ログはpushしない。失敗時はエラーと未実施を記録し、別Editorやキャッシュ書換えを代替にしない。
