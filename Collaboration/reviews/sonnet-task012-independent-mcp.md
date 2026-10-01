# task012 独立レビュー（Claude Sonnet 5）

対象SHA256（指定どおり引用）:
- `Assets/LoopRoom/Scripts/DemoRig.cs`: `5f29a20f751ed95b6ac0e47b7fcbacc123191c9eea3ee2a9341042380edba250`
- `Assets/LoopRoom/Scripts/LoopDemo.cs`: `fccd609817de6128f12fe4f4a7dc5936aa19074175650609ef9fa80a6eb9ffb0`
- `Collaboration/tasks/012-xr-retry-preparation.md`: `61e729170467eaaa9518d902b5d408629df463683bd83d8310461f0ba555f74b`

## 指摘

### 1. 【重大】Interrupted 状態から R 再準備ができない可能性
- **File/Line**: `LoopDemo.cs:82`（`bool idle=Model.Phase==SessionPhase.Ready || Model.Phase==SessionPhase.Finished;`）、`LoopDemo.cs:87`（R キー処理）、`LoopDemo.cs:99`（`Model.Interrupt()`）
- **トリガー**: VR再生中にトラッキングロスト（HMD外れ／Link切断）が0.3秒超続くと `Model.Interrupt()` が呼ばれる（`LoopDemo.cs:99`）。`RefreshWorld()` 内で `SessionPhase.Interrupted` は `Ready`/`Finished` と別枝で扱われており（メッセージ分岐が独立）、`Phase==Interrupted` の間は `idle` が `false` のままになる可能性が高い。この場合、`rig.CanRetryPreparation` が `true` になっていても `LoopDemo.cs:87` の `idle &&` 条件でガードされ、R キーが無視される。
- **なぜ問題か**: task012 の目的は「展示中に Link 接続が一時的に切れたら再起動せずに再準備できるようにする」こと。しかし実際に Link が切れる典型経路（Playing/Blackout 中のトラッキングロスト→`Interrupt()`）を辿ると、まさにその再準備が効かない可能性がある。
- **未確認**: `LoopModel.cs`（`SessionPhase` の定義、`Interrupted` から `Finished` への遷移有無）が本レビューのスナップショットに含まれていないため、`Interrupted` が永続的な独立フェーズか、次フレームで自動的に `Finished` に遷移するかを断定できない。もし独立フェーズのまま留まる設計なら、`idle` の定義（またはR判定の条件）に `SessionPhase.Interrupted` を含めるべき。
- **修正案**: `LoopModel.cs` を確認し、`Interrupted` が終端状態として保持されるなら、R再準備の許可条件に `Model.Phase==SessionPhase.Interrupted` を加える（例: `bool canRetryKey = idle || Model.Phase==SessionPhase.Interrupted;`）。

### 2. 【中】PreparationMessage に R 案内が出ないケースがある
- **File/Line**: `DemoRig.cs:27-29`
```
public string PreparationMessage => xrInitializing ? "..." :
    FloorReady ? "第零室\n頭の位置と向きの追跡を待っています。" :
    "...\nR: 再準備（運営）";
```
- **トリガー**: `FloorReady==true` かつ `HeadTracked==false`（あるいは `RuntimePresent()==false`）の場合、`CanStart` は `false` になり `CanRetryPreparation`（`DemoRig.cs:26`）は `!xrInitializing && !forceDesktop && (desktopFallback || !CanStart)` により `true` になり得る。しかしこの状態では `PreparationMessage` は「頭の位置と向きの追跡を待っています。」のみを表示し、R案内が出ない。
- **なぜ問題か**: HMD内に表示されるユーザー/運営向けメッセージと実際に有効な操作（R再準備）が一致しない。運営が desktop の OnGUI 表示（`LoopDemo.cs` の `showRetryHint`）を常時見ているとは限らない。
- **修正案**: `FloorReady` 分岐のメッセージにも `CanRetryPreparation` が true の場合は "R: 再準備（運営）" を追記する、または分岐条件を `CanStart` ベースに統一する。

### 3. 【軽微】RetryPreparation は外部所有ローダーの場合に再試行の実効性がない
- **File/Line**: `DemoRig.cs:303-312`、`StartXR()` 内 `DemoRig.cs:232-241`
- **トリガー**: 初回起動時に `ownsXR=false`（既に外部が XR ローダーを起動していた）場合、`RetryPreparation()` は `StopOwnedXR()`（`DemoRig.cs:294-301`）が何もせず、その後 `StartXR()` 再実行時も `activeLoader!=null` のため何も再初期化されず、単に Floor 再取得ループへ進むだけになる。Link切断が外部ローダー自体の停止を伴う場合、`activeLoader` が非nullのまま無効化されている可能性があり、再試行しても同じ理由で再度ブロックされる。
- **なぜ問題か**: タスクの主要シナリオ（Link再接続後にRで復帰）が、外部所有ローダーの環境では機能しない懸念がある。ただし、この設計（外部所有ローダーを起動/破棄しない）自体は元のコードの既存方針であり、task012はそれを踏襲している。
- **修正案（任意）**: 挙動として許容するなら、少なくとも `RetryPreparation` が無効化された理由をログに残すなど診断性を上げる。必須の修正ではないため severity は低め。

## 良好点（欠陥ではないが確認できた点）
- `CanRetryPreparation`（`DemoRig.cs:26`）は task012 の**修正後**設計 `!xrInitializing && !forceDesktop && (desktopFallback || !CanStart)` と一致している。
- `RetryPreparation()`（`DemoRig.cs:303-312`）は `xrInitializing=true` をコルーチン起動前に設定しており、多重起動は `CanRetryPreparation` のガードで防止されている。
- `OnDestroy`（`DemoRig.cs:314-318`）の実質的な挙動は `StopOwnedXR()` 抽出前後で変わっていない。
- `origin.enabled=false` の追加は Initialize と RetryPreparation の両方に対称的に入っている。
- `LoopDemo.cs:87` で Playing/Blackout 中の R キーは `idle` 条件により無視される設計は、タスク記載どおり実装されている（指摘1のケースを除く）。

## Verdict: **request_changes**

理由: 指摘1（Interrupted状態でのR再準備可否）は、`LoopModel.cs` が本レビュー対象に含まれておらず断定できないものの、もし `Interrupted` が独立した終端フェーズとして残るなら、本タスクが解決すべき最も典型的な障害シナリオ（Link切断→再準備）そのものが機能しない致命的欠陥になる。この点を `LoopModel.cs` と突き合わせて確認し、必要なら修正するまでは承認できない。指摘2は軽微だが合わせて修正を推奨する。

## 未確認事項
- `LoopModel.cs`（`SessionPhase` 定義、`Interrupt()` の遷移先）未提供のため指摘1は推測に基づく。
- batchmode コンパイル（エラー0・警告0）は本レビューでは未実施（コード読解のみ）。
- Editor Play（desktop fallback → R → 再準備）の実地確認は未実施。
- 実機（Quest3 Link 切断→再接続→R）は未実施（task012 側でも「計画6で確認・未確認」と明記済み）。
- Unity XR Management の `DeinitializeLoader()` 後に `activeLoader` が確実に `null` へ戻るかは、パッケージバージョン依存であり未検証。