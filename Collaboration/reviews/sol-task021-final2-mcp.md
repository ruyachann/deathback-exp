## 指摘事項

1. **中 — 境界点が3個未満でも「境界情報なし」が運営表示に出ない**  
   場所: `Assets/LoopRoom/Scripts/LoopDemo.cs:313, 379, 428`、`CalibrationView.cs:158-160`  
   対象SHA256: `66bbc36959c120c5413e1001056ae3062543ce75b7421eafe1ea74cbb07dd8e0` / `43da0c7f41853f277eee6fde8a965acf2ff85ce5668047b54bddb39518dee9b1`  
   トリガー: `TryGetBoundaryPoints` が `true` を返す一方、取得点が0～2個の場合。  
   `CalibrationView.Refresh` は正しく `Unknown`（白）へ正規化しますが、`lastBoundaryAvailable` は `true` のままです。そのため `showBoundaryHint` が偽となり、必須の「境界情報なし（目視で確認）」が表示されません。  
   修正案: 両取得箇所で、戻り値と点数をまとめて正規化します。例:  
   `lastBoundaryAvailable = rig.TryGetBoundaryPoints(boundaryPoints) && boundaryPoints.Count >= 3;`

2. **低 — 案内表示が目線の「少し下」ではなく上に配置される**  
   場所: `Assets/LoopRoom/Scripts/LoopDemo.cs:161, 164`  
   対象SHA256: `66bbc36959c120c5413e1001056ae3062543ce75b7421eafe1ea74cbb07dd8e0`  
   トリガー: キャリブレーション案内の表示時。  
   カメラ子のローカルYが `+0.02f` なので、パネルと文字は目線より上です。タスク指定は「目線の少し下」です。  
   修正案: パネルと文字のローカルYを負値へ変更し、desktopの60°下向き表示とHMDの両方で視認位置を確認してください。

追修正5の線分判定自体は、点―線分距離を `double` で計算し、`<= 1e-4` を接触扱いとしており、長さ0の辺も点として処理できています。追修正1～4についても、提示範囲から新たな致命的欠陥は確認できませんでした。

## 判定

**request_changes**

境界点3個未満という追修正5の明示ケースで、白表示は成立しても必須の運営向け警告が欠落します。

## 未確認事項

- batchmodeの終了コード、テスト全件PASS、Player.logの例外数は未実行・未確認です。
- `DemoRig`、`RoomVisuals`、`PlayAreaSettings`、`RoomAnchor`、`LoopModel`が未提示のため、境界点の座標変換、入力状態、カメラのレイヤー除外、設定値検証、再開始動作は未確認です。
- `--desktop --auto-calibrate --autostart` の画面証拠、Quest 3/Air Linkでの枠・境界・長押し決定は未確認です。
- 他のレビュー報告は参照していません。

確認対象として提示されたSHA256:

- `AGENTS.md`: `17a8b55855402be023072b21e66a2378c980ade41e9b08062e7ec498cd36df84`
- `CLAUDE.md`: `fa38f7c0c30475591ff99b65e2e95c9b39538915fb6bd703a3f3b900c68d2a82`
- `021-calibration-mode.md`: `97386da25769aa928770e4124fca2a6b764a3ace2c3a563090f3231fc21b6bfb`
- `CalibrationView.cs`: `43da0c7f41853f277eee6fde8a965acf2ff85ce5668047b54bddb39518dee9b1`
- `LoopDemo.cs`: `66bbc36959c120c5413e1001056ae3062543ce75b7421eafe1ea74cbb07dd8e0`
