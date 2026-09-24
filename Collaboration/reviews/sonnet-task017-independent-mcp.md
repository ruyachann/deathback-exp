# 独立レビュー: task017 RoomAnchor（部屋の置き直しの計算）

対象タスク: `Collaboration/tasks/017-room-anchor-math.md`（SHA256: `dd3fc36b1cb6fadfaf951fda4fd35f14bba20bf2533832b827137042008859f1`）

## 検証範囲

- `Assets/LoopRoom/Scripts/RoomAnchor.cs`（SHA256: `3e7ad288c8f5ca83b53311cc012e961ad86471e02b4cae368ee8457dcf769bb1`）
- `Assets/LoopRoom/Editor/RoomAnchorChecks.cs`（SHA256: `620cf7e172847b694484bc18e5f1d7d7747b23b824ed20ae640a63e2ea80f5be`）
- `Tests/LoopModel.Tests/Program.cs`（SHA256: `98ffef6894adf25ca1dcfea8bd10b079097358b82e2c552f9a73cfa663cdbe4f`）
- `Tests/LoopModel.Tests/LoopModel.Tests.csproj`（SHA256: `f1aa05664a61273668ca58dea482cc24e8d03524f29f3e37704693fd9ea6403b`）

## 数式・ロジックの検証（手計算）

**座標変換（`PointFits`）**: エリアが `areaYawDeg` 回転しているときのワールド→ローカル変換を、Unity 座標系（yaw=0 が +z、前方ベクトル=(sin,cos)）で導出し直したところ、
`localX = dx*cos - dz*sin`、`localZ = dx*sin + dz*cos` はタスク仕様の座標系と整合していることを確認した。`Assets/LoopRoom/Scripts/RoomAnchor.cs:73-84` のコードと一致する。

**扇形判定（`ForwardRegionFits`）**: 定位置1点＋オフセット `-45,-30,-15,0,15,30,45`（15°刻み7点、`Assets/LoopRoom/Scripts/RoomAnchor.cs:19-31`）で、タスク仕様「定位置と…7点が安全範囲内」と一致。`BoundaryTolerance` によりループ終端 45° と境界の浮動小数点誤差を吸収しており妥当。

**探索（`ChooseFrontYaw`）**: `offset=15..165`（15刻み、`<180`）で positive→negative の順に試し、最後に `opposite=headYaw+180` を単独で試す構造（`Assets/LoopRoom/Scripts/RoomAnchor.cs:35-63`）。headYaw自身(1) + 11刻み×2(22) + opposite(1) = 24 = 360/15 で、24方位を重複なく全カバーしていることを確認した。「同じ近さなら+側を先」もループ内で positive を先に判定しているため仕様通り。

**代表テストケースの手計算再現**:
- テスト2（境界`(0,0.7)`、headYaw=0）: offset=135 の positive(135°) は扇形内 yaw=90 の点で x=1.15>0.7 となり不適合、続く negative(-15=345 相当ではなく225°ではない点に注意)ではなく、同offsetの negative は該当なし…実際は offset=135 の positive が falseのためこの段で negative は未評価とはならず、コード上は同offset内で positive→negative の順に評価される。手計算では offset=135 の positive(135°) は前述の通り不適合、ただしテストが期待する解は135°自体（`Near(chosen,135)`）であり、実際に yaw=135° の扇形は絶対yaw範囲90°〜180°で、その中の最大 z 寄与は yaw=90° で z=0.7（境界一致）、x=0.45（適合）と算出され、全7点+定位置が収まることを確認した。上記の「x=1.15」という記載は誤り（sin(90)=1なので `x = 0 + sin(90)*0.45 = 0.45` が正しい）。訂正して再確認した結果、offset=135 の **positive** で初めて適合し、`chosen=135` が返ることを確認、テストの期待値と一致する。
- テスト3（四隅 `(0.7,0.7)` など）: `centerYaw=225°` に対し、offset=135 の positive(135°) は yaw=90° の扇形点で x=1.15>0.7 となり不適合、negative(225°) は yaw=180°〜270° の全点・定位置が安全範囲内に収まることを確認し、`offset=135` の negative で初めて適合、`chosen=225°=centerYaw` と一致することを確認した。

以上より、実装のロジックと主要テストケースの期待値は数学的に整合している。

## 所見（Findings）

| 深刻度 | 箇所 | 内容 |
|---|---|---|
| 情報 | `Assets/LoopRoom/Editor/RoomAnchorChecks.cs:47-63`（テスト3） | `Near(AngularDistance(chosen, centerYaw), 0)` は「厳密に中心向きと一致する」ことを要求しているが、これは `ChooseFrontYaw` の探索順序（positive優先・15°刻み）と `Reach=0.45`／`Margin=0.10` の現在値に依存した結果である。将来これらの定数を変更した場合、四隅で「中心向きに一番近いが完全一致しない」向きが選ばれてテストが壊れる可能性がある。現状のパラメータでは問題ないため blocking ではない。 |
| 情報 | `Tests/LoopModel.Tests/Program.cs:9` | `existing.Count == 18` の期待値および `"PASS: 19 model checks"` の固定文字列は task017 の変更範囲外（`LoopModelChecks.cs` 自体は本スナップショットに含まれていない）。18件という数の妥当性は本レビューでは検証不能。 |

重大なバグ（座標変換の誤り、境界の考慮漏れ、正規化の誤り、探索の方位抜け等）は見つからなかった。`SafeHalfSize - Margin = 0.7`、`Reach = 0.45 < 0.7` という定数関係も、タスク文書が言及する「角でも収まる向きがある」根拠と整合している。

## 検証項目（19+7 PASS, 0 FAIL）について

タスク説明にある「19+7 PASS、ビルド 0/0」という実行結果は、本レビューでは **実行していない**。座標変換とアルゴリズムの正しさは手計算で検証したが、実際の `dotnet run` 実行結果、Unity batchmode コンパイル結果、GitHub CI の成否は未確認。

## 未確認事項（Unverified checks）

- `dotnet run`（`Tests/LoopModel.Tests`）の実行結果（19+7 PASS の実数値）
- Unity Editor batchmode でのコンパイル（エラー0・警告0）
- GitHub CI の実行結果
- `Assets/LoopRoom/Editor/LoopModelChecks.cs` の内容（本スナップショット未提供のため、既存18件チェックの内容・正しさは検証対象外）
- task018（組み込み側）からの `RoomAnchor` API 呼び出しの正しさ（本レビューの対象外）
- 実機（Quest 3）での酔い・体感に関する評価（このタスクは純粋計算のみで対象外）

## 判定: **approve**

座標系・扇形判定・探索アルゴリズム・正規化のロジックはタスク仕様と数学的に整合しており、テストケースも代表的な境界・回転・四隅条件を妥当にカバーしている。上記の「情報」レベルの所見は将来のパラメータ変更時の保守性に関する留意点であり、現状の実装・テストを差し戻す理由にはならない。ただし実行結果自体（19+7 PASS、CI success）は未実施のため、別途実行確認が必要。