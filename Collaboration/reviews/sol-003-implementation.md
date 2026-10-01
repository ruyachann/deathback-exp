# 003 — Sol実装報告

実装担当セッション: `sol_timing_implementation`。これは実装者による報告であり、独立レビューではない。

再開後の検証日時: 2026-09-17T00:53:44.2810838Z（JST 2026-09-17 09:53）。停止前から対象2ファイルのSHA256は変わっておらず、再開後の再編集は不要だった。

## 変更内容と範囲

`LoopRules.Validate()` の先頭で7項目すべてのNaN・正Infinity・負Infinityを `double.IsNaN` / `double.IsInfinity` により拒否する。既存の例外型・メッセージを使用し、後続の大小比較、既定値、状態遷移、モデル時刻、イベント名には変更を加えていない。新依存はない。

既存の10チェックは変更せず、同じ `Check` 形式で1チェックを追加した。7項目×3種類の21ケースそれぞれに新しい `LoopRules` を作成し、`Validate()` が `ArgumentException` を投げることを確認する。

この実装担当が書き込んだゲームファイルは次の2ファイルのみ。他の書き込みは原本のバイナリバックアップ2ファイルと本報告のみ。Unity別Editorの起動、Library / Temp / PackageCacheの編集は行っていない。

## 対象版SHA256

| 相対パス | 変更前SHA256 | 変更後SHA256 |
| --- | --- | --- |
| `Assets/LoopRoom/Scripts/LoopModel.cs` | `741043710EF81DA2120EAE08AE9F8F4D1DC98D4CBF62787B3127DDDC2ACE3224` | `67599928845DE6A3159FCD7FF2688F7FF783DBB5B0653B0D40E4F988E9CAD55E` |
| `Assets/LoopRoom/Editor/LoopModelChecks.cs` | `943045A198C37B5D9AA4B365BADD9FB8F0C010ECAD9686271715B105A2B4AAD0` | `3585F347546A97A92D2FFA74D526893F2AE7DCA82624A522C1378EA25DB80E61` |

変更前に `Copy-Item` で原本を `Collaboration/changes/003/before/<同相対パス>` にコピーした。コピー直後に各原本とバックアップのSHA256一致を確認済み。再開後もバックアップは上記変更前SHA256と一致している。

## 独立.NET実行

Unityを起動せず、PowerShell 7.6.5 / CLR 10.0.11 の `Add-Type -Path` で変更後の2ファイルを直接コンパイルして実行した。新しい検証スクリプトファイルやアセンブリはプロジェクトに保存していない。終了コード0。

実行内容（プロジェクトルートから）:

```powershell
$ErrorActionPreference = 'Stop'
Add-Type -Path @('Assets/LoopRoom/Scripts/LoopModel.cs', 'Assets/LoopRoom/Editor/LoopModelChecks.cs')
$results = [LoopModelChecks]::Run()
$results
if ($results.Count -ne 11) { throw "Expected 11 passing checks; got $($results.Count)" }
$model = [LoopRoom.LoopModel]::new($null)
$model.Start()
$model.Advance(1000)
if ($model.TotalTime -ne 180 -or $model.Phase.ToString() -ne 'Finished' -or $model.Outcome.ToString() -ne 'TimedOut') {
    throw 'No-input deadline regression'
}
```

再開後の実行結果:

```text
PASS: Unprotected attack returns immediately and resets only world state
PASS: Shield grants a real escape window
PASS: No escape before unlocking or with a stale loop id
PASS: Repeated death callbacks produce only one reset
PASS: Deadline includes blackouts and ends within 180 seconds
PASS: Missing escape window allows the enemy to flank
PASS: Large and small time steps yield the same no-input outcome
PASS: 100 resets reject old events and clear shield state
PASS: Stop is distinct from death and does not resume the same session
PASS: Invalid timing and non-finite delta are rejected
PASS: All seven timings reject NaN and both infinities
Independent no-input result: TotalTime=180, Phase=Finished, Outcome=TimedOut
Runtime: PowerShell=7.6.5; CLR=10.0.11
UTC: 2026-09-17T00:53:44.2810838Z
```

停止前の最初の試行でも11チェックは全PASSだったが、追加の無操作確認を `New-Object LoopRoom.LoopModel` で呼んだため、PowerShellが省略可能引数のあるコンストラクタを引数なしで解決できず終了コード1となった。ハーネスを `[LoopRoom.LoopModel]::new($null)` に修正し、停止前と今回の再開後の両方で11チェックと追加の無操作確認が終了コード0で完了した。C#変更は不要だった。

## 原本からの差分

バックアップと現行ファイルを `git -c core.autocrlf=false diff --no-index` で比較した差分。以下ではパス表記のみプロジェクト相対に整理した。

```diff
--- before/Assets/LoopRoom/Scripts/LoopModel.cs
+++ Assets/LoopRoom/Scripts/LoopModel.cs
@@ -17,6 +17,14 @@ namespace LoopRoom
         public double endingLength = 8.0;
         public void Validate()
         {
+            if (double.IsNaN(firstShot) || double.IsInfinity(firstShot) ||
+                double.IsNaN(searchShot) || double.IsInfinity(searchShot) ||
+                double.IsNaN(exitOpens) || double.IsInfinity(exitOpens) ||
+                double.IsNaN(exitCloses) || double.IsInfinity(exitCloses) ||
+                double.IsNaN(blackout) || double.IsInfinity(blackout) ||
+                double.IsNaN(playLimit) || double.IsInfinity(playLimit) ||
+                double.IsNaN(endingLength) || double.IsInfinity(endingLength))
+                throw new ArgumentException("Invalid loop timing rules");
             if (firstShot <= 0 || searchShot <= firstShot || exitOpens < firstShot ||
                 exitCloses > searchShot || exitCloses <= exitOpens || blackout <= 0 ||
                 playLimit <= 0 || endingLength <= 0 || playLimit + endingLength > 180.0)
--- before/Assets/LoopRoom/Editor/LoopModelChecks.cs
+++ Assets/LoopRoom/Editor/LoopModelChecks.cs
@@ -64,6 +64,19 @@ public static class LoopModelChecks
             Need(bad,"bad rules");bad=false;try{new LoopModel().Advance(double.NaN);}catch(ArgumentException){bad=true;}
             Need(bad,"bad delta");
         });
+        Check(passed,"All seven timings reject NaN and both infinities",()=>{
+            string[] names={"firstShot","searchShot","exitOpens","exitCloses","blackout","playLimit","endingLength"};
+            Action<LoopRules,double>[] setters={
+                (r,v)=>r.firstShot=v,(r,v)=>r.searchShot=v,(r,v)=>r.exitOpens=v,
+                (r,v)=>r.exitCloses=v,(r,v)=>r.blackout=v,(r,v)=>r.playLimit=v,(r,v)=>r.endingLength=v
+            };
+            double[] values={double.NaN,double.PositiveInfinity,double.NegativeInfinity};
+            for(int i=0;i<setters.Length;i++)foreach(double value in values){
+                var rules=new LoopRules();setters[i](rules,value);
+                bool bad=false;try{rules.Validate();}catch(ArgumentException){bad=true;}
+                Need(bad,names[i]+" accepted "+value);
+            }
+        });
         return passed;
     }
```

## 未実施と引き継ぎ

- Unity統合コンパイル・Unity内チェック: この実装セッションでは未実行。独立.NET実行結果で代用済みとは扱わない。
- HMD / 実機確認: 未実行。
- 新セッションのSol / Sonnet独立レビュー、指摘交換、Astraの受入判断: 未実施。本報告を独立レビューとして数えない。

次の担当は上記変更後SHA256の版を固定して独立レビューを実施する。指定2ファイルの版が変わった場合は、この実行結果やレビュー承認を新しい版へ流用しない。
