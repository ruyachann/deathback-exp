# 024 — 後の課題の整理（STATE 項目9 の a・b・e）

状態: **受入（2026-09-25、reviews/task024-exchange-20260925.md）**。計画 Claude Opus 5.5。実装 GPT-5.6 Sol（A）∥ Claude Sonnet 5（B）。レビュー 実装とは別セッションの Sol と Sonnet。A と B は別ファイルなので並行。

## A（Sol）: 生成後の Rules の書き換えを防ぐ（9-a）

- 現状: `LoopModel.Rules` は readonly の参照だが、`LoopRules` のフィールドは public で書き換えられる。外から `Model.Rules.firstShot = 1` と書くと動作中の規則が変わる。
- 設計: LoopModel は規則を private のフィールドに持ち、内部の計算はそれを使う。公開の `Rules` は**呼ぶたびに複製を返す**プロパティにする（外で書き換えても内部に影響しない）。LoopRules の既存の公開 API（フィールド、Clone、Validate、MinInterval）は変えない（JsonUtility の保存とテストのため）。
- 利用側: LoopDemo の毎フレームの参照（`Model.Rules.exitOpens` など）で複製が毎フレーム作られないよう、必要なら LoopModel に読み取り専用のプロパティ（例 `ExitOpens`）を足すか、LoopDemo 側で開始時に1回だけ複製を保持する。どちらでもよいが、LoopDemo の変更は最小限にする。
- テスト: LoopModelChecks に「`Model.Rules` の値を書き換えても、モデルの動作（例 firstShot の時刻の死亡）が変わらない」を追加。dotnet の Program.cs も同様に回ること。
- 許可ファイル: `Assets/LoopRoom/Scripts/LoopModel.cs`、`Assets/LoopRoom/Editor/LoopModelChecks.cs`、`Tests/LoopModel.Tests/Program.cs`（必要なら）、`Assets/LoopRoom/Scripts/LoopDemo.cs`（Rules の参照箇所だけ）

## B（Sonnet）: 入力サブシステムの一覧の共用をやめる（9-b）と、観客表示の未使用引数（9-e）

- 9-b: DemoRig の `inputs` 一覧を FloorReady（毎フレーム）と XR 準備のコルーチンが共用している。同じフレームでの取り違えを避けるため、それぞれ別の一覧を使う。挙動は変えない。
- 9-e: `RoomVisuals.UpdatePublic(bool vr, bool active)` の `active` は使われていない。**判断（計画担当）**: 暗転中も観客のアバターは体験者の実際の頭と手の動きを表すので、表示・更新を続けるのが正しい。したがって挙動は変えず、使われていない引数 `active` を削除して呼び出し側（LoopDemo の1か所）を合わせる。
- 許可ファイル: `Assets/LoopRoom/Scripts/DemoRig.cs`、`Assets/LoopRoom/Scripts/RoomVisuals.cs`、`Assets/LoopRoom/Scripts/LoopDemo.cs`（UpdatePublic の呼び出し1か所だけ）

LoopDemo.cs は A と B の両方が触るが、箇所が別（A: Rules の参照、B: UpdatePublic の呼び出し）。後から実装する側が前の変更を消さないこと。

## 受入条件

1. batchmode 0/0、テスト全件 PASS（Unity 付属 Roslyn と dotnet 相当）。
2. 証拠: テスト出力の画像と、desktop の自動実行で周回・暗転・観客表示が従来どおりの画面（計画担当が撮影）。
3. 別セッションの Sol と Sonnet の独立レビュー、交換後に両方 approve。
