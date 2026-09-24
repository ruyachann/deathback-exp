# Sonnet task023 再判定（claude -p 直接、claude-sonnet-5、Read/Grep/Glob のみ）

## task023 交換後レビュー（再判定）

**対象ファイル SHA256**
- `Assets/LoopRoom/Scripts/RoomVisuals.cs`: `af880c9c2e268e4f517586ca348336cb76d235e7cd028f0837275326ad9406ef`（提示値。ハッシュ計算ツールが手元にないため実測での再計算はしていないが、タスク文書・Sol レビュー・自分の前回レビューの記載と一致しており矛盾はない）

### 前回指摘①〜③の結論

**① 見送りを Sonnet 本人が承認していない**
→ **解消**。前回は Opus の反証を Sonnet が直接読んで判断した記録がなかった点を問題視したが、今回このレビュー自体が本人（私）による比較画像・テスト結果の直接確認と再判定にあたる。以下の確認をもって私自身が見送りを追認する。

**② 光量の加算は URP の物理ライト単位設定が前提**
→ **解消**。`ProjectSettings/GraphicsSettings.asset` の `m_LightsUseLinearIntensity: 1` はガンマ/リニア色空間での強度計算に関するものであり、Physical Light Units（lumen/lux 単位）とは別設定。実際に `Assets/Settings/PC_RPAsset.asset` および `GraphicsSettings.asset` 全体を確認したが、物理ライト単位に関するフィールド（`useAdditionalLightsPhysical` 等）自体が存在せず、`RoomVisuals.cs` 内でも `light.lightUnit` 等の設定は一切していない。したがって本プロジェクトの URP（Unity 6000.3.15f1）では Point/Spot とも同一の無単位 intensity・同一の距離減衰式（逆二乗）で扱われ、Spot はコーン角減衰が加わるのみで `innerSpotAngle` の内側（110°）では減衰しない。task023.md の反証「Spot 2.6＋Fill 0.6＝3.2＝従来の Point 3.2」の前提は成立していると確認できた。

**③ 証拠がレビュー対象に含まれていない**
→ **解消**。`Collaboration/evidence/20260925-light-focus/light-compare.png`（変更前後の同一構図比較）と `tests.txt` を直接閲覧した。

### 実行した確認
- `light-compare.png`（および元画像 `light-before-point.png` / `light-after-spot.png`）を目視。左右で壁・机・絵画・ドアの明るさ、色味、床のコントラストはほぼ同一。机の下に落ち影が両方の画像に存在し、消えていない。
- `tests.txt` を確認。`csc exit=0`、全 PASS 行、`test exit=0` を確認（batchmode 0/0 相当）。
- `ProjectSettings/GraphicsSettings.asset` と `Assets/Settings/PC_RPAsset.asset` を確認し、Physical Light Units 関連フィールドが存在しないことを確認。
- `RoomVisuals.cs` 198〜213行目のコードを確認。Spot は真下向き（`Quaternion.Euler(90,0,0)`）、`spotAngle=140`／`innerSpotAngle=110`（許容 140〜160° の下限内）、`shadows=Soft`／`Medium`。補助 Point（`ceilingFill`）は `shadows=None` で6面影の再導入なし。窓の Spot（`fill`）とレンダラー設定（本ファイル内に変更なし）は据え置き。

### 未確認事項
- 提示された比較画像は LOOP 開始直後（S01 1.5〜2.0s）のフレームで、Enemy が出現する前のカットのため「敵の影」の変更前後比較はこの証拠だけでは目視できない。なお `RoomVisuals.cs` 上、Enemy を構成する各パーツ（Coat/Head/Visor/Weapon/肩/帽子つば）はいずれも `hidden:true` で生成されており、`Shape()` の実装上 `shadowCastingMode = ShadowCastingMode.Off` が設定される。これは今回の diff（198〜213行目）の外側で以前から変わっていない挙動であり、本タスクによる新規劣化ではないと考えるが、「敵の影が残っていること」という受入条件2の文言との整合は計画・実装担当側での確認が望ましい。
- 実機（Quest 3/3S、Air Link）でのフレーム落ち・GPU負荷は、タスク文書にも明記の通り未確認。
- URP Renderer の追加ライト数上限に対する Spot＋Fill Point＋窓 Spot 3灯の実際の描画コストは未計測（Frame Debugger等の実測はしていない）。

### 判定

**approve**

前回指摘した①〜③はいずれも今回の確認で解消したと判断する。コード（`RoomVisuals.cs` 198〜213行目）自体に技術的欠陥は見当たらず、比較画像・テスト結果の直接確認も完了した。Sol も交換後 approve 済みのため、本レビューをもって受入条件3（両者 approve）は満たされる。ただし上記「未確認事項」（敵の影の実機的な見え方、実機負荷、追加ライト数の実測）は受入後もフォローが必要な点として記録しておく。