# 再レビュー結果（task005 R-1/R-2 反映後）

対象SHA256（提示値をそのまま引用）
- `Assets/LoopRoom/Scripts/LoopDemo.cs`: `9d6925b4e3dcd36edeb2d338f1f39bb9c479bebf0a2d2bfb4caa1380d0e39097`
- `Assets/LoopRoom/Scripts/LoopModel.cs`（参照用）: `67599928845de6a3159fcd7ff2688f7ff783dbb5b0653b0d40e4f988e9cad55e`
- `Assets/LoopRoom/Scripts/DemoRig.cs`（参照用）: `eb753e6b0685b92a771918bd7756086a486ea140ecfb3532dd164f666f421474`
- `Assets/LoopRoom/Scripts/RoomVisuals.cs`（参照用/B-1確認）: `2ba193680da0e7f63893a7c4414579ef6d70a527eaa4761963a037f840a291a3`
- タスク定義: `7a05867dbbeb25a324737b2d7ffc4de41430bf145dd3967de24e79ac4e5c06d1`

## B-1/B-2/B-3の確認

- B-1: `RoomVisuals.BuildSpectator` に `Spectator.GetUniversalAdditionalCameraData().allowXRRendering=false;` あり。記載どおり。
- B-2: `LoopDemo.Update` の `if (!rig.IsVR && !Application.isFocused && !Application.isEditor && !idle) Model.Interrupt();` に `!rig.IsVR` が入っている。記載どおり。
- B-3: `enemyAudio` を `room.Root` 直下の常時アクティブなオブジェクトとして生成し、`RefreshWorld` で `room.Enemy.localPosition+new Vector3(0,1.4f,0)` に追従させている。記載どおり。

## R-1（`OnApplicationPause`）の確認

```
void OnApplicationPause(bool paused) { if(paused && Model!=null && !rig.IsVR) Model.Interrupt(); }
```

- `!rig.IsVR` が追加されており、VR中は `OnApplicationPause` が中断を起こさない。
- VR中の中断経路を洗い出すと、Escキー (`keyboard.escapeKey.wasPressedThisFrame`) と追跡喪失0.3秒 (`trackingLost>.3f`) の2つのみで、フォーカス喪失分岐（`!rig.IsVR && ...`）と `OnApplicationPause` はいずれも `rig.IsVR` 時は評価対象外。タスク記載どおり「Esc と追跡喪失のみ」になっている。欠陥なし。

## R-2（Latch判定拡大）の確認

```
if((Model.Phase==SessionPhase.Playing || Model.Phase==SessionPhase.Blackout) && lastLoopTime<3 && Model.LoopTime>=3)
    enemyAudio.PlayOneShot(room.Latch);
```

`LoopModel.Advance` を確認すると、`Kill` 発生時 `Phase=Blackout` になった後 `BlackoutRemaining` のみが減算され、`LoopTime` はfirstShot/searchShot到達時の値で凍結される（`LoopTime` を更新する分岐は `Blackout` では通らない）。したがって：

- 1フレームでt=3とfirstShotを同時に跨いだ場合でも、そのフレームの `Model.Phase` は `Blackout`、`Model.LoopTime>=3` のままなので条件が成立し、Latchは鳴る。修正前（`Phase==Playing` 限定）で欠落していたケースは解消されている。
- 発火順序は「Latch判定 → Recordsループでの `first_shot`/`flanked` 検出時 `room.Shot` 再生」の順にコードが並んでおり、`PlayOneShot` は呼び出し順に再生開始されるため、Shotより先にLatchが鳴る順序は維持されている。
- 二重再生防止: `lastLoopTime<3` を満たすのは1回だけで、発火後に `lastLoopTime=Model.LoopTime`（>=3）へ更新されるため、Blackoutが複数フレーム継続しても再発火しない。
- 周回切替時: `Model.LoopId!=lastLoop` の分岐が `Advance` 直後・Latch判定より前にあり、`lastLoopTime=0` にリセットされる。新周回の `LoopTime` は `BeginLoop` で0開始のため、同フレームで誤って旧周回のLatch判定を再利用することはない。極端に長いフレームで1回のUpdateに複数周回のKillが発生しても、最終的な `Model.LoopId/LoopTime` に対して1回だけ判定される（中間周回のLatch/Shotが省略される可能性はあるが、これはR-2の対象範囲外の既知の制限）。

## 不変条件の確認

- 周回リセット: `BeginLoop`（`LoopModel.cs`）は変更されておらず、`LoopId!=lastLoop` 検出時に `rig.ClearSelection()` と音声停止も行われる。問題なし。
- 死亡処理一意性: `LoopModel.Kill` は `Phase!=Playing` なら `false` を返す既存ロジックのままで、`LoopDemo.cs` の変更はここに影響していない。
- 旧周回入力拒否: `RaiseShield`/`TryExit` は引き続き `Model.LoopId` を渡しており、`LoopModel` 側の `expectedLoop != LoopId` チェックは変更なし。

## 指摘事項

| severity | 箇所 | 内容 |
| --- | --- | --- |
| 低 | `LoopDemo.cs` `RefreshWorld`: `room.Enemy.gameObject.SetActive(t>=3 && (playing || Model.Phase==SessionPhase.Blackout));` | Enemyの表示条件が `Playing` に加え `Blackout` でもアクティブになるよう拡大されているが、task005本文にはLatch判定拡大のみが明記されており、この表示条件変更は記載されていない。Latch再生時の視覚整合性目的と推測されるが、タスク記述との不一致として記録しておく（機能上の欠陥ではない）。 |
| 情報 | `LoopDemo.cs` `Update` 内Latch再生 vs `RefreshWorld` | `enemyAudio.transform.localPosition` の更新は `RefreshWorld` （Latch再生より後）で行われるため、Latch再生時の音源位置は1フレーム前の座標になる。実害は数十ms相当の定位ずれのみで軽微。 |

## 未確認事項（未実施）

- Unity統合コンパイル（batchmode等）は実行していない。
- Editor Play / 実機（Quest 3 PCVR）での動作確認は実行していない。B-1〜B-3・R-1・R-2の実挙動（HMD内俯瞰混入の有無、Latch可聴性、Esc/追跡喪失以外でVRセッションが切れないこと）はソースコード解析のみに基づく。

## 判定

**approve**

R-1/R-2ともタスク記載どおりに実装されており、`LoopModel.cs` の状態遷移（Blackout中のLoopTime凍結）と整合した論理になっている。二重再生・誤再生・不変条件違反となる具体的な欠陥は確認できなかった。低severityのドキュメント不一致（Enemy active条件の拡大）のみ記録し、ブロッカーとはしない。