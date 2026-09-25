## 指摘事項

1. **高 — B-2 の中断経路が仕様どおり限定されていない**  
   **場所:** `Assets/LoopRoom/Scripts/LoopDemo.cs:200`  
   **対象 SHA256:** `4554d309f5d7e2817e4bdfd4920eb9a888c754b942a4b3e68f7379dd3e207a96`

   `OnApplicationPause(bool paused)` がVRかどうかを判定せず `Model.Interrupt()` を呼びます。したがって、VRランタイムまたはOSからpause通知が届く環境では、フォーカス判定を `!rig.IsVR` で除外した `Update()` とは別経路でVRセッションが中断されます。「VR中の中断は Esc と追跡喪失のみ」というtask005の修正内容を満たしていません。

   **発生条件:** `Playing` または `Blackout` 中に、フォーカス切替・VRダッシュボード表示・HMD状態変化などに伴ってUnityが `OnApplicationPause(true)` を通知した場合。

   **修正案:** VR中はこのコールバックから中断しないよう `!rig.IsVR` を条件に加えるか、pauseによる中断自体を廃止し、VRでは既存の追跡喪失監視とEscだけに限定してください。

2. **中 — 長いフレームで t=3 と攻撃時刻をまとめて越えるとLatch音が欠落する**  
   **場所:** `Assets/LoopRoom/Scripts/LoopDemo.cs:100,113-116`  
   **対象 SHA256:** `4554d309f5d7e2817e4bdfd4920eb9a888c754b942a4b3e68f7379dd3e207a96`

   `Model.Advance()` の後、`Phase == Playing` の場合だけt=3の交差を検出しています。たとえば `lastLoopTime < 3` の状態から1フレームでt=6以上まで進むと、モデルは同じ `Advance()` 内で射撃を解決して `Blackout` に移行します。その後のLatch判定は実行されず、常時アクティブなAudioSourceへ移したにもかかわらず足音が一度も鳴りません。

   **発生条件:** 開始直後のロード停止やバックグラウンド処理などにより、1フレームの `Time.unscaledDeltaTime` がt=3と `firstShot` の両方を越える場合。

   **修正案:** `Advance` をt=3境界で分割してLatchを鳴らしてから残時間を進めるか、同一周回でt=3を通過した事実を最終Phaseとは独立して処理してください。

## 静的確認結果

- B-3の通常フレーム経路では、`Enemy audio` が常時有効な `Room` 直下に置かれ、敵のローカル位置へ追従しているため、旧来の「無効なEnemy上のAudioSource」問題は解消されています。
- B-1は `RoomVisuals.cs:121` の `allowXRRendering=false` により、URP観客カメラをXR描画から除外する実装になっています。対象SHA256は `2ba193680da0e7f63893a7c4414579ef6d70a527eaa4761963a037f840a291a3` です。
- 通常の周回遷移では `ClearSelection()` と `needsRelease` による押しっぱなし入力の拒否、および `LoopModel.Kill()` のPhase/LoopId検査による死亡処理の一意性を壊す変更は見つかりませんでした。
- 参照用 `LoopModel.cs`（`67599928845de6a3159fcd7ff2688f7ff783dbb5b0653b0d40e4f988e9cad55e`）と `DemoRig.cs`（`eb753e6b0685b92a771918bd7756086a486ea140ecfb3532dd164f666f421474`）に、今回の許可範囲から修正すべき追加事項はありません。

## 判定

**request_changes**

B-2の明示条件に反するVR用中断経路が残っています。加えて、B-3は長いフレームで音が欠落します。

## 未確認事項

テストやUnity実行は行っていません。以下は未確認です。

- Unity統合コンパイルとURP API互換性
- Desktop Playでのt=3再生およびAudioSource警告の有無
- Quest 3 PCVRでのウィンドウ切替時のpause/focus通知
- HMDへの観客カメラ混入防止とPC観客表示の維持
- 実機での周回境界、押しっぱなし入力、死亡処理一意性
- 長いフレームを注入した際のLatch再生

レビュー対象文書SHA256:

- `AGENTS.md`: `b085f58471f06f9669636925100228377c8d1e0d801f0bff80ec120ecf7e75d0`
- `CLAUDE.md`: `5e88123462781bc38ff4f6e797d6da67afe5d832a5abc9959b6c6ed4065e50f6`
- task005: `781fe2a73b90b3cbbaad18de7807ec7914f13a0c9d9fe6bb944e3629c65ac441`
