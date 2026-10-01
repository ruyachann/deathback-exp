# task020 独立レビュー

対象SHA256:
- task020: `dabe3c1b78b4b74ca9c0ae5972dac7499cdbd5f70ebdf8e2de0c536ee49d8d48`
- PlayAreaSettings.cs: `9fb475cc77ab33e40448a535d210047e70fb7f6069513a1eff69958eed482217`
- PlayAreaSettingsFile.cs: `0d0e8190b27386cdabdde4f1f1cc9e8d16ad6883849f3182d951757024dac9a6`
- RoomAnchor.cs: `3c13ee4504adc680f6fb52a804709fc2a22d3a52a3faa0e292b02e9c10a9b86a`
- RoomAnchorChecks.cs: `06912abd957e2d3d0c92199d31859b209d30af88d162af53ebe8149c0cd667ad`

## 指摘事項

**[Low] PlayAreaSettingsFile.cs 全体 — LoadOrCreateの異常系が未検証**
「不正なら既定値・警告1回・上書きしない」という受入基準はコード上で意図通りに書かれているが、RoomAnchorChecks.csにこの分岐を検証するテストが1件もない。Unity の `JsonUtility.FromJson` は空文字列等に対して例外を投げずデフォルト相当のインスタンスを返す実装既知の癖があり、壊れたファイルが静かに「正常読込」扱いになるケースが理論上ありうる。
修正案: 存在しないパス／壊れたJSON／範囲外値を含むJSONの3パターンをEditModeテスト等で追加検証する。

**[Low] RoomAnchor.cs:19 `DefaultSettings` が mutable な共有 static インスタンス**
`PlayAreaSettings` は public フィールドを持つ可変クラスなので、理論上どこかで `RoomAnchor.DefaultSettings` 相当の参照を経由してフィールドが書き換われば、定数版APIすべての挙動に影響する。現状書き換え箇所はないが設計上のガードがない。
修正案: 定数版呼び出し毎に `new PlayAreaSettings()` を生成する、または `PlayAreaSettings` を不変にする。

**[Info] RoomAnchor.cs:9 `SafeHalfSize`(=0.8) の命名がmarginを含まない値で紛らわしい**
実際の有効境界は `areaSize/2 - margin = 0.7` であり、`SafeHalfSize` という名前から連想される「安全な半径」そのものではない。task017由来で本タスクの変更範囲外だが、RoomAnchorChecks.cs内で `SafeHalfSize - Margin` という式を使っており誤読を招きやすい。実害はなし。

**[Info] 計算ロジックの手検証は妥当**
`ForwardRegionFitsCore`/`ChooseFrontYaw` の DefaultSettings 経由の値(areaSize=1.6, margin=0.10, reach=0.45, halfAngleDeg=45, searchStepDeg=15)は既存定数と一致しており、代表ケース(中心・境界・四隅・回転座標系)を手計算で追った限り、既存定数版と新オーバーロード版の結果は一致する。areaSize=2.4への変更でhalfSizeが0.8→1.1に緩和され、`pz=0.6` が補正不要になる点も式上整合している。
`RoomAnchor.cs` 自体は `using System;` のみで UnityEngine非依存、`PlayAreaSettingsFile.cs` にのみ `UnityEngine` 依存が分離されている点は設計通り。

## 未確認事項（本レビューでは検証不能)

1. `Tests/LoopModel.Tests.csproj` / `Program.cs` の内容がスナップショットに含まれておらず、「PlayAreaSettings.csの純粋部分は含め、PlayAreaSettingsFile.csは除外」という要求が実際に反映されているか確認不可。
2. 「19+12 PASS、ビルド0/0」というテスト実行結果は伝聞であり、本レビューでは一切のビルド・実行を行っていないため未検証。
3. LoadOrCreateのUnity実機での実際のI/O・`JsonUtility`挙動は未検証（コード読解のみ）。
4. `Docs/DEVICE_QUICKCHECK.md` への追記(計画担当の作業)はスナップショット外で確認不可。
5. `RoomAnchor.SafeHalfSize` を参照する他ファイル(キャリブレーションUI等task021側)との整合性は未提供のため未確認。

## Verdict

**approve**（指摘はいずれも Low/Info、機能上のブロッカーは見当たらない。ただしcsproj内容とテスト実行結果は未検証のため、その点は別途実機/CI証跡での裏付けを推奨）