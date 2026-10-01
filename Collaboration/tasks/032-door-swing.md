# 032 — ドアの開き方

状態: 計画確定（2026-10-01）。計画 Opus 5.5。実装 `implementer`。task030 の後。

## 目的（ユーザー指定 2026-10-01「ドアの開き方などのクオリティを上げてください」）

今の扉は、t=3 から 0.65 秒で横に滑って消える（`LoopDemo.cs` の `room.Door.localPosition` の x を動かす）。本物の扉のように**蝶番で回って開く**ようにする。

## 設計

1. 新規 `Assets/LoopRoom/Scripts/DoorRig.cs`（MonoBehaviour でなくてよい。RoomVisuals から作り、LoopDemo から開き具合を渡す）: 蝶番側の軸（扉の端）を親にして扉板・パネル・ノブを子に置く。`SetOpen(float t)` のような関数で 0（閉）〜1（全開、約 95°）。
2. 動き（t は周回内の時刻、今の t=3 の開始は変えない）: ①ノブが回る（約 0.15 秒）②わずかに隙間が開いて一瞬止まる（ラッチが外れる感じ）③押し開けられて減速しながら止まる（合計 0.8〜1.0 秒、イーズアウト）。開く向きは**部屋の内側へ**、敵の通り道を塞がない側。
3. 音: ノブ・ラッチの「カチャ」と、短いきしみ（ProceduralAudio に追加してよい）。今の Latch（t=3 の足音/ラッチ）と重なりすぎないよう調整。音は扉の位置から 3D で鳴らす。
4. **扉の向こうに廊下**: 開いたとき奥が虚無にならないよう、扉の外側に短い廊下（床・壁・天井、暗めの照明は**影なし**で1つまで）を置く。PrivateLayer か既定かは観客表示の仕組み（観客には扉は閉じたまま）に合わせる。
5. 周回の始まり（暗転明け）は必ず閉じた状態に戻る。Blackout 中に途中の角度が見えないこと。観客用の扉（`publicDoor`）は従来どおり閉じたまま。
6. 敵の出現（t>=3）は今の時刻のまま。敵が扉を通るときに扉板とめり込まないこと（task031 で敵の動きを作り直すときの前提として、開いた扉の位置を task031 に書き残す）。

## 許可ファイル

- 新規 `Assets/LoopRoom/Scripts/DoorRig.cs`
- `Assets/LoopRoom/Scripts/RoomVisuals.cs`（扉と廊下の生成部分）、`Assets/LoopRoom/Scripts/LoopDemo.cs`（扉の開き具合と音を渡す部分）、`Assets/LoopRoom/Scripts/ProceduralAudio.cs`（音を足す場合）

## 受入条件

1. batchmode 0/0、テスト全件 PASS。
2. `deliverable-verifier` が開く途中を連続して撮る（閉→ノブ→隙間→全開、廊下が見える）。暗転明けに閉じていること。
3. `code-reviewer` の独立レビューで approve。
