# 016 — 実機確認をすぐ始めるための準備（計画6の前準備）

状態: **受入済み（2026-09-24、実機の見え方・聞こえ方は計画6で確認）**。結果は `reviews/tasks014-016-exchange-20260924.md`。計画確定（2026-09-24）。計画 Claude Opus 5.5。実装: (a) Claude Sonnet 5、(b)(c) 計画担当が実行・作成（ビルドと手順書）。レビュー Sol と別セッションの Sonnet。task014・015 の後に行う（LoopDemo.cs を task015 が編集するため）。

## 目的

Quest 3 / 3S が手元に来たら、その場で迷わず受入確認を始められるようにする。あわせて、computer-use が使えない状況（2026-09-24 の見えない入力ウィンドウ）でも、画面の証拠を自動で撮れるようにする。

## (a) 自動開始オプション（実装 Sonnet）

- `LoopDemo` に起動引数 `--autostart` を追加。**desktop モードのときだけ**、開始できるようになった時点で1回だけ `Begin()` を呼ぶ。VR では無効（HMD 装着前に始まらないように）。
- 起動引数 `--autoescape`（テスト用、desktop のみ、`--autostart` と併用）: 周回2で t≈1秒に遮蔽、出口が開いたら脱出する。脱出の画面と記録の証拠を人の操作なしで撮るため。通常の起動や VR には影響しない。
- 既存の DemoRig の `--desktop` の読み方（`Environment.GetCommandLineArgs()`）に合わせる。

許可ファイル: `Assets/LoopRoom/Scripts/LoopDemo.cs`

### (a) 追修正（2026-09-24、独立レビュー指摘。計画担当 Opus 5.5）

Sol 高（採用）: `--autostart` が `rig.IsVR` だけで判定されており、`--desktop` なしで起動してローダー初期化に失敗し desktop fallback になった場合にも自動開始する。`--autostart`／`--autoescape` は **`--desktop` を明示した起動でだけ**有効にする。

## (b) Windows ビルド（計画担当）

- batchmode で `DemoSetup.Build`（Prepare → ConfigureXR → Validate → Development ビルド）を実行し、`Builds/Windows/LoopRoom.exe` を作る。Builds/ は Git に入れない（.gitignore を確認）。
- ビルドを `--desktop --autostart` で起動し、PrintWindow でプレイヤーのウィンドウだけを撮る（開始直後、足音の後、180秒超、`--autoescape` で脱出）。

## (c) 実機クイックチェック手順書（計画担当、Sonnet がレビュー）

- `Docs/DEVICE_QUICKCHECK.md`: 1ページ。Quest 3 / 3S 共通。Link の接続確認 → ビルドの起動 → 確認項目（床・頭・両手、Touch Plus 入力、3回の死亡と周回、遮蔽→脱出、観客画面の秘匿、VR 中のウィンドウ切替、Link を抜いて差す→R で再準備、案内パネルと文字の見え方、音の聞こえ方）→ 証拠の撮り方（Quest のスクリーンショット／PC 画面、セッションログの場所）→ 結果の記入欄。詳細は `Docs/UNITY_ACCEPTANCE.md` の章番号で参照する。
- 起動用の `Start-DeviceCheck.ps1`（ビルドの存在確認、Link 用の起動、ログフォルダを開く）。

## 受入条件

1. batchmode コンパイル エラー0・警告0、ビルド成功。
2. ビルドの desktop 自動実行のスクリーンショット（開始、周回、脱出）とセッションログを証拠に保存し、ユーザーに送る。
3. (a) のコードと (c) の手順書を Sol と別セッションの Sonnet がレビューし、両方 approve。
