# task029 独立レビュー（DemoSetup.Build の build-info.json / Start-DeviceCheck.ps1）

対象ファイルと提示された SHA256（`Collaboration/reviews/task029-diff.md` 内記載）:
- `Assets/LoopRoom/Editor/DemoSetup.cs`: `4fb91b575a2c4a6810bccd869e081c3039e6afc18d97af8988131a7bc807936c`
- `Start-DeviceCheck.ps1`: `eba60e9849cbece469d6f05eea7c45244818125529889245df1fd2ad2128592d`

※ 提供されたのは diff とタスク文書、evidence テキストのみで、両ファイルの全文（sha256 が一致する完全な内容）は snapshot に含まれていない。以下は diff とタスク要件・evidence の記述のみに基づく判断であり、実行・再計算は行っていない。

## 所見

### 1. CaptureBuildInfo() の呼び出し位置（要件どおり）— 問題なし
`Build()` 冒頭で `var buildInfo=CaptureBuildInfo();` を `Prepare(); ConfigureXR(); Validate();` より前に呼んでいる（task029.md の「追修正」の指示通り）。これにより Prepare が `ProjectSettings/ProjectSettings.asset` 等を書き換える前提でも、dirty 判定はビルド開始時点のスナップショットになる。設計上の要件は満たされている。

### 2. severity: 低 — `TryRunGit` の標準出力/エラー読み取り順序
`Assets/LoopRoom/Editor/DemoSetup.cs`（diff 内 `TryRunGit`、新ファイルで概ね170行目付近）
```csharp
output=process.StandardOutput.ReadToEnd();
var error=process.StandardError.ReadToEnd();
process.WaitForExit();
```
**トリガー**: `git status` の出力（または stderr）が OS のパイプバッファを超えるほど大きい場合、`ReadToEnd()` を順番に呼ぶ古典的な .NET Process のデッドロックパターンに該当する。通常のプロジェクト規模の差分では発生しにくいが、理論上のリスクとして残る。
**修正案**: `OutputDataReceived`/`ErrorDataReceived` の非同期読み取りに変更するか、`Task.WhenAll` で両ストリームを並行に読む。

### 3. severity: 情報 — commit 取得失敗時の dirty 判定のフォールバック
`CaptureBuildInfo()` は `TryRunGit("rev-parse HEAD",...)` が「プロセス起動に失敗」（git 未インストール等）した場合のみ `gitStarted=false` となり、その後の `git status` 呼び出しをスキップして `dirty=false` のままになる。これは「git が使えない場合はビルドを失敗させない」という要件を満たしており、バグではないが、`dirty` が「実際に未検証」なのか「本当にクリーン」なのかを build-info.json 単体では区別できない。将来的に `dirty` を tri-state（true/false/unknown）にする改善余地はあるが、今回の受入条件には含まれていないため blocking ではない。

### 4. severity: 軽微 — `Start-DeviceCheck.ps1` 先頭に BOM 追加
diff の1行目 `-param(...)` → `+﻿param(...)` で UTF-8 BOM が追加されている。機能への影響はないが、意図した変更か保存時の副作用か diff からは判別できない。

### 5. dirty 対象パスの絞り込みは要件通り
`git status --porcelain=v1 -z --untracked-files=all -- Assets ProjectSettings Packages` で対象を Assets/ProjectSettings/Packages に限定しており、evidence の実物 JSON でも `Collaboration/` 配下や `Start-DeviceCheck.ps1` の変更（現在の git status に存在する）が `dirtyFiles` に含まれていない。これは絞り込みが機能している間接証拠になる。

### 6. `ParseDirtyFiles` のロジック
rename/copy エントリ（`XY PATH\0ORIG_PATH\0`、新パス優先）を想定して次フィールドをスキップする実装は git の porcelain -z フォーマットと整合しており、境界チェック（`i+1<fields.Length`）もあるため配列外アクセスは起きない。目立った欠陥は見当たらない。

## Focus 項目ごとの確認結果

- **ビルドで build-info.json ができるか / Prepare の副作用が数えられないか（修正後）**: diff 上は要件通りに実装されている。evidence の `Builds/Windows/build-info.json`（実物）の `dirtyFiles` が `DemoSetup.cs` と `LoopDemo.cs` のみで、Prepare が触れるはずの `ProjectSettings/ProjectSettings.asset` 等が含まれていないことから、Prepare 前捕捉が機能している可能性を示す間接証拠はあるが、Prepare() 自体の実装全文が snapshot に無く直接確認はできない。
- **3つの警告（なし/dirty/HEAD不一致）**: `device-check-display.txt` に3ケースとも記載を確認した。ただし全て「検証用コピー（起動・フォルダ展開は省略）」であり、実機や Desktop モードでの実行結果ではなく、PowerShell ロジックの単体的な検証にとどまる。

## 未確認事項（unverified checks）

1. `DemoSetup.cs` / `Start-DeviceCheck.ps1` の全文が提供されていないため、記載された SHA256 の再計算・照合はできない。
2. `Prepare()` / `ConfigureXR()` / `Validate()` の実装本体が snapshot に含まれておらず、実際に `ProjectSettings.asset` を書き換えるかどうかを直接確認できていない。
3. Unity batchmode ビルドの実行、build-info.json 生成、Start-DeviceCheck.ps1 の実起動は自分では実行していない（禁止事項のとおり実行もしていない）。evidence の記述内容を字面のみで確認した。
4. 大量差分時の `TryRunGit` デッドロックリスクは実測していない。

## 判定

**approve**

設計要件（Prepare 前の情報取得、対象パス限定、最大20件、git 不在時の安全側フォールバック、3警告）は diff 上で満たされており、ブロッキングな欠陥は見当たらない。上記の低〜軽微な指摘（デッドロック耐性、BOM追加）は次回以降の改善事項として記録するのみで、本タスクの受入を妨げるものではない。