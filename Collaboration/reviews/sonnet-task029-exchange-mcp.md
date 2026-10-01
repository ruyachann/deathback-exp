# task029 独立レビュー（交渉後の再レビュー）

## 手順の確認
まず自分自身で diff（`Collaboration/reviews/task029-diff.md`）とタスク文書（`Collaboration/tasks/029-build-info.md`）のみに基づき独立した欠陥分析を行い、判定を確定した。その後に Sol・Sonnet 両方の独立レビュー（いずれも approve）を読み合わせ、自分の判定を覆す指摘がないか照合した。

対象ソース（提示 SHA256）:
- `Assets/LoopRoom/Editor/DemoSetup.cs`: `4fb91b575a2c4a6810bccd869e081c3039e6afc18d97af8988131a7bc807936c`
- `Start-DeviceCheck.ps1`: `eba60e9849cbece469d6f05eea7c45244818125529889245df1fd2ad2128592d`

※ 両ファイルの全文はスナップショットに含まれておらず、diff とタスク文書のみで判定している。SHA256 の再計算は自分でも行っていない。

## 独立分析の所見

### 1. severity: 低 — `TryRunGit` の stdout/stderr 順次読み取り
`DemoSetup.cs`（diff内 `TryRunGit`）
```csharp
output=process.StandardOutput.ReadToEnd();
var error=process.StandardError.ReadToEnd();
process.WaitForExit();
```
**トリガー**: `git status` の出力が OS パイプバッファを超える場合（大量の untracked/変更ファイル）、標準出力を読み切る前に標準エラーへの書き込みでブロックし、古典的な .NET Process デッドロックパターンに該当しうる。`--untracked-files=all` 指定のため、`node_modules` 相当の大量未追跡ファイルがあると理論上のリスクが現実化しうる。
**修正案**: `OutputDataReceived`/`ErrorDataReceived` の非同期イベント購読、または `Task.WhenAll` で両ストリームを並行に読む。

### 2. severity: 情報 — commit 取得失敗時に dirty 判定が丸ごとスキップされる
`CaptureBuildInfo()` は `TryRunGit("rev-parse HEAD", ...)` の `started` が false（プロセス起動自体に失敗、= git 未インストール等）の場合のみ `git status` をスキップし、`dirty=false` のままにする。これは要件（git不在でもビルドを失敗させない）を満たしており blocking ではないが、`dirty=false` が「実際にクリーン」か「未検証」かを build-info.json 単体では区別できない。将来的な改善余地として記録するに留まる。

### 3. severity: 軽微 — `Start-DeviceCheck.ps1` 冒頭への UTF-8 BOM 追加
`-param(...)` → `+﻿param(...)`。機能への影響はないが、意図した変更か保存時の副作用か diff からは判別できない。

### 4. `ParseDirtyFiles` / dirty 判定タイミング
rename/copy エントリ（新パス→旧パスの順）のスキップ処理、境界チェック（`i+1<fields.Length`）は git porcelain `-z` 形式と整合しており欠陥は見当たらない。`CaptureBuildInfo()` を `Prepare(); ConfigureXR(); Validate();` より前で呼ぶ構成は、task029.md 記載の「追修正」要件（Prepare の副作用を dirty に数えない）を満たしている。

上記いずれも blocking な欠陥ではなく、自分の独立判定は **approve**。

## 両者のレビューとの照合

- **Sol**: 具体的欠陥なし、approve。挙げた確認事項（Prepare 前呼び出し、パス限定、20件上限、git 不在時のフォールバック）は自分の分析と一致。
- **Sonnet**: 自分と同じ「`TryRunGit` の ReadToEnd 順次読み取りによるデッドロックリスク（severity低）」「BOM追加（severity軽微）」「dirty の tri-state 欠如（severity情報）」を指摘し、いずれも受入条件外の改善余地として approve。

両者の指摘内容は自分の独立分析と重なっており、新たに blocking と判断すべき論点は見当たらない。したがって、両者のレビューを読み合わせても自分の判定を変更する必要はない。**双方の approve に加えて自分の approve を要するものはない**（三者とも同一の軽微な観測点に収束しており、追加の request_changes 事由は無い）。

## 未確認事項

1. `DemoSetup.cs` / `Start-DeviceCheck.ps1` の全文が提供されていないため、記載 SHA256 の再計算・照合は未実施。
2. `Prepare()` / `ConfigureXR()` / `Validate()` の本体実装は未提示のため、`ProjectSettings.asset` 書き換えの有無を直接確認できていない。
3. Unity batchmode での実ビルド、build-info.json の実ファイル生成、`Start-DeviceCheck.ps1` の実起動（desktop/実機）は未実行・未確認。
4. 大量差分時の `TryRunGit` デッドロックリスクは実測していない。
5. `dirtyFiles` が21件以上の場合に20件で打ち切られる挙動の実測は未実施。

## 判定

**approve**