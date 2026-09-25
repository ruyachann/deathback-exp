# task018 独立レビュー（追修正2 + 受入条件2 書き直しの確認）

対象SHA256（供給スナップショットより）:
- `Assets/LoopRoom/Scripts/LoopDemo.cs` = `f06bf6dae7f469118a6b349dc2e54834b80f22c0244858a5cde39bbd327975cd`
- `Assets/LoopRoom/Scripts/DemoRig.cs` = `a111a68c8cdb10d969cff70aada005ca691ae2297d4e015c5a62d4cbea5882d5`
- `Assets/LoopRoom/Scripts/RoomAnchor.cs` = `510674914e8105600e0c800c826b2f2a9828d6cdc8e90207675c2bb5aee998e0`
- `Collaboration/tasks/018-room-anchor-integration.md` = `0317c1cb272bcc26627952974af463ff3431e1e733afa5e1fe3698c242c4a3cc`

## 追修正2 の実装確認

1. **Cキー条件（idle && rig.CanStart のみ受付）**: `LoopDemo.cs` の該当ブロック（`if (idle && rig.CanStart && keyboard!=null && keyboard.cKey.wasPressedThisFrame)`）で正しく実装されている。desktop/VR切替検出（`if(rig.IsVR!=wasVR) aligned=false;`）とR再準備後の`aligned=false`も実装済み。**採用済み・妥当。**
2. **fits=false警告を1回に絞る**: `fitsWarned` フラグが `PlaceRoom()` 内で1回のみ`Debug.LogWarning`を出し、Cキー押下時に`fitsWarned=false`でリセットされる。**採用済み・妥当。**
3. **RoomAnchor 計算の手計算検証**: `DemoRig.cs` の `DriftOffsets`/`DriftYaws`（(.15,.10)/20°, (.55,.35)/40°, (-.20,-.05)/-30°, (-.60,-.40)/-120°）について、`RoomAnchor.ForwardRegionFitsCore` の式（SafeHalfSize=0.8, Margin=0.10→halfSize=0.7, Reach=0.45）を手計算で追跡したところ、1番目・3番目（小さいずれ）は`fits=true`（補正なし）、2番目・4番目（大きいずれ）は`fits=false`（補正発生）という、意図した「小・大・小・大」の交互パターンに一致した。**設計どおりに機能していると判断。**

## 新規に見つけた懸念

**[中] `Begin()` とループ変化検出の両方で `PlaceRoom()` が呼ばれる可能性（LoopDemo.cs `Begin()` および `Update()` のLoopId変化ブロック）**
`Begin()` は `PlaceRoom()` を呼んだ直後に `lastLoop=0` を設定し `Model.Start()` を呼ぶ。同一 `Update()` 呼び出し内で続く `Model.Advance()` の結果 `Model.LoopId` が0から変化していれば、`Model.LoopId!=lastLoop` が真になり、同じフレーム内で `simulateDrift`＋`PlaceRoom()` が再度呼ばれる。この場合 `PlaceRoom` のログ（"corrected="/"fits="含む）が1フレームに2行出力され、1行目は drift 適用前（誤った暫定値）になる。受入条件2はこのログを証跡として使う設計のため、判読を誤らせる可能性がある。
- 実害: 視覚的には暗転中のため問題なし（2回目の配置が最終結果として正しい）。
- 根拠が `LoopModel.cs`（未供給）の `LoopId` の初期化・更新タイミングに依存するため、実際に毎回発火するかは未確定。
- 修正案: `Begin()` 側の `PlaceRoom()` 呼び出しを削除し、初回配置もループ変化検出ブロックに一本化する（`lastLoop=0` のリセットのみ残す）。

**[低] Enter と C の同時押しでの競合（LoopDemo.cs `Update()` 冒頭）**
`idle` はフレーム先頭で1度だけ計算されるローカル変数のため、同一フレームで Enter による `Begin()` 実行後も、その後の C キー判定の `if (idle && rig.CanStart && ...)` は古い `idle=true` のままで評価され、周回開始直後の頭位置で `alignCx/alignCz/alignAreaYaw` が上書きされ得る。R キーの分岐は `else if` で Enter と排他だが、C の分岐は独立した `if` のため排他になっていない。
- 修正案: C の判定も `else if` チェーンに含めるか、`Begin()` 実行フラグで当該フレームのC処理をスキップする。

いずれも致命的ではなく、安全性・不変条件（即死/暗転/復帰順、XR原点固定等）には影響しない。

## 未確認事項（Unverified）

- `RoomVisuals.cs`（ラグ追加、`room.Sound` の `room.Root` 子化）はスナップショットに含まれておらず確認不可。
- `LoopModel.cs`（`LoopId` の設定タイミング）未供給のため、上記「中」指摘の実際の発火有無は未確定。
- 受入条件1（batchmode コンパイル エラー0/警告0、モデルテスト全件PASS）は本レビューでは実行・確認していない。
- 提示された具体的ログ値「小さいずれ corrected=False、大きいずれ corrected=True diff -135/165」について、手計算による概算検証を試みたが（4番目のドリフトで `diff≈-135` に相当する `frontYaw≈105°` を検算したところ `fits=false` の再判定になり、一致を確定できなかった）、実機/Unity実行によるログの再確認はしていない。数値自体は計画担当の記録であり、本レビューでは追試できていない。
- Player.log の例外0、スクリーンショットの実物確認は未実施。

## 判定: **approve**

追修正2の3項目（Cキー条件、モード切替/R後のリセット、fits警告の抑制）はコード上正しく実装されており、RoomAnchor計算も手計算検証で意図どおりの補正パターンとなることを確認した。見つかった2件（Begin()二重呼び出しの可能性、Enter/C同時押しの競合）は低〜中程度で、体験の不変条件や安全性には影響しないため、次の修正サイクルでの対応を推奨するに留め、本レビューでは承認とする。ただし未確認事項（RoomVisuals.cs、LoopModel.cs、実機/Unity実行結果、ログ数値の実測）は最終受入前に他の担当による確認が必要。