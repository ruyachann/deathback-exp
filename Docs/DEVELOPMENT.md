# 開発者向け運用ドキュメント

本書は LoopRoom（第零室）プロジェクトにおける開発・共同作業運用のルールをまとめたものである。詳細な役割分担・不変条件は `AGENTS.md`、段階計画は `Docs/Plans/STAGE_PLAN.md` を参照。

## 1. 正本（canonical root）とディレクトリの扱い

- 新正本は `C:\deathback\deathback-exp` である。旧 `outputs/LoopRoomUnity` は移行元の保存版であり、新正本の実機検証済みとして扱ってはならない。
- ユーザーが旧 Unity プロジェクトを開いている場合、そのユーザー編集を保護し、上書きしない。
- ユーザーが Unity を開いている間は、同一プロジェクトを別の Editor インスタンスで開かない。
- `Library/` `Temp/` `PackageCache/` は変更禁止。依存関係の追加・削除・更新は Unity Package Manager の `UnityEditor.PackageManager.Client` API を使用し、manifest と lockfile の解決は Unity に行わせる。バージョン検出・提案と実際の更新を区別し、自動適用しない。
- task001の管理スクリプトは過去の限定されたmanifest変更の証跡であり、今後の依存更新の一般的な実行方法にはしない。

## 2. ブランチ運用とドラフト PR

- 変更は作業ブランチ上で commit / push する。`main` へ直接 push しない。
- Issue に紐づくドラフト PR を作成する。
- マージはユーザーの明示的な指示があるまで行わない。
- 既存の commit 履歴・ブランチを保護する。force-push、履歴の書き換え（amend/rebase の公開後実行）は行わない。

## 3. AI モデル分担の概要

- 通常の計画・実装・確認は GPT-5.6 Sol が担当する。
- 相互レビューは、実装者とは別セッションの Sol と Claude Sonnet 5 が独立して行う（同一 SHA のソースを個別にレビューし、結果を交換する）。
- 重要判断・行き詰まりの相談は Opus 5 を優先し、Opus が利用不可能と確認された場合に限り GPT-6 Astra へ引き継ぐ。
- モデルの黙った切替は禁止。誰がどのモデルで何を行ったかを `Collaboration/STATE.md` 等に記録する。

## 4. 利用枠の管理：Codex（共有）と Claude（別枠）

- Codex の 5 時間利用枠はアカウント共通であり、他タスクとの同時消費を考慮する必要がある。
- Claude の利用枠は Codex とは別に記録・管理する。API 課金額から Claude の残枠を推定してはならない（課金体系が異なるため不正確）。
- 残量が取得できない場合は、推定で作業を続けず、ユーザーへ確認する。

## 5. 残量 10% 未満時のポーズ／引き継ぎ

- いずれかの枠（Codex／Claude）の残量が 10% を下回った時点で、新規の AI モデル呼び出し（実装・レビュー含む）を停止する。
- 停止時点で以下を保存する：
  - 現在の進捗（完了した作業、未完了の作業）
  - 対象コミット SHA（またはレビュー対象のソースハッシュ）
  - 残課題一覧
  - 再開時に実行すべきコマンド／手順
- 保存先は `Collaboration/STATE.md`（および必要に応じて `Collaboration/PAUSE.json`）とする。
- `Collaboration/PAUSE.json` が存在する間は、ユーザーの明示的な再開指示があるまで新しいモデル呼び出し・実装を開始しない。再開時は `Collaboration/RESUME.md` を読み、利用枠と保存版の状態を確認してから再開する。

## 6. 行き詰まり時のエスカレーション：Opus 5 → Astra

- 対象となるのは、体験の核・活動範囲・身体位置の扱い・安全性・システム構成に関わる重要判断、または原因調査や最大 2 案の修正案でも Sol/Sonnet の見解が解消しない行き詰まりに限る。通常のレビューや最終報告のたびに毎回呼び出すものではない。
- Opus 5 を呼び出す前に、正確なモデル ID と利用権限を確認する。**未確認のモデルを「呼出し済み」として扱ってはならない**（例：正式なモデル ID や利用可否が未検証のまま Opus 5 に相談したと記録することは不可）。
- Opus 5 が利用不可能であると確認できた場合に限り、その理由を記録したうえで GPT-6 Astra へ引き継ぐ。Astra を通常時の代替実装モデルとして扱わない。
- 相談時は、問題・試したこと・双方（Sol/Sonnet）の見解・選択肢・判断してほしい点のみを整理して渡し、他の設計/レビュー作業を混在させない。

## 7. 認証 401 とネットワーク到達不可の切り分け

- Claude との連携を試みる前に `Collaboration/history/SONNET_401_ROOT_CAUSE.md` と `CLAUDE_CONNECTION_CHECK.md` を確認する。
- エラーが発生した場合、**401（認証拒否）** と **ネットワーク到達不可／通信拒否** を区別して記録する。両者は原因と対処が異なるため、どちらか一方に決め打ちで対処しない。
- いずれの場合も、権限設定やプロキシ設定を迂回する対処（認証回避、プロキシバイパス等）は行わない。
- 認証情報・トークン・個人設定は、報告書やコミット、push 対象に含めない。

## 8. 生ログの扱い

- 認証エラーや接続診断で得られる生ログ（トークン、内部ネットワーク情報、個人環境固有の情報を含みうるもの）は、Issue／PR／Collaboration 配下のドキュメントに**そのまま含めない**。
- 必要な場合は、機微情報を除いた要約（エラー種別、発生時刻、再現手順）のみを記録する。

## 9. 実行手順（Unity 側）の参照

- Unity プロジェクトの準備・XR 設定・検証・ビルドは `Assets/LoopRoom/Editor/DemoSetup.cs` の以下のメニューのみを使用する。新規の UI コマンドを創作しない。
  - `LoopRoom/1 - Prepare project and scene`
  - `LoopRoom/2 - Configure Quest Link OpenXR`
  - `LoopRoom/3 - Validate settings and model`
  - `LoopRoom/4 - Build Windows demo`
  - `LoopRoom/Run model checks only`
- `LoopRoom/1` の実行前に未保存のユーザー編集を保存・保護する。保存確認をキャンセルした場合は準備を中止する。既存の `Assets/LoopRoom/Scenes/LoopRoom.unity` は開かれ、このシーンがない場合だけ新規作成される。
- `LoopRoom/3` はモデルチェックと OpenXR ローダー設定の検証のみを行い、実機（HMD）挙動は検証しない。実機受入は `Docs/UNITY_ACCEPTANCE.md` の手順に従う。

## 10. 起動オプション（確認・証拠撮影用、2026-09-25 時点）

`Builds/Windows/LoopRoom.exe` に付ける。`--desktop` 以外はすべて **`--desktop` と併用したときだけ**有効（VR では効かない）。

| オプション | 働き | 導入 |
| --- | --- | --- |
| `--desktop` | XR を起動せず PC 画面で動かす（頭＝カメラ、右ドラッグで視点） | 初期 |
| `--auto-calibrate` | キャリブレーション画面で、スプラッシュ終了後3秒待ち、1秒かけて長押しを模擬して決定（リングが伸びる） | task021・027 |
| `--autostart` | 開始待ちになったら自動で開始。フォーカスを失っても中断しない | 初期・task022 |
| `--autoescape` | `--autostart` と併用。2周目で遮蔽を上げ、出口が開いたら脱出してセッションを終える | 初期 |
| `--simulate-drift` | 周回ごとに決まったずれ（位置と向き）を頭に加え、部屋の置き直しと向きの補正を確かめる | task018 |
| `--desktop-pitch <度>` | キャリブレーション後の desktop カメラの下向き角（-70〜70、既定 0）。床や机を撮るとき | task025 |

撮影の例（PowerShell）:

```powershell
.\Builds\Windows\LoopRoom.exe --desktop --auto-calibrate --autostart --autoescape -screen-width 1280 -screen-height 720 -screen-fullscreen 0
```

- セッションの記録: `%USERPROFILE%\AppData\LocalLow\LoopRoomDemo\The Room Before\Sessions\*.json`（`frames` にフレーム時間。task026）。`Player.log` は一つ上のフォルダ。**Player.log には PC 名や IP が入ることがあるので、そのままコミットしない**（必要な行だけ抜き出す）。
- 体験空間の設定: 同じフォルダの `play-area.json`（task020）。
- 画面の取り込み: 起動直後（スプラッシュとその後約1.5秒）は取り込みに映らない。ウィンドウが最小化されたら元に戻してから取り込む。
- ビルド後は設定ファイルの副作用を戻す（`Assets/Settings/*.asset` の3つ、`ProjectSettings/ProjectSettings.asset`・`UnityConnectSettings.asset` を restore、`Assets/XR/Settings/OpenXR Editor Settings.asset(.meta)` を削除）。
