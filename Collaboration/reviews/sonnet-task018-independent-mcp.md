# task018 独立レビュー（部屋の置き直しの組み込み）

対象タスク: `Collaboration/tasks/018-room-anchor-integration.md`（SHA256: `259bee1e29f924335e04b56f282bd9d91021519f0429a2f3312da2677ea53532`）

参照ソース（SHA256）:
- `Assets/LoopRoom/Scripts/LoopDemo.cs`: `af6673b236aafc176437863c107f4174c10dd4fcf94d5d3b90acc41e8cf13ac6`
- `Assets/LoopRoom/Scripts/DemoRig.cs`: `a111a68c8cdb10d969cff70aada005ca691ae2297d4e015c5a62d4cbea5882d5`
- `Assets/LoopRoom/Scripts/RoomVisuals.cs`: `2845b4095389c827612e685c15585ec0ced9e06f60dbe62c24818df10ccc5880`
- `Assets/LoopRoom/Scripts/RoomAnchor.cs`: `3e7ad288c8f5ca83b53311cc012e961ad86471e02b4cae368ee8457dcf769bb1`（許可ファイル外・参照専用のはず）

## 所見

### [Medium] Begin() 実行時、部屋の再配置が暗転していない状態で起きうる
- 場所: `LoopDemo.cs` の `Begin()`（`rig.ClearSelection(); trackingLost=0; PlaceRoom(); ...`）
- 再現条件: Ready 状態（`Model.Phase==SessionPhase.Ready`）では `room.Blackout` は非アクティブ（`RefreshWorld` の `room.Blackout.SetActive(Model.Phase==SessionPhase.Blackout...)` 参照）。この状態でプレイヤーが Enter/A ボタンを押して `Begin()` が呼ばれると、`PlaceRoom()` による `room.Root` の瞬間移動・回転がそのままプレイヤーの視界で起きる。設計節3「暗転中であること」は「周回が変わったフレーム」についてのみ明記されており、初回 `Begin()` 時の扱いが企画・タスクのどこにも明記されていない。
- 影響: 初回開始時に部屋が視界内で瞬間的にジャンプして見える可能性があり、「毎回定位置・正面から始まる」という体験意図に反し得る。ただし旧実装からの踏襲挙動の可能性があり、新規に埋め込まれたバグとは断定できない。
- 修正案: 初回 `Begin()` 前にも一瞬の暗転（`room.Blackout` を先に立てる、または Ready 中は常時 Blackout 相当の演出にする）を挟むか、企画側で「初回は視界内での配置を許容する」と明記する。

### [Low] `SimulateDesktopDrift` は `pitch` を保持したまま `yaw` のみ固定列で上書きする
- 場所: `DemoRig.cs` `SimulateDesktopDrift(int loopId)`（`driftOffset = DriftOffsets[i]; yaw = DriftYaws[i]; ... View.transform.localRotation = Quaternion.Euler(pitch,yaw,0);`）
- 再現条件: `--desktop --simulate-drift` 実行中にオペレーターが右ドラッグで視点操作（`PollMode()` 内の pitch 変更）をした後に周回が切り替わると、証拠用の固定列に含まれない `pitch` が残留し、周回間で「同じ構図」にならない。
- 影響: `--autostart` の自動実行（マウス操作なし）では発生しないため受入条件2の証拠取得自体には影響しない想定だが、手動デモや今後の検証時に構図がずれる可能性がある。
- 修正案: `SimulateDesktopDrift` 内で `pitch = 0;` も固定するか、コメントで「pitch は対象外」と明記する。

### [Low] `RoomAnchor.cs` が許可ファイル外だが、変更が無いことをこのレビューでは検証不能
- 場所: `RoomAnchor.cs` 全体
- タスクの許可ファイルは `LoopDemo.cs` / `RoomVisuals.cs`（ラグのみ）/ `DemoRig.cs`（desktop専用最小変更のみ）で、`RoomAnchor.cs` は「参照用（task017 の API）」とされている。単一スナップショットしか提供されておらず、task017 完了時点の版との差分比較ができないため、「参照専用のまま不変か」は確認できない。
- 修正案（レビュー手続き上）: task017 完了コミットとの `git diff` を提示してもらい、無変更であることを確認する。

### [Info] C キーと Enter/R の排他は要求されていないが、同一フレーム同時押し時に両方実行され得る
- 場所: `LoopDemo.cs` `Update()` の Enter/R 分岐（`if (... ) { Begin(); } else if (... rKey ...) rig.RetryPreparation();`）と、独立した `if (idle && ... cKey ...)` ブロック
- 再現条件: 同一フレームで Enter と C を両方 `wasPressedThisFrame` にする（極めて稀）。
- 影響: `Begin()` と位置合わせが同フレームで両方走っても状態不整合は生じない（位置合わせは `alignCx/alignCz/alignAreaYaw` の保存のみで `PlaceRoom` の入力には使われるが、次の呼び出しから反映されるだけ）ため実害はほぼ無い。タスクの「守ること」は Enter/R の排他のみ要求しており、C との排他は要求されていないため、仕様違反ではない。

### [Info] `RoomAnchor.ChooseFrontYaw` の探索粒度（15°刻み）
- 場所: `RoomAnchor.cs` `ChooseFrontYaw`
- 15° 刻みの探索のため、実際にはフィットする角度が存在してもスキップされ `fits=false` になる偽陰性の余地がある。ただし `RoomAnchor.cs` は task017 の担当範囲であり本レビュー対象外の参照コードなので、指摘のみに留める。

## 追修正要件（交互ドリフト列・ログ）の検証
- `DemoRig.cs` の `DriftOffsets`/`DriftYaws` は追修正コメントの例（(+0.15,+0.10)・20° → (+0.55,+0.35)・40°(補正あり) → (−0.20,−0.05)・−30° → (−0.60,−0.40)・−120°(補正あり)）と一致。`RoomAnchor` の定数（`SafeHalfSize=0.8`, `Margin=0.10` → 有効半径0.7、`Reach=0.45`）で手計算した限り、1・3番目は前方扇形が0.7m以内に収まり `fits=true`（補正なし）、2・4番目は扇形の外周点が0.7mを超え `fits=false`（補正あり）となり、意図通り交互パターンになることを確認した（手計算による簡易検証、実行検証ではない）。
- `PlaceRoom()` のログ出力は「位置・頭yaw・選んだ正面yaw・補正有無（diff付き）・fits」を1行で出しており、追修正の要求項目を満たしている。

## 個別要件の確認結果
- 位置合わせ（C キー、Ready/Finished限定）: 実装あり、仕様通り。
- `room.Sound` のローカル座標化: `localPosition = new Vector3(0,1.4f,.7f)` に修正済み、`room.Root` に追従する。
- ラグ追加（`RoomVisuals.cs`「Reach rug」）: `Detail()` 使用でコライダーなし、既定レイヤー、落ち着いた色。位置・サイズ（前方0〜0.6m、幅1.0m）はタスク例と整合。
- `--simulate-drift` の VR・通常起動への非干渉: `desktopArg` 判定と `SimulateDesktopDrift` 内 `if (IsVR) return;` の二重ガードで担保されている。
- Enter/R の排他、`--autostart` の条件（`desktopArg && ...`）: `DemoRig.cs` の元コメント通り維持されており、劣化は見当たらない。

## 判定

**request_changes**（軽微〜中程度の指摘のみで致命的欠陥はないが、Begin() 時の可視動作について企画側の意図確認が必要、かつ受入条件1・2・4が本レビューでは未確認のため、無条件の approve は避ける）

## 未確認事項（本レビューでは検証不可能）
- 受入条件1: batchmode コンパイルのエラー0・警告0、モデルテスト全件 PASS（実行結果未提供）。
- 受入条件2: `--desktop --autostart --simulate-drift` の証拠スクリーンショット・contact-sheet・Player.log の例外0（画像・ログ未提供）。
- 受入条件4: 実機での実験（未実施と明記されている通り）。
- `LoopModel.LoopId` の初期値・遷移仕様（`LoopModel.cs` 本体が未提供のため、`(loopId-1)%DriftOffsets.Length` のインデックス計算が実際の LoopId=1 始まりと整合するかは推測に留まる）。
- `RoomAnchor.cs` が task017 完了時点から無変更であることの差分確認。