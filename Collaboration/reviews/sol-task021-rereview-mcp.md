## Findings

1. **高 — 境界への接触を緑と誤判定し得る**  
   `Assets/LoopRoom/Scripts/CalibrationView.cs:209-219`  
   SHA256: `c1edb18c98e2c2bde5308fa8f097ff94d66caeb566e9f9fdb73cf81ddcb02ce5`

   `SegmentsIntersect` が接触・共線判定に `d1 == 0` などの完全一致を使っています。回転した正方形やXR境界の浮動小数点座標では、幾何学上は接触していても外積が微小な非ゼロ値となり、交差なし＝`Fits`（緑）になる可能性があります。これは「境界上は Outside」とする仕様（タスク39行目）に反します。

   修正: `Mathf.Abs(d) <= epsilon` を使った符号判定と接触判定に置き換え、許容誤差内をすべて交差扱いにしてください。可能なら計算を `double` に寄せます。

2. **中 — R入力が同一フレームの開始判定を消費できていない**  
   `Assets/LoopRoom/Scripts/LoopDemo.cs:227-228`  
   SHA256: `613fc54d3e786299ecd69f09040980348e6e1d9710a4323905a6d7d149b3ace5`

   開始判定がR判定より先にあります。`idle && CanStart && CanRetryPreparation` が同時に成立する状態でRとEnter・A/X・`autoTrigger` が重なると、227行目で`Begin()`が実行され、R処理には到達しません。追修正3-4の「CまたはRを処理したフレームでは開始判定をしない」を構造上保証できていません。

   修正: C/Rを開始判定より先に排他的に処理し、その時点で`operatorCommandConsumed = true`にした後、未消費の場合だけ開始判定してください。

3. **中 — 「境界外で決定」の表示が次の決定より前に消える**  
   `Assets/LoopRoom/Scripts/LoopDemo.cs:299,312`  
   SHA256: `613fc54d3e786299ecd69f09040980348e6e1d9710a4323905a6d7d149b3ace5`

   Outsideで決定後、Rによる再準備またはdesktop/VR切替で`ResetAlignment()`が呼ばれると、`calibrationOutside=false`に戻ります。次のキャリブレーションをまだ決定していなくても警告が消えるため、「次の決定まで残す」というタスク38行目の仕様を満たしません。

   修正: `ResetAlignment()`では`calibrationOutside`を消去せず、`CommitCalibration()`で新しい判定結果を確定したときだけ更新してください。

## Verdict

**request_changes**

追修正3-3の`calibrating && idle`による部屋非表示、およびC/長押し/自動決定時の同一フレーム開始抑止は静的には実装されています。しかし、上記3点、特に境界の安全側判定とR入力順序が受入仕様を満たしていません。

## 未確認事項

指示どおりビルド・テスト・Unity実行は行っていません。次は未確認です。

- batchmodeのコンパイル0/0、全テストPASS
- `--desktop --auto-calibrate --autostart`で2秒表示後に開始まで進むこと
- Player.logの例外0件と境界外決定時の警告
- DemoRig未提示部分の60°下向き、A/X長押し、境界点のワールド座標変換
- PrivateLayerが観客カメラから除外されること
- Quest 3/Air Linkでの枠表示、境界比較、長押し決定

レビュー対象SHA256:

- `AGENTS.md`: `17a8b55855402be023072b21e66a2378c980ade41e9b08062e7ec498cd36df84`
- `CLAUDE.md`: `fa38f7c0c30475591ff99b65e2e95c9b39538915fb6bd703a3f3b900c68d2a82`
- `Collaboration/tasks/021-calibration-mode.md`: `2f9d9c9a1eadbfb90769924874ed10eb404e25703e3b804f1eeb2966bb53a3c5`
- `LoopDemo.cs`: `613fc54d3e786299ecd69f09040980348e6e1d9710a4323905a6d7d149b3ace5`
- `CalibrationView.cs`: `c1edb18c98e2c2bde5308fa8f097ff94d66caeb566e9f9fdb73cf81ddcb02ce5`
