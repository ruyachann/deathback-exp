# 001 — Input SystemのEditorコンパイル阻害を解消する

状態: Sonnet実装依頼用。統合・受入は未完了。

## 問題と根拠

Unity 6000.3.15f1で開いた既存プロジェクトは、Input System 1.12.0の `InputSystemPluginControl.cs(47,25)` で `CS0117: BuildTarget does not contain a definition for ReservedCFE` となる。
現行パッケージソースでも `UNITY_6000_0_OR_NEWER` の下で当該enumを参照している。

## 変更許可範囲

`Packages/manifest.json` の `com.unity.inputsystem` のバージョンのみ。候補は **1.17.0**。
他の依存、プロジェクト設定、Library/PackageCacheは編集しない。packages-lock.jsonはUnityの依存解決に委ねる。
既にユーザーがmanifestを変更していたら差分を報告し、旧版で上書きしない。

## 設計判断

削除されたEditor APIをPackageCache内で書き換える応急措置を避け、公開パッケージを更新する。
Unity公式Input System 1.17の変更履歴は存在し、1.16でUnity6.3 betaの警告修正も記録されている。ただし、この情報だけで6.3.15f1と全XR依存の組合せが検証済みとはしない。

- https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/changelog/CHANGELOG.html
- https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.inputsystem.html

## 受入条件

1. JSONとして妥当、他の依存の値は同一。
2. Unityが1.17.0を解決し、ReservedCFEエラーが消える。新しいエラーは記録して次の別タスクへ。
3. プロジェクトC#の統合コンパイルを試し、結果を記録する。
4. 最初のデスクトップ再生でEnter/Space/E、後続実機でHMD/手/グリップを確認する。未実行は未確認と記す。
5. 同じ変更版をCodex/AstraとClaudeがレビューし、報告を交換する。

このタスク単独で「VRデモ動作済み」とは扱わない。ツール無効の入力パケットの場合、実装案だけを返し、実際のUnity確認は実行しない。
