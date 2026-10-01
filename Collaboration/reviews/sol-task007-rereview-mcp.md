## Findings

1. **重大 — 有効な設定でも `Advance()` が停止しない**

   - 場所: `Assets/LoopRoom/Scripts/LoopModel.cs:31-35, 127-159`
   - SHA256: `2aeda54c2b50052761f02cdb842b1c58ed17e0d0dd63af824502245d2a200de7`
   - 発生条件: `Validate()` は極端に小さい正数を許可する。例えば `firstShot=1e-200`、`searchShot=2e-200`、`exitOpens=1e-200`、`exitCloses=1.5e-200`、`blackout=1e-200` は検証を通る。この状態で `Advance(1)` を呼ぶと、`delta -= slice` が浮動小数点精度のため `delta` を変化させないまま死亡と周回を繰り返し、事実上終了しない。
   - 影響: `MaxStep=3600` は巨大な入力値を制限するだけで、`MaxStep` 以下の反復回数を一般には有界にしていない。今回の追修正の目的である停止性が、公開 API が許容する入力全体では満たされない。
   - 修正案: `firstShot`、`searchShot-firstShot`、`blackout` など反復を進める時間区間に、数値許容誤差と整合する実用的な最小値を設ける。併せて、各反復で `delta` または終了状態が確実に進む不変条件を実装する。極小だが検証を通るルールで `Advance(MaxStep)` が有限時間内に完了するテストも必要。

2. **中 — `MaxStep` 超過が `LoopDemo.Update()` の未処理例外になる**

   - 場所: `Assets/LoopRoom/Scripts/LoopDemo.cs:102`
   - SHA256: `9d6925b4e3dcd36edeb2d338f1f39bb9c479bebf0a2d2bfb4caa1380d0e39097`
   - 発生条件: スリープ復帰、デバッガ停止、極端なフレーム停止などで `Time.unscaledDeltaTime > 3600` になると、`Model.Advance()` の `ArgumentOutOfRangeException` が `Update()` から未処理で伝播する。
   - 影響: モデル状態は変更されないものの、「その1フレームを捨てるだけ」ではなく、Unity のエラーログと当該 `Update()` の途中終了を発生させる。実行環境によっては監視上の異常として扱われる。
   - 修正案: 許可ファイルを拡張し、`LoopDemo` で超過フレームを明示的にスキップして必要なら一度だけ警告する。あるいは、この未処理例外を許容することを受入仕様として明記する。

3. **軽微 — 「状態変更なし」と「長時間を全量処理」のテストが不完全**

   - 場所: `Assets/LoopRoom/Editor/LoopModelChecks.cs:37-54`
   - SHA256: `0b9479f224ed6f7f847fcd81885de6d6112aa5a8a0bbc9aea38de991912d55ac`
   - 発生条件:
     - 既定ルールの1000秒チェックは `TotalTime == 1000` を確認せず、途中で時間を捨てても通り得る。
     - 超過拒否後は `LoopId`、`TotalTime`、レコード数しか比較せず、`Phase`、`Outcome`、`LoopTime`、`BlackoutRemaining`、盾・射撃状態の変化を検出できない。
     - 既定タイミングだけで `MaxStep` を試しており、Finding 1 の極小タイミングを検出しない。
   - 修正案: `TotalTime` の全量消費を確認し、拒否前後の全公開状態を比較する。許容範囲下限付近のカスタムルールも追加する。

## 静的に確認できた点

- `enforcePlayLimit=false` では3か所の時間上限処理が無効になっており、通常設定では `TimedOut` に遷移しない。
- `enforcePlayLimit=true` では172秒の `TimedOut` と8秒後の `Finished` が維持されている。
- 通常のフレーム時間は `MaxStep` に到達しない。
- 死亡、Blackout、旧周回入力拒否、脱出、中断、エンディングの主要な分岐に、今回の変更による別の具体的退行は見つからなかった。

## Verdict

**request_changes**

`MaxStep` 以下でも、検証を通るルールによって `Advance()` が停止しないため、追修正の停止性要件を満たしていません。

## 未検証事項

指示どおり、テストやコマンドは実行していません。以下は未確認です。

- `dotnet` モデルテスト15件
- Unity batchmode コンパイル
- Editor Playでの180秒超過
- Unityでのスリープ／長時間停止復帰時の例外挙動
- Quest 3 PCVR実機での周回、追跡喪失、脱出、中断
- 性能および3600秒入力の実測完了時間

## 対象スナップショット

- `AGENTS.md`: `be53f607d20e3503473a167b094d52e1ac454328b09b0c7e64a3df382ae1ebd5`
- `CLAUDE.md`: `5e88123462781bc38ff4f6e797d6da67afe5d832a5abc9959b6c6ed4065e50f6`
- `Collaboration/tasks/007-remove-session-time-limit.md`: `754be22e350677385dc5e049b56efbe9e36abb26cd438429c059c168004b6bca`
- `LoopModel.cs`: `2aeda54c2b50052761f02cdb842b1c58ed17e0d0dd63af824502245d2a200de7`
- `LoopModelChecks.cs`: `0b9479f224ed6f7f847fcd81885de6d6112aa5a8a0bbc9aea38de991912d55ac`
- `Program.cs`: `012fda101ccd073f63acd99a28b954275ef3c938e6a3e85010237c7c89a45f47`
- `LoopDemo.cs`: `9d6925b4e3dcd36edeb2d338f1f39bb9c479bebf0a2d2bfb4caa1380d0e39097`
