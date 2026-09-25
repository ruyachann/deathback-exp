# task010 追修正版 独立レビュー

対象SHA256（与えられたスナップショットの値をそのまま引用）:
- `Assets/LoopRoom/Scripts/LoopModel.cs`: `a95cfd677fd4dd34dfe3e33f159207c9366de71e26f1ac91f43a68f7dfd578b0`
- `Assets/LoopRoom/Editor/LoopModelChecks.cs`: `a9dc8f1d5baba9476bffa03d4b4e1b9d1d3aab6957a4375f3e4a4510f4077b7d`
- `Tests/LoopModel.Tests/Program.cs`: `ec974b45ca77b441b62c5691186c5dc56b58238ebb633f0c8b964db72eda42c1`
- タスク文書 `Collaboration/tasks/010-rules-copy-and-minimums.md`: `2fbb00db131ea2d724567ee55b310b6b27b8304470976b1ee49cfc045cd106ed`

他のレビュー報告（sol/sonnet の task010 レビュー）は読まずに、上記スナップショットのみから独立に判断した。

## 検証観点ごとの確認結果

### 追修正1（許容誤差1e-9・6.0/6.05・6.5/6.55の境界）
`LoopModel.cs` 39〜45行目付近（`Validate()` 内、既存の順序検査ブロックの直後）で、
```
if (firstShot < MinInterval - IntervalTolerance ||
    searchShot - firstShot < MinInterval - IntervalTolerance ||
    exitCloses - exitOpens < MinInterval - IntervalTolerance ||
    blackout < MinInterval - IntervalTolerance ||
    endingLength < MinInterval - IntervalTolerance ||
    playLimit < MinInterval - IntervalTolerance)
```
という形で、差分（searchShot-firstShot, exitCloses-exitOpens）・単独値（firstShot, blackout, endingLength, playLimit）のすべてに`IntervalTolerance = 1e-9`が一様に適用されている。指示通りの形。

`LoopModelChecks.cs`の`"Minimum interval boundaries are enforced"`テストに
```
new LoopRules{firstShot=6.0,searchShot=6.05,exitOpens=6.0,exitCloses=6.05}.Validate();
new LoopRules{exitOpens=6.5,exitCloses=6.55}.Validate();
```
が追加されている。`6.05-6.0`および`6.55-6.5`はIEEE754の丸め誤差で厳密に0.05にならず約`0.049999999999999822`前後になるが、これは`0.05 - 1e-9 ≈ 0.049999999`より大きいため`Validate()`は例外を投げない。1e-9という許容幅は通常の減算で生じる誤差（1e-15〜1e-16オーダー）より十分大きく、境界値は問題なく通過する。**要求通り実装されている。**

### 追修正2（検索区間・出口区間の「半分は拒否」ケースが既存の順序検査より先に引っかからないこと）
`LoopModelChecks.cs`の`makeTooSmall`配列:
```
r=>{r.firstShot=MinInterval; r.searchShot=MinInterval*1.5;
    r.exitOpens=r.firstShot; r.exitCloses=r.searchShot;},           // search interval
r=>{r.exitOpens=6.5; r.exitCloses=6.5+MinInterval/2;},               // exit interval
```
search intervalケースでは `exitOpens==firstShot`, `exitCloses==searchShot` として出口区間を検索区間内に収めており、`exitCloses > searchShot`（既存の順序検査）には引っかからず、`searchShot-firstShot < MinInterval-1e-9`（新規検査）でのみ拒否されることを確認した。exit intervalケースも同様に `[firstShot, searchShot]` のデフォルト範囲(6.0〜12.0)内に収まる値を使っており、既存の順序検査を素通りして新規検査だけで拒否される構成になっている。**要求通り実装されている。**

### 追修正3（最小許容ルールで`Advance(MaxStep)`が戻り`TotalTime==MaxStep`）
```
Check(passed,"Minimum intervals remain bounded at MaxStep",()=>{
    var rules=new LoopRules{ firstShot=MinInterval, searchShot=MinInterval*2,
        exitOpens=MinInterval, exitCloses=MinInterval*2, blackout=MinInterval,
        playLimit=MinInterval, endingLength=MinInterval };
    var m=new LoopModel(rules); m.Start(); m.Advance(LoopModel.MaxStep);
    Near(m.TotalTime, LoopModel.MaxStep);
});
```
`enforcePlayLimit`は既定でfalseのため、`playLimit=MinInterval`はValidateの180秒チェックには影響せず、かつAdvance内の`untilLimit`は`enforcePlayLimit`がfalseの間`double.PositiveInfinity`のままなので、TimedOutに落ちずに周回し続け、MaxStepまで`TotalTime`が単調加算される設計と整合している。回帰テストとして意図通り成立している。**要求通り実装されている。**（ただしこのテストは「playLimit自体も最小値の境界検査に含める」ことのみ検証しており、`enforcePlayLimit=true`かつ全区間MinIntervalの組合せでのMaxStep到達は検証していない。後者は仕様上Finished後にAdvanceが即returnするため、そもそもTotalTime==MaxStepにはなり得ない。タスク文言の趣旨からは逸脱しておらず、欠陥とは判断しない。）

### 追修正4（Rulesの外部書き換え）
タスク文書で明示的に見送りとされており、今回のスナップショットでも`LoopRules`の全フィールドはpublicなまま、`Model.Rules`もreadonlyだが中身は変更可能。**指示通り対象外。**

### Program.cs のカウント整合性
`LoopModelChecks.Run()`内の`Check(...)`呼び出しを数えると18件（既存6件＋task010関連4件＋既存8件）で、`Program.cs`の`Require(existing.Count == 18, ...)`と一致している。数え漏れ・ズレなし。

## 指摘事項（findings）

| severity | file:line(目安) | 内容 |
|---|---|---|
| info | `LoopModel.cs:39-45` | `IntervalTolerance=1e-9`は絶対誤差固定。今回の値域（0.05〜172秒）では十分だが、将来MinIntervalや入力値の桁が大きく変わった場合は絶対誤差では不十分になりうる。現状は欠陥ではないが設計メモとして残す余地あり。 |
| info | `Assets/LoopRoom/Editor/LoopModelChecks.cs`（"Minimum intervals remain bounded at MaxStep"） | このテストは`enforcePlayLimit=false`の経路のみを検証しており、`enforcePlayLimit=true`かつ全区間MinIntervalでのMaxStep挙動（Finished到達後は即returnしTotalTimeが伸びない）は別経路として未検証。タスク文の意図とは矛盾しないため指摘に留める。 |

いずれも実装の正しさを損なう欠陥ではなく、approveを妨げるものではない。

## Verdict

**approve**

## 未検証事項（unverified checks）

- `dotnet test`／Unity付属Roslynでの実際のテスト実行結果（全18＋派生ケースがPASSすること）は未実施。
- Unity Editor batchmodeでのコンパイルエラー0・警告0は未確認。
- `"Minimum intervals remain bounded at MaxStep"`テストが実機速度で妥当な時間内（36,000回程度のwhileループ相当）に完了するかの実測は未確認（理論上は有界だが実行未検証）。
- 6.05/6.55の浮動小数点丸め誤差値そのものを実行環境で出力して確認してはいない（IEEE754倍精度の一般的挙動からの推論）。