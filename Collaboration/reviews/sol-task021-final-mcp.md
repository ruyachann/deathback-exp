## Findings

- **重大度: High**
  - **場所:** `Assets/LoopRoom/Scripts/CalibrationView.cs:212-235`（`Sign`、`SegmentsIntersect`、`OnSegment`）
  - **SHA256:** `39fa9f8161fa0afb9d028625755659f5e72d6dda2362a7206e905f031f9e783a`
  - **問題:** `Epsilon = 1e-4` を交差積の絶対値に直接適用しています。交差積の単位は m² であり、境界線の長さに比例するため、「1e-4 m以内を接触扱い」という追修正4の条件を満たしません。
  - **トリガー:** 例えば長さ4mの境界辺と正方形辺が平行で、距離が `0.00005m` の場合、交差積は約 `0.0002` です。距離はepsilon内ですが `Sign` は非ゼロとなり、辺同士を非接触と判定して緑（`Fits`）になり得ます。
  - **修正案:** 点と直線の距離として判定するか、符号判定の許容値を `Epsilon * segmentLength` として正規化してください。退化辺も考慮し、各端点間・点と線分間の距離が `<= 1e-4m` なら必ず交差／接触扱いにするのが明確です。計算は最後まで `double` で行ってください。

## Verdict

**request_changes**

追修正4の中心要件である「境界からepsilon以内はすべてOutside」が保証されず、安全表示が誤って緑になる経路が残っています。

一方、以下は supplied source 上では要求どおりです。

- C/Rは開始判定より前に処理され、`operatorCommandConsumed` で同一フレームの開始を抑止。
- `calibrationOutside` は `ResetAlignment()` で消去されず、次の `CommitCalibration()` で更新。
- 境界判定には四隅の内包だけでなく辺同士の交差検査も追加済み。

## 未確認事項

実行・ツール利用は行っていないため、以下は未確認です。

- Unity batchmode の終了コード `0/0`、全テストPASS、コンパイル。
- Quest 3/Air Linkでの境界取得、表示、A/X長押し。
- `--desktop --auto-calibrate --autostart` の画面とPlayer.log。
- 未提示の `DemoRig.TryGetBoundaryPoints`、`RoomAnchor`、`PlayAreaSettings`との統合。
- 実機におけるPrivateLayer、枠線、床、案内表示の見え方。

## 確認したスナップショット

- `AGENTS.md`: `17a8b55855402be023072b21e66a2378c980ade41e9b08062e7ec498cd36df84`
- `CLAUDE.md`: `fa38f7c0c30475591ff99b65e2e95c9b39538915fb6bd703a3f3b900c68d2a82`
- `Collaboration/tasks/021-calibration-mode.md`: `1dedeb2e04dcc9178b6b3906dd8bade76692a8d322bbd3fb67afd4dce22e5455`
- `Assets/LoopRoom/Scripts/LoopDemo.cs`: `66bbc36959c120c5413e1001056ae3062543ce75b7421eafe1ea74cbb07dd8e0`
- `Assets/LoopRoom/Scripts/CalibrationView.cs`: `39fa9f8161fa0afb9d028625755659f5e72d6dda2362a7206e905f031f9e783a`
