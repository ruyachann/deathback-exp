## 独立判定

`request_changes`

### Findings

1. **高 — 無効化した位置合わせの数値が残り、安全範囲計算に再利用される**

   - 場所: `Assets/LoopRoom/Scripts/LoopDemo.cs:156,161,243-244`
   - SHA256: `f06bf6dae7f469118a6b349dc2e54834b80f22c0244858a5cde39bbd327975cd`
   - トリガー:
     1. Cで非ゼロ位置・向きを保存する。
     2. Desktop/VRを切り替える、またはRでVR再準備する。
     3. Cを押し直さず開始する。
   - 問題: `aligned=false` にするだけで、`alignCx`、`alignCz`、`alignAreaYaw`を初期値へ戻していません。`PlaceRoom()`も`aligned`を参照せず、古い値を常に`ChooseFrontYaw()`へ渡します。表示上は「未」でも、別モードまたは再準備前の位置合わせが実際には残ります。追修正2の「再位置合わせが必要」と、未実施時は`c=(0,0), areaYaw=0`という設計に反します。安全範囲を誤認する可能性もあります。
   - 修正: 位置合わせ無効化を一つのメソッドにまとめ、フラグと3値を同時に初期化するか、`PlaceRoom()`で`aligned == false`なら必ずゼロ値を渡してください。
   - 関連タスクSHA256: `0317c1cb272bcc26627952974af463ff3431e1e733afa5e1fe3698c242c4a3cc`

2. **中 — EnterとCの同時入力では、開始後にもCが受理される**

   - 場所: `Assets/LoopRoom/Scripts/LoopDemo.cs:151,160-168`
   - SHA256: `f06bf6dae7f469118a6b349dc2e54834b80f22c0244858a5cde39bbd327975cd`
   - トリガー: Ready/FinishedでEnterとCを同じフレームに押す。
   - 問題: `idle`を`Begin()`前に一度だけ計算しています。`Begin()`が開始処理と最初の`PlaceRoom()`を終えた後も、古い`idle=true`によってC処理が実行されます。その結果、第1周は以前の位置合わせで配置され、その直後にPlaying中の姿勢が新しい位置合わせとして保存され、後続周回だけに適用されます。「Cはidle && rig.CanStartのときだけ」に厳密にはなっていません。
   - 修正: C処理を開始判定より前に行うか、C処理時に現在の`Model.Phase`を再評価してください。

### 確認できた範囲

`DemoRig.cs`（SHA256 `a111a68c8cdb10d969cff70aada005ca691ae2297d4e015c5a62d4cbea5882d5`）のドリフト列と、`RoomAnchor.cs`（SHA256 `510674914e8105600e0c800c826b2f2a9828d6cdc8e90207675c2bb5aee998e0`）の探索順からは、小さいずれが無補正、大きいずれが概ね`-135°`、`+165°`補正になる構成を静的に確認しました。`fits=false`警告も、Cによる再位置合わせまで一度に制限されています。

### 未確認事項

- 指示どおり、コンパイル、テスト、Unity実行は行っていません。
- batchmodeのエラー・警告0、モデルテスト全件PASS、Player.log例外0。
- `--desktop --autostart --simulate-drift`の実画面とログ。
- Quest 3でのFloor追跡、暗転中の配置、到達範囲、音源方向。
- `RoomVisuals.cs`がスナップショットに含まれていないため、ラグ、Collider不在、音源の親子関係の全体確認。
- `LoopModel`が未提供のため、同時入力時を含む実際のPhase遷移。

その他の供給文書は、`AGENTS.md` SHA256 `17a8b55855402be023072b21e66a2378c980ade41e9b08062e7ec498cd36df84`、`CLAUDE.md` SHA256 `fa38f7c0c30475591ff99b65e2e95c9b39538915fb6bd703a3f3b900c68d2a82`として確認しました。
