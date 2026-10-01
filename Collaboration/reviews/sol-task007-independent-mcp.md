## Findings

- **中 — 上限無効時、非常に大きい有限 `delta` で `Advance()` が実質停止する**
  - 場所: `Assets/LoopRoom/Scripts/LoopModel.cs:119-145`
  - 対象 SHA256: `dc0d1dc38a90c6fc74c70d4780feb160d2339d261b0d88121a9253f1b4685a30`
  - トリガー: 既定ルールで開始後、`Advance(double.MaxValue)` のような有限だが巨大な値を渡す。
  - 原因: 上限無効時は `untilLimit = PositiveInfinity` となり、各周回で `slice` は約6秒以下になります。しかし巨大な `delta` では浮動小数点の精度上、`delta -= slice` が `delta` を変化させません。状態だけが周回し続け、ループ終了条件へ進めません。非有限値は拒否していますが、これは現在受理される有限入力です。
  - テスト上の不足: `Assets/LoopRoom/Editor/LoopModelChecks.cs:37-41`（SHA256 `05b8b2fddc3776849ad624858fa4786563d4925bc3cca1089e3f80b17371041d`）は1000秒のみで、この非進行条件を検出しません。
  - 修正案: 1回の `Advance` に受理する最大値を明示して状態変更前に拒否するか、反復周回を高速化して必ず残時間が減る実装にする。そのうえで巨大な有限値が速やかに拒否または完了するテストを追加する。

通常の有限フレーム時間については、`Math.Min(x, PositiveInfinity)` は有限側を返し、`TimedOut` 判定も必要箇所で `enforcePlayLimit` により保護されています。既定 false の1000秒継続、true の172秒 `TimedOut`＋8秒後の180秒 `Finished`、条件付き検証、脱出・中断・死亡・Blackout・旧周回入力拒否について、ほかに具体的な退行は見つかりませんでした。

設計に列挙された3項目は、1000秒の既定動作、条件付き `Validate()`、既存の脱出・中断チェックにより概ね検証されています。`Program.cs`（SHA256 `4d01d73bcfb82168332c4aaeadaaa17524d5419d45f4811e92ceedd975534e8d`）の件数13＋追加1件も整合しています。

## Verdict

**request_changes**

上限無効化で新たに到達可能になった、受理済み有限入力による非進行を解消または明示的に拒否する必要があります。

## 未確認事項

- テスト、Unityコンパイル、Editor Play、実機確認は実行していません。
- シーンファイル自体は提示されていないため、保存値が実際に `playLimit: 172`／`endingLength: 8` で、新フィールドが未保存であることは確認できません。
- 未保存の `bool` が false になる前提は妥当です。CLRの既定値とフィールド初期値がともに false ですが、既存シーンのUnityデシリアライズ結果は未検証です。
- 180秒超過後の脱出・Esc・追跡喪失は静的には従来経路を維持していますが、統合動作は未確認です。

レビュー対象として確認したその他のSHA256:

- `AGENTS.md`: `be53f607d20e3503473a167b094d52e1ac454328b09b0c7e64a3df382ae1ebd5`
- `CLAUDE.md`: `5e88123462781bc38ff4f6e797d6da67afe5d832a5abc9959b6c6ed4065e50f6`
- task007: `04098ddac34a66a053489bc1db8a037832e84af16e789ef6eacdce17ec3b2d93`
- `LoopDemo.cs`: `9d6925b4e3dcd36edeb2d338f1f39bb9c479bebf0a2d2bfb4caa1380d0e39097`
