# 独立レビュー（task016：実機確認準備）

対象タスク: `Collaboration/tasks/016-device-check-readiness.md` (SHA256: `a3006e4adda7d39d7928ee2ec91718ac0936d9818e50af3dcae1291f54754a43`)
対象ソース: `Assets/LoopRoom/Scripts/LoopDemo.cs` (SHA256: `2a85b749348d7cc179bd8602982a52c7bff1f5cc972f086dc6a6c02f9295c516`)、`Docs/DEVICE_QUICKCHECK.md` (SHA256: `2f30798e303fabb1ab3f09a9b037d8565f2a33c4bad963d74c4447e755cfae84`)、`Start-DeviceCheck.ps1` (SHA256: `fe3e795893136a04430febf8de11a181dc7420365f61b071b30e70227b3ecfb1`)
参照専用: `Assets/LoopRoom/Scripts/DemoRig.cs` (SHA256: `793caa1e50c3efd52d48f278cc8043598dddb7d68833d395e2e885b737730bbc`)、`Docs/UNITY_ACCEPTANCE.md` (SHA256: `b1b1f081ae69374df6eafabccabc8d90b571d03fbfb26d1747bcc4f5cbd7e9e1`)

※対象ファイル内のコメント等に本レビュー手順の変更を促す記述は無いが、念のため確認したうえで手順に沿ってレビューする。

---

## 指摘事項

### [高] `Start-DeviceCheck.ps1`：Git 不在環境でスクリプト全体がクラッシュしビルド起動に到達しない

該当箇所:
```powershell
$ErrorActionPreference = 'Stop'
...
"...commit=$(& git -C $PSScriptRoot rev-parse --short HEAD 2>$null)" |
    Add-Content -LiteralPath (Join-Path $note 'session-notes.txt') -Encoding utf8
$playerArgs = @(); if($Desktop) { $playerArgs += '--desktop' }
if($playerArgs) { Start-Process -FilePath $exe -ArgumentList $playerArgs } else { Start-Process -FilePath $exe }
```

**発生条件**: 実機チェックを行う PC に `git` がインストールされていない、または PATH に無い場合。`git` コマンド自体が見つからないケースは PowerShell の `CommandNotFoundException`（コマンド解決時点のエラー）であり、`2>$null` は標準エラー出力のリダイレクトにすぎず、このエラーは抑止できない。`$ErrorActionPreference = 'Stop'` の有無に関わらずこの種のエラーは terminating となり、以降の `Start-Process`（ビルド起動）に到達せず、`Start-DeviceCheck.ps1` 全体が失敗する。task016 の目的は「Quest が来たらその場で迷わず受入確認を始められること」だが、これは実機確認の最初のステップ自体を止めうる欠陥。

**修正案**: `git` 呼び出しを `try { ... } catch { $commit = 'unknown' }` で囲む、または `Get-Command git -ErrorAction SilentlyContinue` で存在確認してから実行する。

### [中] `Docs/DEVICE_QUICKCHECK.md`：`UNITY_ACCEPTANCE.md` への章番号参照が一部不正確

- 「2. 遊ぶ（→ 第4・5章）」の項目5〜8（何もしないと t=6 で撃たれる／遮蔽で防げる／脱出できる）は、`UNITY_ACCEPTANCE.md` 第4章「Play の 3 回開始/停止（**Unity Editor**）」の内容（Editor限定の起動・停止手順）とは対応しない。第5章は「3回の死亡ループとXR原点非再割当」で項目9とは対応するが、項目5〜8に該当する合否基準を持つ章が `UNITY_ACCEPTANCE.md` に存在しない。
- 「3. 運営まわり（→ 第6・7章）」の項目10（周回中に別ウィンドウをクリックしてもセッション継続）は、第6章「追跡中断」（`HeadTracked`/`RuntimePresent()` の喪失）の内容とは別の概念（`LoopDemo.cs` の `!rig.IsVR && !Application.isFocused` 条件によるフォーカス喪失時の中断／非中断）であり、対応章が無い。
- 「4. 見え方と聞こえ方（→ 第2章 手順5）」の項目17（音量バランス）は、第2章手順5に音声に関する記述が無く参照が不適切。

**修正案**: 該当する章が無い項目は「本書独自の確認項目」と明記するか、`UNITY_ACCEPTANCE.md` 側に対応する手順・合否基準を追記する。レビュー focus に明示された「独立番号の参照が正しいか」を問われた点として重要度は中とした。

### [低〜中] `Docs/DEVICE_QUICKCHECK.md`：PowerShell 実行ポリシーに関する案内が無い

手順0-4で `.\Start-DeviceCheck.ps1` の実行を指示しているが、Windows 既定の実行ポリシー（`Restricted`）下ではスクリプト実行がブロックされうる。この対処（`Set-ExecutionPolicy` や `-ExecutionPolicy Bypass` 起動など）が「困ったとき」節を含めどこにも無い。運営担当が開発機以外の PC で初回実行する場面を想定すると、初動でつまずく典型例。

### [低] `Start-DeviceCheck.ps1`：ビルドが無い場合の代替コマンド例に `-logFile` が無い

```
& 'C:\Program Files\Unity\Hub\Editor\6000.3.15f1\Editor\Unity.exe' -batchmode -quit -projectPath '$PSScriptRoot' -executeMethod DemoSetup.Build
```
`-logFile` の指定が無く、batchmode 実行時にビルド失敗の原因診断がしづらい。Unity バージョン（`6000.3.15f1`）とメニュー名は `UNITY_ACCEPTANCE.md` 前提条件（0章）と一致しており、その点は整合している。

### [低] `Docs/DEVICE_QUICKCHECK.md`：「Quest 3S は特に」の節に 3S 固有の記述が実質無い

項目14〜17（パネル文字、机の視認性、明るさ・霧・ブルーム、音量バランス）は Quest 3/3S 共通の一般的な確認項目であり、`UNITY_ACCEPTANCE.md` 第2章手順5が言及する解像度・視野角・レンズ差に起因する Quest 3S 固有の観点（例：視野の端の切れ方の違い）が具体化されていない。

### [低・未検証] `Assets/LoopRoom/Scripts/LoopDemo.cs`：`--autostart` 単独使用（`--desktop` 併用なし）時の仕様が未文書化

該当箇所:
```csharp
autostart = Array.IndexOf(args, "--autostart") >= 0;
...
bool autoTrigger = autostart && !autostartUsed && !rig.IsVR;
```
`--autostart` は `rig.IsVR` のみでガードされ `--desktop` フラグとは独立している。`DemoRig.cs` を確認すると、HMD 未接続時は `CanStart`（`forceDesktop || desktopFallback`）が false のままなので誤発火はしない設計にはなっているが（`StartXR()` のタイムアウト分岐では `desktopFallback` がセットされないため）、task016 本文・(b) は常に `--desktop --autostart` の組み合わせのみを想定しており、単独使用時の意図した挙動がタスクにもコードコメントにも明記されていない。実害は無いと判断するが、ランタイム挙動（`LoopModel`/`DemoRig` の実際の状態遷移）は静的レビューでは確定できないため未検証とする。

### [低] `LoopDemo.cs`：`autoTrigger` と `R` キー処理の排他

```csharp
if (idle && rig.CanStart && (enter || (rig.IsVR && rig.StartPressed) || autoTrigger)) { ... Begin(); }
else if (idle && keyboard!=null && keyboard.rKey.wasPressedThisFrame && rig.CanRetryPreparation) rig.RetryPreparation();
```
自動開始条件が成立した同一フレームで `R` を押しても `RetryPreparation()` は呼ばれない（`else if`）。自動起動シナリオで人手が同時に `R` を押す実運用は考えにくく実害は小さいが、「既存の Enter/R の操作を壊さない」という受入条件に対する理論上のエッジケースとして記録する。

---

## 良好な点（欠陥ではない確認事項）

- `autostart`/`autoescape` はフィールド既定値 `false` かつ `autostartUsed` により1回のみ発火する設計で、周回2固定・t≈1秒遮蔽・出口開放後即脱出という仕様と実装が一致している。
- `Start-DeviceCheck.ps1` の証跡フォルダ命名（`Collaboration/evidence/<日付>-device-check`）は `Docs/DEVICE_QUICKCHECK.md` の記述および `AGENTS.md` の証跡規約と一致している。
- `Start-DeviceCheck.ps1` のビルド未検出時のメニュー名・Unity バージョンは `UNITY_ACCEPTANCE.md` 記載の実在メニューと一致している。

---

## Verdict: **request_changes**

`Start-DeviceCheck.ps1` の Git 依存によるクラッシュ可能性（実機確認の初動を止めうる）と、`Docs/DEVICE_QUICKCHECK.md` の `UNITY_ACCEPTANCE.md` 章番号参照の不整合（focus で明示的に問われた検証項目）が是正対象と判断する。`LoopDemo.cs` の (a) 実装自体は大きな欠陥は見当たらないが、`--desktop` 併用前提の明記不足は軽微な文書課題として残す。

## 未確認事項

- `Assets/LoopRoom/Editor/DemoSetup.cs`（`DemoSetup.Build` の実装）は本レビューでは未提供のため、`Start-DeviceCheck.ps1` が案内する batchmode コマンドが実際に成功するかは未検証。
- `LoopRules`/`LoopModel`（`Model.LoopId`、`Model.ExitAvailable`、`Model.LoopTime` の実装）は未提供のため、`autoescape` のタイミング（t≈1秒遮蔽、出口開放検知）が実行時に仕様通り動くかは未検証。
- Unity Editor でのコンパイル（エラー0・警告0）、実際のビルド成功、Quest 3/3S 実機での動作・証拠画像は本レビューでは実施していない（コード上の静的レビューのみ）。
- `Start-DeviceCheck.ps1` を実際の Windows PowerShell 5.1 環境で実行した結果（特に Git 不在時の挙動）は未検証。