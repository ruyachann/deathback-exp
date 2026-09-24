# 017 — 部屋の置き直しの計算（企画 v0.2 第2節）

状態: 計画確定（2026-09-24）。計画 Claude Opus 5.5。実装 GPT-5.6 Sol（`codex exec` 別セッション）。レビュー 実装とは別セッションの Sol と Claude Sonnet 5。task018（組み込み、Sonnet）と並行。

## 目的

ループ開始時に、仮想の部屋を体験者の位置と向きに合わせて置き直すための純粋な計算（Unity に依存しない C#）。前方の領域が現実の安全範囲に収まる正面の向きを選ぶ。

## API（task018 がこの形で呼ぶ。変えない）

新規 `Assets/LoopRoom/Scripts/RoomAnchor.cs`（namespace `LoopRoom`、`using System;` のみ、UnityEngine に依存しない）:

```csharp
public static class RoomAnchor
{
    public const double SafeHalfSize = 0.8;   // 現実に動ける範囲 1.6m 四方の半分
    public const double Margin = 0.10;        // 身体・位置合わせ誤差・追跡遅延の余裕（実機で調整）
    public const double Reach = 0.45;         // 定位置から前方に必要な動きの半径
    public const double HalfAngleDeg = 45;    // 前方の扇形の半角（正面 ±45°）
    public const double SearchStepDeg = 15;   // 向きの補正の刻み

    // 位置 (px,pz) から向き yawDeg を正面にしたとき、前方の扇形（半径 Reach、正面 ±HalfAngleDeg）と定位置が
    // 安全範囲（中心 (cx,cz)、向き areaYawDeg、半辺 SafeHalfSize - Margin の正方形）の内側に収まるか。
    public static bool ForwardRegionFits(double px, double pz, double yawDeg, double cx, double cz, double areaYawDeg);

    // headYawDeg を正面にして収まればそれを、収まらなければ headYawDeg から ±SearchStepDeg 刻みで近い順に探し、
    // 最初に収まる向きを返す（同じ近さなら + 側を先）。どれも収まらなければ fits=false で headYawDeg を返す。
    // 返す値は [0,360) に正規化する。
    public static double ChooseFrontYaw(double px, double pz, double headYawDeg, double cx, double cz, double areaYawDeg, out bool fits);
}
```

- 座標は Unity と同じ: 床面は (x, z)、yaw は度で、yaw=0 の正面が +z、yaw=90 が +x（前方ベクトル = (sin yaw, cos yaw)）。
- 扇形の判定は、定位置と、正面 -45°〜+45° を 15° 刻みにした半径 Reach の点（7点）が、すべて安全範囲の正方形（エリアの向きで回した座標、境界上は内側に含める）の内側にあることとする。

**計画の修正（2026-09-24、計画担当）**: 当初は前方の半円（±90°）としたが、Sol が「余裕を引いた正方形（半辺0.7）の角では、半円の両端（幅0.9m）がどちらかの辺を必ず越え、どの向きでも収まらない」ことを指摘（15°刻み全24方向で0件を確認）。計画担当の誤り。必要な動きの領域を前方 ±45° の扇形に狭める。角 (0.7,0.7) から中心向き（225°）なら両端 (0.7,0.25)・(0.25,0.7) が内側に入るので、四隅でも収まる向きがある。テスト3は (±0.7, ±0.7) のまま。

## テスト（新規 `Assets/LoopRoom/Editor/RoomAnchorChecks.cs`、LoopModelChecks と同じ書き方）

1. 中心・どの向きでも収まる（補正なし）。
2. 境界ぎりぎりで外を向いている → 補正され、返した向きで ForwardRegionFits が true、かつ補正量が最小（1刻み手前の向きは収まらない）。
3. 四隅（±0.7, ±0.7）でも、必ず収まる向きがある（fits=true）。中心方向に近い向きになる。
4. エリアの向き areaYawDeg が 0 以外（例 30°）でも正しく判定する。
5. 安全範囲の外に立っている（例 1.0, 0）→ fits=false、headYaw を返す。
6. 返す値が [0,360) に正規化される（負の入力、360 超の入力）。
7. 決定的（同じ入力で同じ出力）。

`Tests/LoopModel.Tests/LoopModel.Tests.csproj` に RoomAnchor.cs と RoomAnchorChecks.cs を Compile で加え、`Program.cs` で RoomAnchorChecks も実行して件数を表示する（GitHub CI の dotnet でも回る）。

## 許可ファイル

- `Assets/LoopRoom/Scripts/RoomAnchor.cs`（新規）
- `Assets/LoopRoom/Editor/RoomAnchorChecks.cs`（新規）
- `Tests/LoopModel.Tests/LoopModel.Tests.csproj`、`Tests/LoopModel.Tests/Program.cs`

## 受入条件

1. モデルテストと RoomAnchor のチェックが全件 PASS（Unity 付属 Roslyn）、GitHub CI success。
2. Unity batchmode コンパイル エラー0・警告0。
3. 別セッションの Sol と Sonnet の独立レビュー、交換後に両方 approve。
4. 証拠: テスト出力の画像。
