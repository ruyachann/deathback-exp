# 028 — 安全な向きが無いときの運営への警告

状態: **受入（2026-09-25、reviews/tasks028-029-exchange-20260925.md）**。計画 Claude Opus 5.5（Astra の計画レビュー A を受けて）。実装 Claude Sonnet 5。レビュー 実装とは別セッションの Sonnet と GPT-5.6 Sol。task029（Sol）と並行。

## 背景（Astra `reviews/astra-plan-review-20260925.md` の A）

企画 v0.2 は「収まる向きは必ずある」としていたが、体験者の基準点（頭の床面投影）が体験空間の中心から `areaSize/2 − margin` を超えて外れると、どの向きでも前方の領域が収まらない（`RoomAnchor` の fits=false）。今は警告ログを1回出すだけで、頭の向きのまま部屋を置いて続行する。運営は画面からそれに気づけない。

## 方針（計画担当の判断）

- **挙動は変えない**（続行する。自動で止める・自動でキャリブレーションし直すのは身体位置と即時性に関わる重要判断なので、実機確認とユーザー判断の後に決める）。
- 運営が気づいて対処できるようにする: 体験者を中央付近へ戻す、または C でキャリブレーションし直す（手順は計画担当が DEVICE_QUICKCHECK に書く）。

## 設計

1. LoopDemo: 直近の置き直し（PlaceRoom）が fits=false なら、運営表示（desktop は常時、VR は F2。観客には出さない）に目立つ色で「安全な向きなし: 体験者を中央へ（またはCで再設定）」を表示する。次の置き直しで fits=true になれば消える。キャリブレーションし直したときも消える。
2. fits=false が起きた回数と最後の位置（中心からの距離 m）を数え、セッションログに `noFitPlacements`（回数）と `maxNoFitDistance`（最大の距離 m）として残す。
3. 警告ログは今の「1回だけ」から、**fits=false に変わった瞬間ごと**に1行（連続する周回では出し続けない）に変える。
4. HMD の体験者には何も出さない（没入を壊さないため。運営が対処する）。

### 追修正（2026-09-25、撮影で判明。計画担当 Opus 5.5）

警告の行が暗い赤で灰色のパネル上だと読みにくい（evidence/20260925-no-fit/warning-before-color-fix.png）。→ 明るい色（例 (1, .5, .35)）にし、太字にする。ほかの行の色は変えない。

追記: 太字にするとパネルの右端からはみ出す（evidence/20260925-no-fit/warning-compare.png）→ 文言を「安全な向きなし: 中央へ / C で再設定」に短くする。

## 許可ファイル

- `Assets/LoopRoom/Scripts/LoopDemo.cs`

## 受入条件

1. batchmode 0/0、テスト全件 PASS。
2. 証拠: `--desktop --auto-calibrate --autostart --simulate-drift` で fits=false が起きる場合の運営表示（計画担当が撮影）。起きない場合は計画担当が起こし方を決める（例: play-area.json の areaSize を小さくする）。
3. 別セッションの Sonnet と Sol の独立レビュー、交換後に両方 approve。
