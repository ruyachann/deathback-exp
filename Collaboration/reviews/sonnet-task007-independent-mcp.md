# task007 独立レビュー

対象SHA256（提供スナップショットより引用）:
- `Assets/LoopRoom/Scripts/LoopModel.cs`: `dc0d1dc38a90c6fc74c70d4780feb160d2339d261b0d88121a9253f1b4685a30`
- `Assets/LoopRoom/Editor/LoopModelChecks.cs`: `05b8b2fddc3776849ad624858fa4786563d4925bc3cca1089e3f80b17371041d`
- `Tests/LoopModel.Tests/Program.cs`: `4d01d73bcfb82168332c4aaeadaaa17524d5419d45f4811e92ceedd975534e8d`
- `Assets/LoopRoom/Scripts/LoopDemo.cs`（参照用・未変更想定）: `9d6925b4e3dcd36edeb2d338f1f39bb9c479bebf0a2d2bfb4caa1380d0e39097`

## 設計適合性の確認

**1. `enforcePlayLimit` の追加とデフォルト**
`LoopRules` に `public bool enforcePlayLimit = false;`（LoopModel.cs 17行目付近）を追加。`Validate()` 内の7項目のNaN/無限大検査は全て維持されており、`playLimit + endingLength > 180.0` の判定のみ `enforcePlayLimit &&` で条件化されている。設計通り。

**2. `Advance()` の分岐**
```csharp
double untilLimit = Rules.enforcePlayLimit ? Rules.playLimit - TotalTime : double.PositiveInfinity;
if (untilLimit < 0.0000001) { End(SessionPhase.TimedOut); continue; }
```
`enforcePlayLimit=false` のとき `untilLimit` は常に `PositiveInfinity` となり、この判定は恒偽（`PositiveInfinity < 0.0000001` は成立しない）なので `TimedOut` に遷移しない。Blackout分岐の `Math.Min(BlackoutRemaining, untilLimit)` および Playing分岐の `Math.Min(next - LoopTime, untilLimit)` も、`PositiveInfinity` とのMinはIEEE754上安全に有限側を返すため、全経路で問題なし。`Rules.enforcePlayLimit && TotalTime >= Rules.playLimit - 0.0000001` のガードも `enforcePlayLimit=false` 時は恒偽になり、`TimedOut` 遷移を確実に抑止している。

`enforcePlayLimit=true` の場合は `untilLimit = playLimit - TotalTime` となり、既存の172秒TimedOut→8秒エンディング→180秒Finishedのロジックがそのまま再現される構造になっている。

**3. テストの検証**

`LoopModelChecks.cs` は13件のチェックを含み（数え上げにて確認）、Program.csの `existing.Count == 13` と一致。設計で要求された3項目は以下でカバーされている。
- 項目①（既定で長時間TimedOutしない）: `"Default rules keep looping without a session deadline"` で `Advance(1000)` 後に `Playing`/`Blackout` かつ `LoopId>1` を確認。
- 項目②（Validateの条件化）: `"Session deadline validation is conditional"` で `enforcePlayLimit=false` かつ `playLimit=180`（実質188>180）でValidateが通過し、`enforcePlayLimit=true` では例外になることを確認。
- 項目③（Interrupt/脱出+ending）: 既存の `"Shield grants a real escape window"`、`"Stop is distinct from death..."` が `new LoopModel()`（既定false）で実施されており、脱出・中断・Finished遷移が既定設定でも機能することを確認できている。

`Program.cs` 側は `new LoopRules { enforcePlayLimit = true }` を明示し、172秒TimedOut・180秒Finished・以降記録停止までを従来通り検証。`existing.Count == 13` と `"PASS: 14 model checks"`（13+Program.cs内1件）の数値も整合している。

## 指摘事項

| 重大度 | 箇所 | 内容 |
|---|---|---|
| Low | `LoopModelChecks.cs`「Session deadline validation is conditional」 | `enforcePlayLimit=true` で `playLimit+endingLength` がちょうど180になる境界値（180.0000超えない側）を明示的に検証していない。ただし `Program.cs` の172+8=180ケースおよびLoopModelChecksの「Deadline includes blackouts...」（`enforcePlayLimit=true` の既定値172/8を使用）で間接的にカバーされているため実害は小さい。 |
| Info（未検証・要実機確認） | シーン `LoopRoom.unity` の `playLimit:172` 保存値と新フィールドの既定反映 | Unity YAMLシリアライゼーションはフィールド未存在時にC#側の初期値（`false`）を使うのが標準動作であり、設計上の前提は妥当と考えられる。ただし本レビューはソースコードのみが対象でシーンYAMLの内容は提供されておらず、実際のシーンファイルの中身およびUnity Editor上での挙動は確認できていない。 |
| Info | `LoopDemo.cs` | 変更なし（参照用として提示された通り）。`timings` フィールドの扱いにも変更影響は見当たらない。 |

重大な論理欠陥（無限ループ化、境界条件での不正な状態遷移、既存の周回・死亡・Blackout・脱出・中断・エンディング挙動への回帰）は確認されなかった。

## 判定

**approve**

## 未検証事項

- `dotnet` によるテストの実行結果（本レビューではソース解析のみで実行はしていない）
- Unity Editor / batchmodeでのコンパイル確認（エラー0・警告0）
- `Assets/LoopRoom/Scenes/LoopRoom.unity` のYAML内容（`playLimit:172` 保存と新フィールド既定値の実際の反映）
- Editor Play（デスクトップ）での180秒超過セッション継続の実機確認
- 他レビュアー（実装者gpt-5.6-solおよび並行レビューのCodex）の報告内容（本レビューは独立判断として作成し、他レポートは未参照）