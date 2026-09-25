# 008 — Sync-PublishedBranch.ps1 が unborn HEAD で必ず失敗する不具合の修正

状態: **受入済み（2026-09-24）**。結果は `reviews/task008-exchange-20260924.md`。計画確定（2026-09-24）。計画は Claude Opus 5.5（`claude-opus-5-5`）。実装は GPT-5.6 Sol（`codex exec` の別セッション）。レビューは実装とは別セッションの Sol と Claude Sonnet 5。

## 背景

2026-09-22 に Claude Sonnet 5 が発見（task006 DRAFT の「既知バグ」節）。Windows PowerShell 5.1 では、`$ErrorActionPreference = 'Stop'` のもとで外部コマンドの標準エラー出力を `2>$null` でリダイレクトすると、NativeCommandError として終了扱いになる。9行目の `git rev-parse --verify HEAD 2>$null` は、HEAD が無い（unborn）ときに git が標準エラーへ `fatal` を出すため、スクリプト本来の用途である「unborn HEAD からの初回同期」で必ず失敗する。

このリポジトリのローカル履歴は 2026-09-22 に手動で同期済みなので、今の正本では影響しない。新しい複製や別の PC で同期するときに必要になる。

## 修正方針

- 9-10行目の HEAD 有無の判定を、標準エラーを出さない形にする。`git rev-parse --verify --quiet HEAD` は HEAD が無いとき何も出力せず終了コード 1 を返す。
- 念のため、その呼び出しの間だけ `$ErrorActionPreference` を `'Continue'` にし、`try/finally` で元に戻す。標準出力・標準エラーは捨てる。
- 判定結果は終了コードだけで決める（HEAD がある = 0 のとき、従来どおり 'Local history already exists…' で拒否）。
- それ以外の手順（origin の確認、index が空の確認、fetch、switch -c、reset --mixed、upstream 設定）と各メッセージは変更しない。

## 許可ファイル

- `Sync-PublishedBranch.ps1` のみ。

## 受入条件（Windows PowerShell 5.1 で実行する）

作業用の一時リポジトリ（正本とは別の場所）で確認する。正本の `.git` には触れない。
1. `git init` 直後、origin を正本と同じ URL にした unborn リポジトリで、スクリプトが最後まで成功する。ブランチ `codex/stage-01-looproom` が作られ、upstream が `origin/codex/stage-01-looproom`、作業ファイルは消えない（事前に置いたダミーファイルが残る）。
2. 同期後にもう一度実行すると、'Local history already exists…' で拒否し、何も変更しない。
3. index にファイルがある unborn リポジトリでは 'Index is not empty…' で拒否する。
4. 実装とは別セッションの Sol と Sonnet が同じ SHA256 で独立レビューし、交換後に両方 approve。

## リスク

- fetch は GitHub へのネットワーク読み取りを伴う（公開リポジトリの取得のみ。push はしない）。
