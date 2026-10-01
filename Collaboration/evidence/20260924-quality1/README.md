# クオリティ第1弾（task014・015・016a）の証拠（2026-09-24）

撮影: Claude Opus 5.5。Windows ビルド（Development、Unity 6000.3.15f1）を `--desktop --autostart --autoescape -screen-width 1280 -screen-height 720` で起動し、PrintWindow でプレイヤーのウィンドウだけを2秒おきに撮影（人の操作なし）。変更前は同じビルド手順で作った変更前のコードの `--desktop` 起動（`../20260924-quality-before/ready-screen.png`）。

| ファイル | 内容 |
| --- | --- |
| `before-after.png` | 変更前（開始待ち）と変更後（周回1の敵、周回2の脱出、終了後の案内パネル）の比較 |
| `frames/f00-…f13-…png` | 自動実行の全フレーム（約2秒間隔） |
| `text-depth-crop.png` | 奥の壁の文字が手前のパネル・遮蔽に正しく隠れることの拡大（修正前は「BEFORE」がパネルの上に描かれていた） |
| `waveforms.png` / `.txt` | 合成音5種の波形。実際の `ProceduralAudio.cs` を、UnityEngine の最小スタブと一緒にコンパイルして実行し、サンプルをそのまま描画。全音 clipped 0、peak ≤ 0.65、RoomTone のループ継ぎ目 0.0015（単発音の「seam」は立ち上がりの値で、クリックの指標ではない） |
| `session-log-autoescape.json` | 自動実行のセッションログ: outcome=Escaped、elapsed=12.66、enforcePlayLimit=false |

ビルドの Player.log: 例外 0 件、`LoopDemo: message panel sized (measured) bounds=(0.56, 0.16, 0.00) shrink=0.3017`。batchmode コンパイル エラー0・警告0。

## 途中で見つけて直したこと（計画担当が指示し、各実装担当が修正）

1. 案内パネルの計測で NullReferenceException（TextMesh には MeshFilter が無い）→ MeshRenderer.localBounds で計測。
2. 変更直後の部屋が暗すぎた → 環境光・照明・霧の調整、ACES → Neutral、露出 +0.4。
3. 案内パネルの文字が視野より大きい（幅 1.86m）→ 最長行 0.56m 以内に縮小。
4. TextMesh の文字が奥行きを無視して手前に出る（壁の文字が遮蔽やパネルを突き抜ける）→ URP 用の文字シェーダー `LoopRoom/Text`（ZTest LEqual、Single Pass Instanced 対応）。

## 未確認

- 実際の音の聞こえ方、HMD 内の見え方・フレームレート（Quest 3 / 3S で確認、Docs/DEVICE_QUICKCHECK.md）。
- desktop 表示ではカメラが時計に近く、時計の数字と壁の看板が大きく映る（既存の desktop カメラ位置。HMD では頭の位置になる）。
