# 015 — クオリティ第1弾: 音と案内パネル（計画8）

状態: **受入済み（2026-09-24、実機の見え方・聞こえ方は計画6で確認）**。結果は `reviews/tasks014-016-exchange-20260924.md`。計画確定（2026-09-24）。計画 Claude Opus 5.5。実装 Claude Sonnet 5（`claude -p` 別セッション）。レビュー 実装とは別セッションの Sonnet と GPT-5.6 Sol。

## 目的

1. **音**: 今の音は `RoomVisuals.Tone()` の単純なサイン波・ノイズ（チャイム660Hz、射撃80Hzノイズ、足音170Hz、扉880Hz）。外部の音声ファイルを使わず、コードの合成で「それらしい」音にする。部屋の環境音（低いうなり・空調）も加え、静かすぎる空間をなくす。
2. **案内パネル**: 2026-09-24 の Editor Play で、開始待ち・終了後の案内パネル（`Ready and ending panel`、カメラ前 0.80–0.82m）の文字がパネルと画面からはみ出した（evidence/20260924-editor-play/03）。文字がパネルの内側に収まり、HMD でも desktop でも読めるようにする。

## 設計

- 新規 `Assets/LoopRoom/Scripts/ProceduralAudio.cs`（static クラス、UnityEngine のみ依存）に合成関数を置く。サンプルレートは 44100。すべて決定的（乱数は固定シード）。
  - `Chime()`: 周回の始まり。倍音を持つ鐘の音（基音＋非整数倍音、長めの減衰）。
  - `Latch()`: t=3 の足音／遮蔽の音。低いドスン（短い低音＋短いノイズ）。
  - `Shot()`: 射撃。鋭い立ち上がりのノイズ＋低い衝撃音、短い残響感。
  - `Open()`: 出口の解錠。金属的な2音。
  - `RoomTone()`: 環境音のループ（数秒、継ぎ目が目立たないようループ点をそろえる）。低いうなり＋薄いノイズ。音量は小さく。
- `LoopDemo.Start()` で `room.Build` の後に `room.Chime = ProceduralAudio.Chime()` のように差し替える（RoomVisuals.cs は編集しない。`RoomVisuals.Tone()` はそのまま残す）。環境音用の AudioSource を Root 下に1つ作り、`loop=true` で再生する。音量は既存の効果音より小さく、空間化はしない（またはごく弱く）。暗転中も鳴り続けてよい。
- 既存の再生の仕組み（どの出来事でどの音をどの AudioSource で鳴らすか、Latch の t=3 判定、周回切替時の Stop）は変えない。ただし周回切替の `room.Sound.Stop(); enemyAudio.Stop();` で環境音が止まらないよう、環境音は別の AudioSource にする。
- 案内パネル（`BuildMessage()`）: パネルの大きさと文字の大きさ（`characterSize`、`fontSize`）を見直し、最長の文言（desktop の「第零室 / 操作確認 …」3行、VR の準備メッセージ4行）がパネル内に収まるようにする。カメラからの距離（0.8m 前後）と位置は大きく変えない。必要ならパネルを少し下げ、文字は左右中央揃えのまま。

### 追修正（2026-09-24、計画担当 Opus 5.5。ビルドの自動実行で判明）

ビルド（`--desktop --autostart --autoescape`）で `NullReferenceException at LoopDemo.BuildMessage () LoopDemo.cs:90`。TextMesh は text を入れた直後にメッシュを作らない（描画時に作る）ため、`MeshFilter.sharedMesh` が null になる。例外で `Start()` が途中で止まり、パネルと文字の位置・大きさが未設定のまま →「脱出した。」も終了後の案内も表示されない（evidence/20260924-quality1/frames）。
- 修正: 計測を遅らせる。BuildMessage では仮の文字（最悪ケース）を入れて文字色を透明にし、パネルは非表示のまま。`LateUpdate`（または RefreshWorld の先頭）で、まだ計測していなければ `sharedMesh` が null でなく頂点がある場合にだけ bounds を測ってパネルを合わせ、文字色を戻し、以後は測らない。
- 数フレーム（例 30 フレーム）たっても測れない場合は、固定の大きさ（幅 0.62、高さ 0.26 程度）にする。
- 例外を出さないこと。Start() は最後まで実行されること。

### 追修正2（2026-09-24、計画担当 Opus 5.5）

追修正1のビルドで `NullReferenceException at LoopDemo.LateUpdate () LoopDemo.cs:102` が毎フレーム（Player.log に 25,967 件）。**TextMesh には MeshFilter が無い**（描画は MeshRenderer のみ）ため `GetComponent<MeshFilter>()` が null。最初の例外（BuildMessage:90）も同じ原因で、計画担当の「メッシュがまだ作られていない」という見立ては誤りだった。
- 計測は `message.GetComponent<MeshRenderer>().localBounds.size`（Renderer.localBounds、ローカル座標でカメラの向きに依存しない）で行う。renderer が null、または size の x・y が 0 なら計測しない。
- 30 フレームで固定の大きさに戻す仕組みは残す。例外を出さない（LateUpdate の中でも null を確認する）。
- 計測したことを1回だけ `Debug.Log`（測った大きさと、計測か固定かの別）に出す（ビルドのログで確認するため）。

### 追修正3（2026-09-24、計画担当 Opus 5.5）

追修正2で例外は0件、計測も成功したが、測った大きさが `(1.86, 0.53)`（カメラ前 0.82m で幅 1.86m）。パネルを文字に合わせることはできたが、文字自体が視野より大きい（frames/f06 ではパネルが画面の外まで広がる）。
- 最悪ケースの行の幅が **0.56m 以内**（0.82m 先で左右およそ ±19°、楽に読める範囲）になるよう、測った幅から縮小率 `k = min(1, 0.56 / 幅)` を求め、`message.characterSize` に掛ける。パネルは `測った大きさ × k + 余白 0.05m`。
- 固定の大きさに戻す場合も、幅 0.62・高さ 0.26 のままでよい（文字は縮めない）。
- 案内パネルの文字の素材は task014 追修正3で追加される `RoomVisuals.TextMaterial(font)` を使い、奥行きを考慮して描く（パネルの前 0.02m にあるので隠れない）。
- Debug.Log に縮小率も出す。

### 追修正4（2026-09-24、独立レビュー指摘。計画担当 Opus 5.5）

Sol request_changes（`sol-task015-independent-mcp.md`）、Sonnet approve。採用: (1) 計測が成功したフレームに、仮の文字（「国」4行）が1フレームだけ見える → 計測したフレームでは透明のまま大きさだけ確定し、次の RefreshWorld で実際の文言・パネル表示・文字色の復元を同時に行う。(2) 固定サイズに戻す場合に余白 0.05 が足されて 0.67×0.31 になる → 固定の場合は 0.62×0.26 をそのまま使う（余白は計測時のみ）。

### 追修正5（2026-09-24、再レビュー指摘。計画担当 Opus 5.5）

再レビュー: Sol approve、Sonnet request_changes（`sonnet-task015-rereview-mcp.md`）。
- 不採用（Sonnet 高「material 差し替えで Color.clear が消え仮の文字が見える」）: TextMesh の color は素材の色ではなく頂点カラーで保持される。変更前のコードも「color を設定 → material を差し替え」の順で、文字は指定の色（アイボリー）で表示されていた（evidence/20260924-quality-before）。
- 採用（Sonnet 中）: 固定サイズに戻す場合に文字が縮まらず、0.62×0.26 のパネルからはみ出す。固定サイズのときは `characterSize` を開発機で測った縮小率に合わせて `0.0145 × 0.30 ≈ 0.0044` にする（ログに固定サイズであることと値を出す）。

## 許可ファイル

- `Assets/LoopRoom/Scripts/ProceduralAudio.cs`（新規、.meta は Unity が作る）
- `Assets/LoopRoom/Scripts/LoopDemo.cs`

RoomVisuals.cs は task014 が同時に編集するので触らない。

## 受入条件

1. Unity batchmode コンパイル エラー0・警告0。
2. 証拠: 合成した各音の波形またはスペクトルの画像（長さ・最大振幅・クリップしていないこと）と、案内パネルが収まった画面のスクリーンショット（task016 の自動開始オプションか Editor で撮影）。
3. 別セッションの Sonnet と Sol の独立レビュー、交換後に両方 approve。
4. 実際の聞こえ方（音量バランス・不快さ）は人の耳で確認する（ユーザー、計画6で合わせて確認）。未確認と明記。
