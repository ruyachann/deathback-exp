# Sonnet stage1 docs final recheck (2026-09-24)

modelUsage: claude-sonnet-5
Targets: Docs/UNITY_ACCEPTANCE.md 5553a388f8776ebd4bc8e6800502c1c400c586e818a165eeb321b32e7776b70d, Docs/DEVELOPMENT.md de697cc16561cf9631d4a3848b4cfa97339d53bcf80fc2ef085b435f7d12d518

## 1. F1・F2 の解消判定

**F1（中）: 解消**
- 根拠: Docs/UNITY_ACCEPTANCE.md 27行目に「この文言の分岐は頭部追跡や表示ランタイムの有無を直接区別しないため、原因の判断には `HeadTracked` と `RuntimePresent()` も確認する」が追加された。DemoRig.cs 24-28行目の実装（`PreparationMessage` は `xrInitializing`／`FloorReady` のみで分岐し、`HeadTracked`・`RuntimePresent()` を直接参照しない）と矛盾なく一致する説明になった。以前指摘した「文言だけでは原因を誤診断し得る」という懸念は、文書側が明示的にその限界を注記することで解消済み。

**F2（低）: 解消**
- 根拠: Docs/DEVELOPMENT.md 11行目に「task001の管理スクリプトは過去の限定されたmanifest変更の証跡であり、今後の依存更新の一般的な実行方法にはしない。」が追記され、AGENTS.md 35行目の同一文言と一致した。両文書間の非対称性は解消済み。

## 2. DemoRig.cs 全体で新たに確認できた事項（前回は抜粋のため deferred/unverified 扱い）

- **タイムアウト機構の実在と挙動**: `StartXR()`（220-284行目）に `deadline = Time.realtimeSinceStartup + 10f`（242行目）による10秒のタイムアウトがあり、タイムアウト時は `xrInitializing=false` にするのみで、`floorPrepared` が既に true ならそのまま `FloorReady=true` を維持する。よってUNITY_ACCEPTANCE.md 27行目の「準備待ちのタイムアウト後も Floor が確定したままならこの文言となり、頭部追跡と表示ランタイムがともに復帰すれば開始ゲートが成立する」という記述は実装と一致すると確認できた。
- **Floor 準備失敗時の自動再試行なし**: 失敗経路（259, 266, 283行目）はいずれも警告ログ後 `yield break` でコルーチンを終了するのみで、Floor交渉をやり直すループは存在しない。UNITY_ACCEPTANCE.md 28行目の「Floor 準備失敗の自動再試行は現行実装にない」という否定的主張が正しいと確認できた。
- **`drivers[0]` が常に頭部トラッキングデバイスである前提**: `Initialize()` 84行目で `drivers[0]` はHMD用に最初に代入され、手のドライバ（`drivers[1]`, `drivers[2]`）はその後の `for` ループ（89-107行目）で代入される。配列初期化順序上、`drivers[0]` が常にHMDを指すという前提は妥当と確認できた。

（`Model.Phase` 遷移に関する項目はLoopDemo.cs/LoopModel.csが本レビュー対象に含まれないため、依然として未検証のまま。）

## 3. 現行方針とのずれ（F1/F2の解消判定とは別件）

Docs/UNITY_ACCEPTANCE.md は「前提条件」（10行目、1セッション180秒目標）と第8章「180秒以内の確認」（107-116行目、超過を不合格基準とする）を維持している。しかしAGENTS.md 41行目に記載の通り、2026-09-23のユーザー決定で「180秒の時間上限は当面外し、まずしっかり遊べる体験を優先する」方針に変わっている。段階1文書としては当時の仕様に沿った記述で誤りではないが、現行運用方針とは食い違っているため、今後の更新（または注記追加）対象として認識しておくべき。

## 4. 最終判定

**approve**

F1・F2はともに解消済みで、DemoRig.cs全体の確認によっても新たな矛盾は見つからなかった。180秒節の方針ずれは今回のrequest_changesの対象ではなく、今後の文書更新タスクとして別途扱うことを推奨する。
