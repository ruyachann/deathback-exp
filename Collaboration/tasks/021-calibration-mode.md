# 021 — キャリブレーション画面（足元に体験空間を表示して決定する）

状態: 計画確定（2026-09-24）。計画 Claude Opus 5.5。実装 Claude Sonnet 5。レビュー 実装とは別セッションの Sonnet と GPT-5.6 Sol。task020（設定、Sol）と並行。task020 の API（`PlayAreaSettings`、`RoomAnchor` の設定オーバーロード）を使う。

## 目的（ユーザー指定 2026-09-24）

VR ゴーグルをつけると、足元に必要な広さ（自分を初期位置として）が見え、決定するとそこが体験の空間としてキャリブレーションされる。

## 設計

1. **起動時はキャリブレーション画面から始まる**（LoopDemo に「キャリブレーション中」の状態を追加。LoopModel の Phase は変えない。Ready の前段として LoopDemo 側で持つ）。desktop でも同じ（desktop は頭の位置＝カメラ）。
2. **足元の表示**（HMD と desktop の両方に見える。観客には見せない: PrivateLayer 8。当たり判定なし）:
   - 体験空間の正方形（一辺 `settings.areaSize`）の外枠を床の少し上（1〜2cm）に線で描く。中心は**頭の床面投影**、向きは**頭の yaw**（毎フレーム追従）。
   - 内側に余裕 `margin` を引いた正方形（点線か薄い線）と、定位置から前方の扇形（半径 `reach`、±`halfAngleDeg`）を薄く塗る。定位置に小さな足元の印。
   - 境界情報: `XRInputSubsystem.TryGetBoundaryPoints` で Quest の境界が取れた場合は床に薄く描き、体験空間の正方形が境界の内側に収まっていれば枠を**緑**、はみ出していれば**赤**。取れない場合は枠を**白**にし、運営表示に「境界情報なし（目視で確認）」と出す（取得失敗を安全と扱わない）。
   - 頭の前方 1m 程度、目線の少し下に案内の文字: 「足元の枠が体験の空間です／A か X を長押しで決定（運営: C）」。文字は `RoomVisuals.TextMaterial` を使う。
3. **決定**: A または X を **1秒長押し**（`DemoRig` の開始ボタンと同じ入力。短押しは無視）、または運営キー **C**。決定した瞬間の頭の床面位置と yaw を、task018 の位置合わせの値（alignCx, alignCz, alignAreaYaw）として保存し、`aligned=true`。表示を消して Ready（開始待ち）へ。枠が赤でも決定はできるが、運営表示とログに警告を出す（展示では運営が判断）。
4. **やり直し**: Ready または Finished で運営が **C** を押すと、キャリブレーション画面に戻る（今の「C で即時に位置合わせ」を置き換える）。desktop/VR の切り替えや R の再準備の後も、キャリブレーション画面に戻る（task018 の ResetAlignment と同じ扱い）。
5. **設定**: 起動時に `PlayAreaSettings.LoadOrCreate(Application.persistentDataPath + "/play-area.json")` を読み、表示と `RoomAnchor.ChooseFrontYaw(..., settings, out fits)` の両方で使う。
6. **証拠用オプション** `--auto-calibrate`（`--desktop` 併用時のみ）: キャリブレーション画面を2秒表示してから自動で決定する（`--autostart` はその後に動く）。desktop のカメラは床の枠が見えるよう、キャリブレーション中だけ少し下を向ける（例 pitch 45°）。

## 許可ファイル

- `Assets/LoopRoom/Scripts/LoopDemo.cs`
- 新規 `Assets/LoopRoom/Scripts/CalibrationView.cs`（枠・扇形・境界・案内の表示。ほかの任意の名前でもよい）
- `Assets/LoopRoom/Scripts/DemoRig.cs`（長押しの判定、境界点の取得、desktop のキャリブレーション中の向きに必要な最小の変更のみ）

RoomAnchor.cs と PlayAreaSettings.cs は task020 の担当が作成中なので触らない（task020 文書の API どおりに呼ぶ）。

## 守ること

- XR 原点を動かさない。キャリブレーション中はセッションを始めない（死亡・周回は起きない）。観客に枠や案内を見せない。Enter/R/C の排他、--autostart/--autoescape/--simulate-drift の条件を壊さない。

## 受入条件

1. batchmode 0/0、テスト全件 PASS。
2. 証拠: `--desktop --auto-calibrate --autostart` の自動実行で、足元の枠（白、境界情報なし）が見える画面と、決定後に開始する画面（計画担当が撮影）。Player.log の例外0。
3. 別セッションの Sonnet と Sol の独立レビュー、交換後に両方 approve。
4. 実機（Air Link）で、枠が足元に正しく見えるか、境界との比較、長押しでの決定を確認（未確認と明記）。
