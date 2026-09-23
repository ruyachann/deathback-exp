# 006（下書き・未承認）— 段階2の追加修正候補（B-4 / B-5 / B-6 / B-7、A-1は方針決定が先）

状態: **DRAFT**。2026-09-22、ユーザー指示によりClaude Sonnet 5が下書きのみ作成。Sol（目的・許可ファイル・受入条件・リスクの正式記載）とユーザーの確認を経るまで実装しない。task005と同じ「非対象」リストに挙がっていた項目の整理。

根拠: `Collaboration/reviews/fable-plan-and-code-review-20260918.md` Part A-1、Part B-4〜B-7。

## 先に方針決定が必要な項目

### A-1（高）実時間180秒の定義（コード修正ではなく仕様決定）
- 現状: `LoopModel`は**モデル時間**で`playLimit+endingLength<=180`を保証するが、追跡瞬断中はモデル時間が停止し実時間は伸びる。壁時計上限は明示的に非対象のまま。
- 決定が必要: 案A(壁時計180秒でモデル残り時間を打ち切りTimedOut扱い) か 案B(実時間超過は運営判断で中断、要件を「モデル時間180秒」に緩める)。どちらか1つをユーザーが選び、STAGE_PLAN.md/ARCHITECTURE.mdに反映してから実装タスク化する。
- **2026-09-23 ユーザー決定（案A/Bのどちらでもない）**: 「180秒の制限は今はなくし、まずはしっかり遊べるように持っていきたい」。180秒上限は当面の要件・受入基準から外す。壁時計上限の追加は行わない。AGENTS.md の不変条件と STAGE_PLAN.md に反映済み。
- 実装への影響（未実装、別タスク化）: `LoopModel.cs:30` の `playLimit + endingLength > 180.0` 検証と `playLimit` による TimedOut 経路をどう扱うか（上限撤廃 / 十分大きな既定値 / 運営のみ終了）を計画担当が決めてタスク化する。モデルテスト（管理26/モデル12）の180秒前提も同時に更新が必要。

## 実装候補（許可ファイルは正式タスク化時にSolが確定）

| 項目 | 優先度 | 場所 | 修正案概要 | リスク・懸念 |
| --- | --- | --- | --- | --- |
| B-4 XR準備の再試行手段が無い | 中 | DemoRig.cs:259,266,283 | `RetryPreparation()`を追加し、`!xrInitializing && !CanStart`時に準備フラグをリセットして`StartXR()`再実行。LoopDemoで運営キー(例: R)に割当 | Quest3実機でのみ効果検証可能。誤って周回中に再トリガーしない入力条件の確認が必要 |
| B-5 机の近縁が頭位置から0.17mで身体と交差 | 中 | RoomVisuals.cs:72、LoopDemo.Begin:137 | Desk/取っ手/時計を一括+0.15m程度ずらし、机近縁を0.30〜0.35mへ | 体験の身体位置に関わる変更のため、AGENTS.mdの「Opus優先の相談条件」(2m活動範囲・身体位置)に該当する可能性がある。実装前にOpus/ユーザー確認を検討 |
| B-6 LoopRulesの共有参照と再検証なし | 中 | LoopDemo.cs:11,41、LoopModel.cs:61-65 | `Model=new LoopModel(timings)`前に`timings`を複製(`JsonUtility`往復)して渡す最小修正。恒久対応はIssue4 | 最小修正のみなら影響範囲は小さい |
| B-7 Quest3のコントローラープロファイル（2026-09-24: 実機が Quest 3S になる可能性あり。3S も Touch Plus なので優先度を上げる） | 中→高候補 | DemoSetup.cs:98,117 | `MetaQuestTouchPlusControllerProfile`も有効化、Validateは「どちらか一方が有効」を合格条件に | 実機(Quest3接続)でのみ最終確認可能。現在Quest3接続不可のため検証が保留になる |

## 非対象（このDRAFTでも対象外のまま）
B-8(Build-Demo.ps1の既存Editor起動確認)、B-9(Program.csのチェック件数固定)、A-2〜A-9は別途整理。

## Sync-PublishedBranch.ps1の既知バグ（このDRAFTとは別件、報告のみ）

**2026-09-24: task008 で修正・受入済み。** 以下は発見時の記録。
2026-09-22のGit同期作業で判明: Windows PowerShell 5.1で`$ErrorActionPreference='Stop'`と`git ... rev-parse --verify HEAD 2>$null`（9行目、unborn HEAD時に意図的に失敗させる行）の組み合わせが、redirectで抑制されないNativeCommandErrorとして扱われ、スクリプトがunborn HEAD（今回のような初回同期）で必ず終了コード1になる。スクリプト本来の主目的（unborn HEADからの初回同期）が動作しない状態。今回は同じgit手順を手動実行して回避し、スクリプト自体は変更していない（許可範囲外のため）。修正例: 9行目を`try { ... } catch {}`で囲む、または`$ErrorActionPreference`を該当行だけ`'SilentlyContinue'`に切り替える。

## 次のステップ
1. ユーザーがA-1の方針(案A/案B)を決定。
2. Solが本DRAFTを土台に正式タスク(目的・許可ファイル・受入条件・リスク)を作成、特にB-5は身体位置変更のためOpus相談要否を判断。
3. B-4/B-6はQuest3不要で実装・部分検証可能。B-5/B-7はQuest3実機確認が前提。
4. Sync-PublishedBranch.ps1のバグ修正は別途小タスク化。
