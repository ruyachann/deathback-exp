# task005 実装報告（2026-09-19, Claude Fable 5.1）

- 実装者: Claude Fable 5.1（ユーザー直接依頼）。独立レビューではない。
- 対象タスク: Collaboration/tasks/005-stage2-first-fixes.md
- 他モデル呼出し: なし。PAUSE.json は Codex 枠の停止記録として据え置き。

## 変更ファイルと SHA256

| ファイル | 変更前 | 変更後 |
| --- | --- | --- |
| Assets/LoopRoom/Scripts/LoopDemo.cs | 449b19bd9694afa8cb5eab145a70b46dadbab41a362404900923e893f172d39f | 4554d309f5d7e2817e4bdfd4920eb9a888c754b942a4b3e68f7379dd3e207a96 |
| Assets/LoopRoom/Scripts/RoomVisuals.cs | a9cb72a4e78a2b83f76a62b15434b93f433cab975bac29b6a475a655153d332f | 2ba193680da0e7f63893a7c4414579ef6d70a527eaa4761963a037f840a291a3 |

## 変更内容

### B-2 LoopDemo.cs（Update）
`if (!Application.isFocused && !Application.isEditor && !idle) Model.Interrupt();` に `!rig.IsVR &&` を前置。desktop 確認モードのみフォーカス喪失で中断する。VR 中の中断経路は Esc、追跡喪失 0.3 秒、OnApplicationPause のまま。

### B-3 LoopDemo.cs（Start / RefreshWorld）
- `enemyAudio` を `room.Enemy` 上の AddComponent から、`room.Root` 直下の新規 GameObject `Enemy audio`（`playOnAwake=false`、spatialBlend 等は従来値）へ変更。
- `RefreshWorld` の Enemy 位置更新直後に `enemyAudio.transform.localPosition = room.Enemy.localPosition + (0,1.4,0)` を追加。Enemy が非アクティブでも localPosition は毎フレーム更新されるため、足音（t=3, 扉付近 (.8,1.4,2.5)）と射撃（t>=12, 回り込み位置）の定位は従来と同じ経路をたどる。
- 周回切替時の `enemyAudio.Stop()`、Latch/Shot の `PlayOneShot` 呼出しは無変更。

### B-1 RoomVisuals.cs（BuildSpectator）
- `using UnityEngine.Rendering.Universal;` を追加。
- `Spectator.GetUniversalAdditionalCameraData().allowXRRendering = false;` を追加。API は Library/PackageCache/com.unity.render-pipelines.universal@40f8b5b9939e/Runtime/UniversalAdditionalCameraData.cs の `CameraExtensions.GetUniversalAdditionalCameraData`（182行, 無ければ AddComponent）と `allowXRRendering` プロパティ（879行）で確認。
- `stereoTargetEye=None` は Built-in fallback 用に残した。

## 実行した検証

| 検証 | 結果 |
| --- | --- |
| `python -m unittest discover -s Tools -p test_automation.py` | 26 PASS（変更対象外の回帰確認） |
| Unity 6000.3.15f1 batchmode コンパイル（`-batchmode -nographics -quit`、Editor 未起動を Temp/UnityLockfile 不在で確認） | Tundra build success、Assembly-CSharp.dll 再生成、`error CS` 0 件、`warning CS` 0 件、終了コード 0（ログはローカルのみ、push しない） |
| Editor Play / Latch 警告確認 | 未実施 |
| Quest 3 実機（B-1 描画、B-2 フォーカス） | 未実施。Quest 3 は接続不可（STATE.md） |
| .NET モデル 12 チェック | 未実施（この PC に SDK なし。LoopModel は無変更） |

## 残る未確認事項

- B-1: `allowXRRendering=false` で PC ウィンドウ側の観客表示が従来どおり出るか（XR ミラー表示との関係）は実機で確認。
- B-3: 足音の定位が「扉の方向」として自然か。Enemy 初期位置 (.8,0,2.5) は扉 (.8,1.18,2.86) の手前 0.36m。
- 相互レビュー: Sol/Sonnet の独立レビューは未実施。

## 次の担当

ユーザー判断で (a) Sol/Sonnet レビューへ同 SHA を渡す、または (b) Fable 実装を受け入れて Editor Play 確認へ進む。
