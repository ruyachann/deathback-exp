# 022 — 証拠用の自動実行がフォーカス喪失で中断しないようにする

状態: **受入（2026-09-25、reviews/tasks022-023-exchange-20260925.md）**。計画 Claude Opus 5.5。実装 Claude Sonnet 5。レビュー 実装とは別セッションの Sonnet と GPT-5.6 Sol。task023（照明、Sol）と並行。

## 背景

desktop 表示ではウィンドウのフォーカスを失うとセッションを中断する（LoopDemo.cs の `!rig.IsVR && !Application.isFocused && !Application.isEditor && !idle`）。これは desktop での確認用として正しいが、証拠撮影の自動実行（`--desktop --autostart` など）では、撮影スクリプトや他のウィンドウが前に出るだけで中断され、途中の画面が撮れない（STATE 項目9の保留事項）。

## 設計

- `--autostart` が有効な実行（`--desktop` 併用時のみ有効という既存の条件はそのまま）では、フォーカス喪失による中断をしない。
- それ以外（通常の desktop 表示、VR）の挙動は変えない。Esc・追跡喪失・`--autoescape` の中断は従来どおり。
- 起動時に、フォーカス喪失を無視していることを Debug.Log で1回出す（証拠のログで分かるように）。

### 追修正（2026-09-25、独立レビュー指摘。計画担当 Opus 5.5）

Sol approve、Sonnet request_changes（`sonnet-task022-independent-mcp.md`）。採用（中）: `OnApplicationPause(true)`（最小化など）でも desktop のセッションが中断されるため、`--autostart` 実行ではこの経路でも中断しない（`!autostart` を条件に加える）。

## 許可ファイル

- `Assets/LoopRoom/Scripts/LoopDemo.cs`

## 受入条件

1. batchmode 0/0、テスト全件 PASS。
2. 証拠: `--desktop --auto-calibrate --autostart` の実行中に別ウィンドウを前面に出しても周回が続く（計画担当が撮影、ログの1行）。
3. 別セッションの Sonnet と Sol の独立レビュー、交換後に両方 approve。
