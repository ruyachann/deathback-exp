# task008 独立レビュー: Sync-PublishedBranch.ps1 unborn HEAD 修正

対象SHA256（提供されたスナップショットより）:
- `Sync-PublishedBranch.ps1`: `82b58822b14b1fde0acdedd05cc4be58794313e03a314daa26ec4ca3a5ef9884`
- `Collaboration/tasks/008-fix-sync-published-branch.md`: `1e66e29e234a3cf75bbed47c593f0691190b0d05d907a0442fda3a4acffd03c9`
- `AGENTS.md`: `be53f607d20e3503473a167b094d52e1ac454328b09b0c7e64a3df382ae1ebd5`
- `CLAUDE.md`: `5e88123462781bc38ff4f6e797d6da67afe5d832a5abc9959b6c6ed4065e50f6`

## 分析

### 1. NativeCommandError回避（9〜16行目）
```powershell
$savedErrorActionPreference = $ErrorActionPreference
try {
    $ErrorActionPreference = 'Continue'
    & git -C $PSScriptRoot rev-parse --verify --quiet HEAD >$null 2>$null
    $headExitCode = $LASTEXITCODE
} finally {
    $ErrorActionPreference = $savedErrorActionPreference
}
```
- `2>$null` によるstderrリダイレクトがNativeCommandErrorを引き起こすのは `$ErrorActionPreference = 'Stop'` の場合のみ（Windows PowerShell 5.1の既知の挙動）。呼び出し直前に `'Continue'` へ切り替えているため、unborn HEADでgitが `fatal:` をstderrへ出しても例外にならない。
- `try/finally` で `$savedErrorActionPreference`（呼び出し前の値、実質 `'Stop'`）に確実に戻しており、以降の処理は元通り `Stop` で実行される。finally節に到達しない経路（PowerShellのプロセス強制終了など）は通常想定不要であり問題としない。
- `--verify --quiet` はunborn HEAD検出の標準的なイディオムで、`--quiet` 自体もstderr出力の抑制に寄与するため、リダイレクトとの二重対策になっている。冗長だが害はなく、「念のため」という計画方針（task008本文）に沿っている。
- 判定を `$headExitCode`（終了コードのみ）で行っており、標準出力・標準エラーの内容には依存していない。計画通り。

### 2. 拒否条件・後続手順
- 17行目: `$headExitCode -eq 0` で「Local history already exists…」により拒否。既存メッセージと文言一致。
- 18〜19行目: `ls-files --cached` によるindex非空チェックは変更なし。
- 20〜26行目: `fetch` → `show-ref --verify` → `switch -c` → `reset --mixed` → `set-upstream-to` → `status --short --branch` の順序・引数はタスク記載の「変更しない」方針通り維持されている。
- 7〜8行目のorigin URL確認（`https://github.com/ruyachann/deathback-exp.git` 固定チェック）も変更されていない。

### 3. PowerShell 7固有構文の有無
- `&&`/`||`（パイプラインチェーン演算子）、`??`（null合体）、三項演算子 `? :` など PowerShell 7 で追加された構文は使用されていない。
- `>$null`、`2>$null`、`try/finally`、`$LASTEXITCODE`、位置指定の型付きパラメータ (`[string[]]$Arguments`) はいずれも Windows PowerShell 5.1 で有効。

## Findings

| Severity | 箇所 | 内容 |
|---|---|---|
| Nit（低） | Sync-PublishedBranch.ps1:12 | `--quiet` と `$ErrorActionPreference='Continue'`+`2>$null` の二重対策は冗長（`--quiet` 単体でも `fatal:` 抑制が期待できる標準イディオム）。ただし計画書の「念のため」方針に沿った意図的な安全策であり、修正は不要。 |

重大な欠陥・保護の弱体化・PowerShell 7依存は見つからなかった。

## Verdict

**approve**

## 未検証事項（実行はしていない、コードレビューのみ）

- Windows PowerShell 5.1 の実環境で、unborn HEAD リポジトリに対し本スクリプトを実行し、実際に `NativeCommandError` が発生しないことの実機確認。
- 使用中のGitバージョンで `git rev-parse --verify --quiet HEAD` が実際にstderr出力を伴わないかどうかの実地確認。
- 受入条件1〜3（初回同期成功・作業ファイル保持・upstream設定／2回目実行時の拒否／index非空時の拒否）の実行確認。
- fetch時のネットワーク到達性・認証状態への依存。