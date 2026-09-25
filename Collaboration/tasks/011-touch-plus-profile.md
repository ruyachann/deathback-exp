# 011 — Quest 3 / 3S 用 Touch Plus コントローラープロファイル（task006 B-7）

状態: **受入済み（2026-09-24）**。結果は `reviews/tasks010-011-013-exchange-20260924.md`。計画確定（2026-09-24）。計画 Claude Opus 5.5。実装 GPT-5.6 Sol。レビュー 別セッションの Sol と Claude Sonnet 5。

## 目的

実機が Quest 3 または Quest 3S になる。どちらも付属コントローラーは Touch Plus。現在 `DemoSetup.ConfigureXR` は `OculusTouchControllerProfile` だけを有効にし、`Validate` もそれを必須にしている。Touch Plus 用の `MetaQuestTouchPlusControllerProfile`（OpenXR 1.16.1 に同梱を確認済み）も有効にし、Link 経由でランタイムが Touch Plus プロファイルを返しても入力が取れるようにする。

## 設計

- `ConfigureXR`: `OculusTouchControllerProfile` に加えて `MetaQuestTouchPlusControllerProfile` も `enabled=true`。見つからない場合は既存と同じ形の警告。
- `Validate`: 「2つのうち少なくとも1つが有効」なら合格。どちらも無効なら例外。両方有効かどうかをログに出す。
- DemoRig の入力バインディングが汎用の XR コントローラーパス（`<XRController>{LeftHand}` 等）なら変更不要。特定プロファイルのパスに依存している箇所があれば、実装者が報告する（コード変更はしない）。

## 許可ファイル

- `Assets/LoopRoom/Editor/DemoSetup.cs`
- `Assets/XR/Settings/OpenXR Package Settings.asset`（ConfigureXR 実行で変わる分だけ）

## 受入条件

1. batchmode で `-executeMethod DemoSetup.ConfigureXR` と `DemoSetup.Validate` が成功し、OpenXR Package Settings.asset で Touch Plus プロファイル（Standalone）が `m_enabled: 1` になる差分だけが出る。
2. batchmode コンパイル エラー0・警告0。
3. 別セッションの Sol と Sonnet の独立レビュー、交換後に両方 approve。
4. 証拠: Validate のログと asset 差分の画像。
5. 実機での操作到達は Quest 3/3S 受入（計画6）で確認（本タスクでは未確認と明記）。
