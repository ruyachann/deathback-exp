# 029 — ビルドの出どころの記録（実機確認の基準版）

状態: **受入（2026-09-25、reviews/tasks028-029-exchange-20260925.md）**。計画 Claude Opus 5.5（Astra の計画レビュー 上位3件の3）。実装 GPT-5.6 Sol。レビュー 実装とは別セッションの Sol と Claude Sonnet 5。task028（Sonnet）と並行。

## 背景

`Start-DeviceCheck.ps1` は、実行ファイルの更新日時と**今の**ローカル HEAD を記録する。ビルドの後にコミットや編集をすると、どのソースから作ったビルドかが分からない。実機確認の結果と修正前後の比較のため、ビルドの時点の情報をビルドと一緒に残す。

## 設計

1. `DemoSetup.Build()` の成功後に `Builds/Windows/build-info.json` を書く: `commit`（`git rev-parse HEAD` の完全なハッシュ）、`dirty`（`git status --porcelain` に Assets/・ProjectSettings/・Packages/ の変更があれば true。Collaboration/ など体験に入らない変更は数えない）、`dirtyFiles`（そのパスの一覧、最大20件）、`builtAt`（ISO 8601）、`unityVersion`。git が使えない場合は `commit` を "unknown" にしてビルドは失敗させない。
2. `Start-DeviceCheck.ps1`: 起動時に `build-info.json` があれば、その内容を記録ファイルに含め、`dirty=true` なら「未コミットの変更を含むビルド」と警告を表示する。無ければ「build-info なし（古いビルド）」と表示する。今の HEAD と build-info の commit が違えば、それも表示する。
3. ビルドの副作用のファイル（Assets/Settings など）は `dirty` の判定の前に数えないで済むよう、判定はビルドの**前**に行う（ビルド中に Unity が書き換える分を数えない）。

### 追修正（2026-09-25、計画担当の確認で判明）

`CaptureBuildInfo()` が `Prepare(); ConfigureXR(); Validate();` の**後**に呼ばれるため、Prepare が書き換える `ProjectSettings/ProjectSettings.asset` が dirty に数えられる（コミット済みの状態からビルドしても dirty=true になる）。→ 情報の取得を Build() の最初（Prepare より前）に移す。

## 許可ファイル

- `Assets/LoopRoom/Editor/DemoSetup.cs`、`Start-DeviceCheck.ps1`

## 受入条件

1. batchmode のビルドで build-info.json ができる（計画担当が確認・撮影）。Start-DeviceCheck.ps1 の表示（desktop で起動して確認）。
2. 別セッションの Sol と Sonnet の独立レビュー、交換後に両方 approve。
