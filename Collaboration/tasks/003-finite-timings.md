# 003 — 不正な非有限timing値を状態モデルの入口で拒否する

状態: Astra設計に基づきSolが実装担当。最新の実施状況はCollaboration/STATE.mdを参照。002とは独立だが同時に同じファイルを編集しない。

## 問題と範囲

`LoopRules.Validate()` の大小比較はNaNを拒否できない。Astra独立試験で `firstShot=double.NaN` が受理され、Start→Advance(1)後にTotalTime/LoopTimeがNaNになることを確認した。通常デフォルトの既存10チェックは全PASS。

変更許可: `Assets/LoopRoom/Scripts/LoopModel.cs` と `Assets/LoopRoom/Editor/LoopModelChecks.cs` のみ。

## 最小実装

- 全7項目 `firstShot, searchShot, exitOpens, exitCloses, blackout, playLimit, endingLength` のNaN/正負InfinityをValidateの最初に拒否する。
- Unity/ランタイム互換性のため既存コードと同じ `double.IsNaN` / `double.IsInfinity` を使えば十分。新依存不要。
- 既存のタイミング順序制約・既定値・イベント名・状態遷移は変えない。
- 各項目へNaN/正負Infinityをそれぞれ入れた計21ケースの拒否を、既存Check形式の1項目にまとめて検証する。毎ケース新しいLoopRulesを作る。

## 受入条件

1. 既存10チェックと追加1チェックがPASS。
2. 通常無操作のモデルはTotalTime=180でFinished/TimedOutのまま。
3. 指定ファイル以外は変更なし。新依存なし。
4. 独立.NET実行とUnity内実行を分けて記録する。可能な方だけ通した場合、他方は未実行と記す。
5. 同じSHA256に対するSol/Sonnetの独立レビュー・指摘交換を残す。

## 非対象

実行中Inspector変更へのライブ反映、Rulesの不変オブジェクト化、壁時計3分上限はこの修正に含めない。必要なら別設計に戻す。
