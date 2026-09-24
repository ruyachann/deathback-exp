# 023 — 天井灯の影の負荷を下げる（VR 向け）

状態: **受入（2026-09-25、reviews/tasks022-023-exchange-20260925.md）**。計画 Claude Opus 5.5。実装 GPT-5.6 Sol。レビュー 実装とは別セッションの Sol と Claude Sonnet 5。task022（Sonnet）と並行。

## 背景

天井灯（RoomVisuals.cs の "Ceiling light"）は影付きの Point light。点光源の影は6面のシャドウマップを描くため、両眼を描く VR では負荷が大きい（task019 の確認で判明）。Quest 3S も対象。

## 設計

- 影を落とす光を、天井から真下に向けた **Spot light 1つ**（影 Soft、解像度 Medium、spotAngle は部屋の床と机を覆う広さ、例 140〜160°、innerSpotAngle は自然にぼける値）に置き換える。影のマップは1面になる。
- 天井や壁の上部が暗くなりすぎないよう、**影なし**の Point light（弱め）を補助として残してよい。見た目（明るさ・色・机と敵の影）はできるだけ今と同じにする。
- ほかの光（窓の Spot、影なし）と、レンダラーの影の設定は変えない。

### 独立レビューの扱い（2026-09-25、計画担当 Opus 5.5）

Sol approve、Sonnet request_changes（`sonnet-task023-independent-mcp.md`）。
1. 見送り（Sonnet 中「補助光の重複で明るくなる」）: 光の真下では Spot 2.6＋補助 0.6＝3.2 で従来の Point 3.2 と同じ。Spot の外側は従来より暗くなる方向で、明るくはならない。変更前後の同一構図（evidence/20260925-light-focus/light-compare.png）でほぼ同じ見た目を確認した。
2. 見送り（Sonnet 低〜中「浅い角度の影が消える」）: 真下から70°より外の光は影なしの補助光で照らしており、その範囲の落ち影がなくなるのは負荷削減の代償として許容する。見え方は実機確認で判断する。

### 交換後（2026-09-25）

Sol approve。Sonnet request_changes（`sonnet-task023-exchange-mcp.md`）: ①見送りを Sonnet 本人が承認していない、②光量の加算は URP の単位設定が前提、③証拠がレビュー対象に含まれていない。
- ①③: 比較画像（`evidence/20260925-light-focus/light-compare.png`）とテスト出力（`tests.txt`）を Sonnet が直接読める形で渡し、本人の再判定を求める。
- ②: URP には HDRP の物理ライト単位（lumen 等）が無く、Point と Spot の intensity は同じ単位・同じ距離減衰（Spot は角度の減衰が加わるだけで、innerSpotAngle の内側は減衰なし）。よって光の真下（内側 110°）では 2.6＋0.6 が従来の 3.2 と一致する。

## 許可ファイル

- `Assets/LoopRoom/Scripts/RoomVisuals.cs`

## 受入条件

1. batchmode 0/0、テスト全件 PASS。
2. 証拠: 変更前と変更後の同じ構図の画面（計画担当が撮影、desktop の周回中）。机・敵の影が残っていること。
3. 別セッションの Sol と Sonnet の独立レビュー、交換後に両方 approve。
4. 実機での負荷（フレーム落ち）は未確認と明記。ユーザーの Air Link 確認で見る。
