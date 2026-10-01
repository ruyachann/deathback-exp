# task015 独立レビュー（クオリティ第1弾: 音と案内パネル）

対象SHA256:
- `Assets/LoopRoom/Scripts/ProceduralAudio.cs` = `6e020145a1e1877d5275539d12fd32eb5617ef9446800dd7fab225abb1e47b0f`
- `Assets/LoopRoom/Scripts/LoopDemo.cs` = `2a85b749348d7cc179bd8602982a52c7bff1f5cc972f086dc6a6c02f9295c516`
- `Assets/LoopRoom/Scripts/RoomVisuals.cs`（参照用、変更なし）= `80142550a2080983266e673b40b9de6684119f715c021ed474e2b7282d2334fc`
- `Collaboration/tasks/015-quality-audio-and-panel.md` = `bfe65b6b40aa4d691aa71f20a4d84be8371df52c940c24b21cf3e0e4080d411d`

本レビューはスナップショット記載のソースのみを根拠とし、テストやビルドは実行していない（実行したとは主張しない）。他レビューは読んでいない。

## Findings

### [Medium] LoopDemo.cs:87, 121-122 — `LoopRoom/Text` シェーダーのソース未提供のため `Color.clear` による計測用プレースホルダ非表示化が保証できない
- トリガー: `BuildMessage()` で `message.color=Color.clear` にした後 `RoomVisuals.TextMaterial(font)`（内部で `Shader.Find("LoopRoom/Text")`）をマテリアルに割り当てる。もしこのシェーダーが頂点カラー（アルファ含む）を参照しない実装であれば、計測完了（最大30フレーム）までの間、"国"×20文字×4行の巨大なプローブ文字列がそのまま可視状態で描画される可能性がある。
- 該当シェーダーファイル（`Assets/LoopRoom/Shaders/`）の内容はスナップショットに含まれておらず確認不能。
- 修正: シェーダーソースを添付するか、`message.gameObject.SetActive(false)` をBuildMessage末尾に追加し、`ApplyMessageSize` で明示的に `SetActive(true)` する等、シェーダー実装に依存しない非表示化を行う。

### [Low] LoopDemo.cs:88-94 — プローブ文字列（20全角文字×4行）が実際の最悪ケースを網羅しているか未確認
- トリガー: コメントで「rig.PreparationMessage」を含む全メッセージの最長行が20文字・最大4行と主張しているが、`DemoRig.cs`（`rig.PreparationMessage`, `rig.CanStart` 等の定義元）はスナップショットに含まれておらず、この主張の妥当性を検証できない。もし実運用のメッセージがこれを超える行を含む場合、追修正3の「幅0.56m以内」を超えてパネル外にはみ出す可能性が残る。
- 修正（提案）: `DemoRig.cs` を含めて再レビューするか、`rig.PreparationMessage` の最長行を含めた文言一覧をタスクに明記する。

### [Low] RoomVisuals.cs（変更なし） + LoopDemo.cs:52-53 — `RoomVisuals.Build()` 内の `Tone()` 呼び出しが無駄
- トリガー: `room.Build()` で `Chime=Tone(...)` 等が生成された直後、`LoopDemo.Start()` の52-53行目で即座に `ProceduralAudio.*` へ上書きされる。機能上の欠陥はないが、起動時に不要な波形生成（4クリップ分）が行われる。
- タスク文書に「RoomVisuals.cs は編集しない」と明記されているための許容されたトレードオフであり、修正は不要と判断。参考情報として記載。

### [Info] LoopDemo.cs:194付近（`RefreshWorld()`） — `message.text` を毎フレーム再代入
- トリガー: `messageSized` が true の間、`RefreshWorld()` は同じフェーズが続く限り同じ文字列を毎フレーム `message.text` に再代入しており、TextMeshのメッシュがフレーム毎に再生成されうる。既存コード（`room.Clock.text` 等）と同じパターンであり、性能上軽微な指摘に留まる。修正不要。

## 検証できた点（コード読解ベース）

- `ProceduralAudio.RoomTone()`: `humA=55Hz`, `humB=58.5Hz`, 高調波 `freq[h]=k*fundamental`（`fundamental=1/4`）は全て `loopSeconds=4s` に対して整数サイクル（`55*4=220`, `58.5*4=234`, `k*fundamental*4=k`）となり、証跡ログにある「継ぎ目 0.0015」相当のクリックレス設計は理論上妥当。
- 各 `Normalize(samples, peak)` の `peak` 値: Chime=.5f, Latch=.6f, Shot=.65f, Open=.55f, RoomTone=.18f — いずれも証跡記載の「peak<=0.65」を満たし、`Normalize` の実装上クリッピング（|sample|>1）は発生しない。
- `ambience`（環境音）は `room.Sound` / `enemyAudio` とは別の `AudioSource` として生成されており（LoopDemo.cs:65-68）、周回切替時の `room.Sound.Stop(); enemyAudio.Stop();`（Update()内）では停止しない。`ambience.volume=.12f` は `room.Sound.volume=.25f`、`enemyAudio.volume=.22f` より小さく、`spatialBlend=0` で空間化していない。仕様どおり。
- 案内パネル計測: `LateUpdate()`（LoopDemo.cs:97-105）は `MeshRenderer.localBounds`（`MeshFilter`ではなく）を使用し、`renderer==null` または `size.x/y<=0` の場合は計測をスキップして次フレームへ委ねる。30回失敗で固定値 `(0.62, 0.26)` にフォールバックし、`characterSize` は縮めない（`fixedFallback` 時は `shrink` 計算をスキップ）。仕様（追修正2・3）と一致し、`try/catch` なしでも例外要因（null参照）は事前にnullチェックされている。
- 縮小率 `shrink=Mathf.Min(1f, 0.56f/bounds.x)` をパネル・文字双方に適用し、余白0.05mを加算（LoopDemo.cs:114-120）。`Debug.Log` は測定/固定の別と `bounds`・`shrink` を1回だけ出力（`messageSized=true` 到達後は `LateUpdate` が即return するため2回目は発生しない）。
- テキスト表示位置: パネル `z=0.82`、文字 `z=0.80`（パネルより0.02m手前）で、コメント通り文字が隠れない配置。

## Verdict

**approve**（ただし下記「未検証事項」の解消を推奨。Medium指摘はブロッキングではないが、シェーダー実装の確認を強く推奨）

## 未検証事項（unverified checks）

1. `Assets/LoopRoom/Shaders/`（`LoopRoom/Text` シェーダー）のソースコード未提供のため、`Color.clear` によるプレースホルダ非表示化の実効性は未確認。
2. `Assets/LoopRoom/Editor/DemoRig.cs`（`rig.PreparationMessage`, `rig.CanStart`, `rig.View` 等）未提供のため、プローブ文字列（20文字×4行）が実運用の最悪ケースを網羅しているか未確認。
3. Unity batchmode コンパイル（エラー0・警告0）は未実施。
4. `evidence/20260924-quality1/waveforms.png` および該当スクリーンショット画像そのものは本スナップショットに含まれず、目視確認はしていない（ログ記述・コード上の数値との整合性のみ確認）。
5. 実際の聞こえ方（音量バランス・不快さ）はタスク文書記載通り未確認（人による確認待ち）。
6. Editor Play / ビルドでの実行結果（例外0件、ログ出力の実測）は未実施。