# 033 — ゲーム性 v0.3: 窓の狙撃とブラインド（モデル）

状態: **受入（2026-10-01、code-reviewer approve＝reviews/code-reviewer-task033.md・code-reviewer-task033-fix.md、証拠 evidence/20261001-gameplay-model/）**。計画 Opus 5.5（Docs/Plans/gameplay_v0.3.md、Astra のセカンドオピニオン反映済み）。実装 `implementer`（Sonnet 5.5）。レビュー `code-reviewer`。コンパイル・テスト・撮影は Opus。

## 目的

1周の流れを「扉の男（t=6）→ 窓の狙撃（t=9）→ 回り込み（t=12）と出口（9.5〜12）」の3段階にする。本タスクは**純粋なモデル（LoopModel）とテスト**、および既存の操作経路（desktop のキー、`--autoescape`）が壊れないための LoopDemo の最小限の変更。見た目（ブラインド、向かいの建物、ガラス）は task034。

## 設計

1. `LoopRules`: `windowShot = 9.0` を追加。`exitOpens` の既定を 6.5 → **9.5** に。`Validate()` に `firstShot + MinInterval <= windowShot <= searchShot - MinInterval` を追加（既存の検証の書き方・許容誤差に合わせる）。exitOpens・exitCloses の既存の検証は維持。
2. `LoopModel`:
   - `BlindsClosed`（周回ごとに false に戻る）と `CloseBlinds(int expectedLoop)`（Playing・同じ周回・未実施のときだけ true、記録 `blinds_closed`）。開け直しはない（一度下ろしたら周回の終わりまで下りたまま）。
   - 時刻の処理: 今の firstShot・searchShot に、**windowShot** を加えた3つの出来事を時刻順に処理する（長いフレームで複数の時刻をまたいでも順番どおり。今の「時刻で区切って進む」方式を保つ）。
     - t=firstShot: 今どおり（遮蔽あり→ `shot_blocked`、なし→ 死亡 `first_shot`）。
     - t=windowShot: firstShot で死んでいない場合のみ。ブラインドが下りていれば `window_blocked`、下りていなければ死亡 `window_shot`。
     - t=searchShot: 今どおり死亡 `flanked`。
   - 手助けのための情報: `LastDeathCause`（直前の周回の死因、なければ null）と `SameCauseStreak`（同じ死因が何周続いたか）。新しい周回の始めに更新。脱出・中断では変えない。
3. LoopDemo（最小限）: desktop の **B キー**で `CloseBlinds`。`--autoescape` の自動操作に、遮蔽と同じタイミングでブラインドを下ろす処理を加える（出口は今どおり ExitAvailable で）。死亡の効果音・暗転の既存の処理が `window_shot` でも動くこと（死因の文字は出さない）。F2 の運営表示に `SameCauseStreak` と直前の死因を出してよい（観客には出さない）。
4. 既存のテストで出口 6.5 を前提にしたものは、新しい既定値に合わせて直す（テストの意図は変えない）。

## テスト（LoopModelChecks に追加、Tests/LoopModel.Tests でも回る）

1. 遮蔽あり・ブラインドなし → t=9 で `window_shot` 死亡。
2. 遮蔽あり・ブラインドあり → t=9 で `window_blocked`、t=12 で `flanked`。
3. 遮蔽あり・ブラインドあり → t=9.5〜12 で出口を使うと Escaped。t<9.5 の出口は失敗。
4. 遮蔽なし → t=6 で死亡し、t=9 の処理は起きない。
5. 1回の Advance で t=5→t=13 をまたいでも、6→9→12 の順で正しく処理される（遮蔽・ブラインドの有無の組み合わせ）。
6. 旧周回の `CloseBlinds` は拒否、周回が変わると BlindsClosed は false。
7. `SameCauseStreak`: 同じ死因が続くと増え、違う死因で 1 に戻る。
8. Validate が windowShot の範囲外を拒否。

### Opus の引き継ぎ（2026-10-01）

実装担当の指摘: シーン `Assets/LoopRoom/Scenes/LoopRoom.unity` に LoopDemo の `timings` がシリアライズされ `exitOpens: 6.5` が残っており、新しい既定値を上書きする。→ Opus がシーンの値を `exitOpens: 9.5` に直し `windowShot: 9` を追加（許可範囲外の小さな変更のため Opus が実施）。

### 追修正（2026-10-01、code-reviewer request_changes。reviews/code-reviewer-task033.md）

1. 採用（P2、案 A）: カスタム Rules で `exitOpens < windowShot` だと窓の脅威を飛ばして脱出できる → `Validate()` に `exitOpens >= windowShot - IntervalTolerance` を追加。影響する既存テスト（最小間隔・境界・極小値など exitOpens を窓より前にしているもの）を、テストの意図を保ったまま exitOpens を窓以降に直す。「exitOpens が windowShot より前の Rules を拒否」のテストを追加。
2. 採用（P3）: `Kill(id, null)` で連続回数が更新されない → 死因が null なら "unknown" として扱う。
見送り: VR のブラインド操作（task034 で配線する。033 と 034 の間に実機確認は挟まない）、運営表示の死因の日本語化（task034 で検討）、窓の銃声の位置（task034）。

## 許可ファイル

- `Assets/LoopRoom/Scripts/LoopModel.cs`、`Assets/LoopRoom/Editor/LoopModelChecks.cs`、`Tests/LoopModel.Tests/Program.cs`
- `Assets/LoopRoom/Scripts/LoopDemo.cs`（上の 3 の最小限）

## 受入条件

1. Opus が実行: テスト全件 PASS（Unity 付属 Roslyn）、ビルド 0/0、`--autoescape` で脱出まで到達（セッションログ）、何もしない周回と遮蔽だけの周回で死因が first_shot / window_shot になる（ログ）。
2. `code-reviewer` の独立レビューで approve。
