# 独立レビュー（task009: peer_review_mcp.py の Codex 呼出し不具合修正）

## 対象と参照SHA256（供給値）
- `Tools/peer_review_mcp.py`: `20ecfd09a671f1e9bc90a64f5e216ab1dcd63c71bf5730bcab7784c14c6bbe52`
- `Collaboration/tasks/009-fix-peer-review-codex-invocation.md`: `26ea140d0760bf8d22584ff643d73653af661a9db4861d6f6ff5919c47391976`
- `AGENTS.md`: `be53f607d20e3503473a167b094d52e1ac454328b09b0c7e64a3df382ae1ebd5`
- `CLAUDE.md`: `5e88123462781bc38ff4f6e797d6da67afe5d832a5abc9959b6c6ed4065e50f6`

## 検証結果（FOCUS項目ごと）

### 1. Codex実行ファイル解決順（CODEX_CLI_PATH → which → LOCALAPPDATA最新codex.exe）
`Tools/peer_review_mcp.py` の `reviewer_executable`（20ecfd09…hb52 中、`configured = os.environ.get("CODEX_CLI_PATH")` 以下）はタスク記載の3段階解決を実装している。
1. `CODEX_CLI_PATH` が存在するファイルなら採用
2. `shutil.which("codex")`
3. Windowsのみ `%LOCALAPPDATA%\OpenAI\Codex\bin\*\codex.exe` から `st_mtime` 最大のものを選択

いずれも見つからなければ `None` を返し、呼出し元 `run_review` が `RuntimeError(f"{reviewer} CLI is unavailable")` を送出する。タスク記載の解決順・失敗時挙動と一致しており欠陥なし。

### 2. `--ask-for-approval` の位置
実装のCodexコマンドは
```
[executable, "--ask-for-approval", "never", "exec", "--model", model, "--sandbox", "read-only", "--ephemeral", "-c", "mcp_servers.peer_claude.enabled=false", "-C", str(ROOT), "-"]
```
であり、タスク記載の「`exec` の前にトップレベル引数として置く」方針と一致する。`-C` にはPathではなく `str(ROOT)` を渡しており、subprocessへの引数として妥当。欠陥なし。

### 3. `reviewer_executable` の記録
`metadata["reviewer_executable"] = executable` として `status.json` に記録されている（受入条件と一致）。ただし、この値には実行環境のユーザー名を含むローカルパス（例: `C:\Users\<user>\...`）が含まれ得る。AGENTS.mdの「認証情報や個人の設定を報告に含めない」との方針とは目的が異なる（実行バイナリの所在証跡なのでタスク要求に沿っている）が、記録内容に個人環境依存パスが残る点は運用上留意すべき（Severity: Info）。

### 4. Claude側の不変
`shutil.which("claude")` はそのまま。`command = [executable, "-p", "--model", model, "--safe-mode", "--no-session-persistence", "--tools", "", "--output-format", "json"]` も変更の痕跡なし。タスク要求「Claude 側の不変」と矛盾しない。

### 5. プロンプト・スナップショット・SHA256・PAUSE判定・テーブル定義
- PAUSE判定: `(ROOT / "Collaboration" / "PAUSE.json").exists()` で存在。
- SHA256記録: `snapshot()` 内 `hashlib.sha256(raw).hexdigest()` で存在。
- 出力先: `RUNS / f"{stamp}-mcp-{reviewer}-review"` で存在。
- ツール定義: `tool_name = "review_with_claude" if reviewer == "claude" else "review_with_codex"` で存在。
いずれも機能としては現行コード内に確認できるが、**修正前バージョンとのdiffは供給されていないため「変更されていないこと」自体は比較検証できない**（後述の未確認事項）。

### 6. CODEX_CLI_PATH検証の甘さ／globの範囲
- `Path(configured).is_file()` は存在確認のみで、実体がCodex CLIかどうか・実行権限があるかは検証しない。無効な場合は警告なく次の解決手段へ黙って フォールバックする。ただしこれはタスク受入条件2「存在しないパスでも2〜3番目の方法で見つかる」に明示的に整合する仕様であり、欠陥ではない（Severity: Info — ログ等でCODEX_CLI_PATH不採用を明示すればUX向上するが必須ではない）。
- `glob("*/codex.exe")` は `bin` 直下1階層のみを探索し、タスク記載のパス構造（`bin\<ハッシュ>\codex.exe`）と一致。`bin` が存在しない場合も例外を投げず空candidatesとなり `None` を正しく返す。範囲の過不足は確認できず。

### 7. その他
- `configured` が相対パスの場合、`Path(configured).is_file()` はPythonプロセスの現在の作業ディレクトリを基準に判定される点に注意（Severity: Info、実運用では絶対パス設定が前提のため影響小）。
- subprocess呼出しは `shell=False` かつ引数リスト形式であり、コマンドインジェクションのリスクはない。

## Findings まとめ

| 重大度 | 箇所 | トリガー | 修正案 |
|---|---|---|---|
| Info | `Tools/peer_review_mcp.py` `reviewer_executable`（CODEX_CLI_PATH分岐） | CODEX_CLI_PATHが無効な値でも警告なくフォールバック | 必須ではないが、フォールバック発生時にログを残すと運用しやすい |
| Info | `Tools/peer_review_mcp.py` `status.json` の `reviewer_executable` | 実行環境のユーザー名を含むパスが記録される | 気にするなら相対パス化や末尾のみ記録を検討（受入条件上は現状で可） |
| Info | `Tools/peer_review_mcp.py` `reviewer_executable`（CODEX_CLI_PATH分岐） | 相対パス指定時、判定基準cwdが実行時cwdと異なりうる | 絶対パス前提であることをタスク/ドキュメントに明記 |

Critical/High/Mediumレベルの欠陥は確認できなかった。

## Verdict: approve

## 未確認事項（unverified checks）
- 実際に `codex exec` を起動してのエンドツーエンド動作（受入条件1）は未実施（本レビューはコード解析のみで、テスト実行やCLI呼出しは行っていない）。
- `CODEX_CLI_PATH` に存在しないパスを設定した際の実機フォールバック動作（受入条件2）は未検証。
- `--reviewer claude` 側の `tools/list` 応答（受入条件3）は未検証。
- 修正前バージョンとのdiffが供給されていないため、「プロンプト・スナップショット・SHA256記録・PAUSE判定・出力先・ツール定義を変えない」という制約が本当に無変更かは、供給された単一スナップショットのみでは断定できない。
- 別セッションのSol側独立レビューとの突合は本レビュー実施前に読んでいない（指示どおり）。