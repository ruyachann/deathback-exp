# task027 独立レビュー（GPT/Claude 混在禁止・独立判断）

対象スナップショット（`Collaboration/reviews/task027-diff.md` 記載のSHA256を引用）:
- `Assets/LoopRoom/Scripts/LoopDemo.cs` = `cda0552efd6d0ff62174c2acf57168ab07525aaa600f08c9fddf501e81ce3ec6`
- `Assets/LoopRoom/Scripts/CalibrationView.cs` = `556b7ab6d61a92011d0a3f6f99a447a36ffd778d56da8c1ca844401fb06b1618`
- `Assets/LoopRoom/Scripts/ProceduralAudio.cs` = `abfa232a09f14b50a85080b7399c2889a55290480ba92cdfdf7642069139be7e`

※ ハッシュ値自体を再計算するツールは使用していない（本文のみで判定する指示のため）。ハッシュとdiff内容の一致は未検証。

## 指摘事項

**（中）CalibrationView.cs 付近（`const float HoldRingHeight=.024f, ...` の直前コメント）**
コメントで「他の全ての高さ（BoundaryHeightを含む）より上」と主張しているが、`BoundaryHeight` や `MarginHeight` の実際の数値は本diffに含まれておらず（変更されていないため差分に現れない）、この主張の真偽をコードだけでは検証できない。追修正2の目的（扇形の上に重ならず表示する）が実際に達成されたかは、renderQueue+1という保険（belt-and-braces）はあるものの、**高さの数値比較自体が確認不能**。撮影での確認が必須。

**（中）LoopDemo.cs `CommitCalibration()` 内（`calibrationHoldProgress=0;` を含む行、旧行番号377付近）**
`calibrationHold>=1f` あるいは `autoCalibrateTimer>=4f` の分岐で一旦 `calibrationHoldProgress=1f` を代入した直後に `CommitCalibration()` を呼んでいるが、`CommitCalibration()` 内部で即座に `calibrationHoldProgress=0` にリセットしている。さらに `calibration.SetHoldProgress(...)` の呼び出しは `if(calibrationVisible)` ブロック内のみで、コミット直後は `calibrationVisible` が偽になり呼ばれない。結果として、リングが**満タン（t=1）の状態で実際に描画される瞬間は存在しない**（前フレームの途中状態のまま画面が閉じる）。設計意図（「満ちたら決定」の見た目）とわずかに乖離するが、受入条件2（伸びていく途中の画面）には抵触しない軽微な差異。

**（低）ProceduralAudio.cs `CalibrationConfirm()`（82行目以降）**
瞬時周波数 `freq` をそのまま `Sin(2π·freq·t)` に代入しており、周波数変化区間（`t<0.08`）で位相の連続性（本来は周波数の時間積分を位相に使うべき）が保証されていない。理論上わずかな不連続（クリック性ノイズ）が生じ得るが、既存の`ProceduralAudio`内の他関数と同様の簡易実装であれば許容範囲内。実際に耳障りかは音声確認が必要（未確認）。

**（低・確認要）LoopDemo.cs `rig.Haptic(.12f)` 呼び出し（377行目付近）**
`Haptic` のシグネチャ（引数が振動時間か強度か、単一引数で正しいか）が本diffだけでは分からない。ビルド0/0が保証されているなら問題ないはずだが、独立レビューとしてはAPI一致を目視確認できていない。

**（情報）タスク文書 `027-calibration-hold-feedback.md` の記述矛盾**
設計欄4「決定までの合計時間は従来どおり約2秒」と、追修正欄「1秒→3秒に延ばす（決定までは約4秒）」が同一ファイル内に併存し、数値が矛盾したまま残っている。実装（`autoCalibrateTimer>=4f`）は追修正の指示に従っており正しいが、**文書の更新漏れ**として指摘。

**（実装ロジックとして妥当と判断した点）**
- `holdDt = Mathf.Min(Time.unscaledDeltaTime, .1f)` は `calibrationHold` と `autoCalibrateTimer` の両方に適用されており、追修正2-2の要求（両方の計時にdt上限をかける）を満たしている。
- `SplashScreen.isFinished` を条件に追加したことで、スプラッシュ中は `autoCalibrateTimer` が進まない設計になっており、追修正の意図と一致する。
- `calibrationHold>=1f` と `else if (autoCalibrate ...)` が相互排他になっており、実際の長押しが自動計時より優先される点は妥当。
- Cキー即時決定・境界外警告(`calibrationOutside`)・`operatorCommandConsumed`のロジックには変更が加えられておらず、既存動作を壊していないとdiff上は判断できる（ただし全文非公開のため断定はできない）。
- 確認音は`Chime`とは別の`CalibrationConfirm()`として新規実装されており、コメントでも意図的に区別されていることが明記されている（`Chime`自体の実装は本snapshotに含まれず直接比較は不可）。

## 判定

**approve**

致命的なロジック欠陥・受入条件を明確に破る問題は本文からは発見できなかった。ただし上記「中」評価の2点（リングの高さ優先順位が数値で確認できない点、コミット直後にt=1が実際には描画されない点）は実機・撮影確認に強く依存するため、証拠画像でのリング視認性の裏付けを条件付けることを推奨する。

## 未確認事項

- batchmode 0/0・テスト全件PASSの実行結果（未実施）
- `--desktop --auto-calibrate --autostart` での実画面キャプチャ、リングが扇形/枠線に隠れず表示され伸びていく様子
- キャリブレーション画面が約3.6秒表示されてから決定に至ること（コード上の計算のみでは正確な検証不可）
- `BoundaryHeight`／`MarginHeight`など、diffに含まれない既存定数の実際の値とHoldRingHeightとの大小関係
- `rig.Haptic(float)` の実際のシグネチャと意味
- `ProceduralAudio.Chime()` の実装内容との聴感上の区別（このsnapshotには含まれず）
- 提示されたSHA256値と実ファイル内容の一致（ハッシュ再計算は未実施）
- 実機（Quest 3 PCVR）での長押しの見え方・音・振動の体感（タスク自体も未確認と明記）