# 007 — セッション時間上限（180秒）を既定で無効にする

状態: 計画確定（2026-09-23）。計画は Claude Opus 5.5（`claude-opus-5-5`）。実装は GPT-5.6 Sol（`gpt-5.6-sol`、`codex exec` の別セッション）。レビューは実装とは別セッションの Sol と Claude Sonnet 5。

## 目的

2026-09-23 のユーザー決定「180秒の制限は今はなくし、まずしっかり遊べるように持っていきたい」をコードに反映する。脱出するまで周回を続けられ、終了はプレイヤーの脱出か運営の中断（Esc / 追跡喪失）だけにする。再導入はユーザー判断なので、上限の仕組みは残してフラグで無効にする。

## 設計

- `LoopRules` に `public bool enforcePlayLimit = false;` を追加する。
  - シーン `Assets/LoopRoom/Scenes/LoopRoom.unity` には `playLimit: 172` / `endingLength: 8` が保存されているが、新フィールドはシーンに無いため、Unity はフィールド初期値 `false` を使う。**シーンは編集しない。**
- `LoopRules.Validate()`
  - NaN / 無限大の検査は7項目すべてで維持する。`playLimit > 0`、`endingLength > 0` も維持する。
  - `playLimit + endingLength > 180.0` の検査は `enforcePlayLimit == true` のときだけ行う。
- `LoopModel.Advance()`
  - `enforcePlayLimit == false` のとき、`untilLimit` を `double.PositiveInfinity` として扱い、`TimedOut` へは遷移しない（3か所: 131-132、137、144行付近）。
  - `enforcePlayLimit == true` のときは現在と同じ挙動（172秒で TimedOut、8秒のエンディングの後 Finished）。
  - 周回、死亡、Blackout、脱出、中断、エンディング（`endingLength`）の挙動は変えない。
- `SessionPhase.TimedOut`、LoopDemo の TimedOut 表示文言は残す（フラグ有効時に使う）。

### 追修正（2026-09-23、独立レビュー指摘への対応）

Sol（別セッション）の独立レビュー指摘（中）: 上限を外したことで、巨大な有限 delta（例: `double.MaxValue`）を渡すと `delta -= slice` が浮動小数点精度で変化せず、`Advance()` が終わらない。これまでは playLimit がループ回数を抑えていた。Sonnet は approve。

計画担当の決定: `LoopModel.MaxStep = 3600.0` を追加し、`Advance()` 冒頭の入力検査で `delta > MaxStep` を状態変更前に拒否する（既存の NaN/無限大/負値の拒否と同じ扱い）。LoopDemo からの通常のフレーム時間では到達しない。スリープ復帰などで到達した場合も、その1フレームの時間が捨てられるだけで、モデルの状態は変わらない。拒否を確認するチェックを1件追加する。

## 許可ファイル

- `Assets/LoopRoom/Scripts/LoopModel.cs`
- `Assets/LoopRoom/Editor/LoopModelChecks.cs`
- `Tests/LoopModel.Tests/Program.cs`

LoopDemo.cs、RoomVisuals.cs、DemoRig.cs、シーン、設定、依存は変更しない。

## テストの更新

- 既存の上限テスト（LoopModelChecks「Deadline includes blackouts and ends within 180 seconds」、Program.cs の 172/180 秒確認）は `new LoopRules{ enforcePlayLimit = true }` で実行し、現在の期待値のまま通す。
- 追加するチェック:
  1. 既定ルールでは無操作で長時間（例: 1000秒）進めても `TimedOut` にならず、`Playing` または `Blackout` のまま周回が続く（LoopId が増える）。
  2. 既定ルールでは `playLimit + endingLength > 180` でも Validate を通る。`enforcePlayLimit=true` では従来どおり拒否する。
  3. 既定ルールでも Interrupt と脱出で終了し、`endingLength` 後に Finished になる（既存チェックで足りれば新規は不要）。
- Program.cs の `existing.Count == 11` と「PASS: 12 model checks」は、追加したチェック数に合わせて更新する。

## 受入条件

1. `dotnet` によるモデルテスト（Tests/LoopModel.Tests）が全件 PASS。
2. Unity batchmode コンパイルでエラー0・警告0。
3. 実装とは別セッションの Sol と Sonnet が同じ SHA256 で独立レビューし、交換後に両方 approve。
4. Editor Play（desktop）で 180秒を超えてもセッションが続くこと（任意。実施した場合のみ記録）。

## リスク

- 上限が無いと、脱出できないプレイヤーは運営が Esc で止めるまで続く。展示運用時の打ち切り手順は段階5で決める。
- 1フレームの `Time.unscaledDeltaTime` が `MaxStep`（3600秒）を超えると、`Advance` が例外を出し、LoopDemo.Update のそのフレームの残りが飛ぶ（例外ログが1回出る。モデルの状態は変わらず、次フレームから継続する）。1時間を超える停止からの復帰でのみ起きるので、プロトタイプでは許容する（2026-09-23 照合）。
- `LoopRules.Validate()` は極小のタイミング値（例 1e-200）を受け入れ、その場合 Advance が止まらない。task007 以前からの既存課題として Issue4 で扱う。
- セッションログ（SessionLog.timings）に `enforcePlayLimit` が加わる。過去ログとの互換性に問題はない（JsonUtility は欠けたフィールドを既定値にする）。
