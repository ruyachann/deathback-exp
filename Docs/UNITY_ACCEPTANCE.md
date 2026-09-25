# Unity/実機 受入手順書（第零室デモ）

本書は Quest 3 / Quest 3S PCVR 版デモ（現行設計、Docs/Plans/STAGE_PLAN.md 段階2相当）に対する**手動受入テストの手順書**である。実機が Quest 3S になる可能性があるため、機種が確定するまで両方を想定する（2026-09-24 追記）。

**重要**: 本書はテストの実施方法・期待される証拠・不合格判定基準を定めるものであり、実施結果や合格を示すものではない。各項目は実施後に日付・実施者・対象コミットSHA・結果（実施済み/未実施/合格/不合格）を別途記録すること。未実施の項目を実施済み・合格として扱ってはならない。

## 0. 前提条件

- Unity 6000.3.15f1、対象は Meta Quest 3 / Quest 3S（PCVR、Quest Link もしくは Air Link、Touch Plus コントローラー）。機種が確定次第、本書と AGENTS.md の不変条件・STAGE_PLAN の対象機種を更新する（2026-09-24 追記）。
- プレイ領域は 2m 四方。時間上限は当面なし（`LoopRules.enforcePlayLimit=false`）。再導入はユーザー判断（2026-09-23 決定、2026-09-24 本書へ反映）。
- `Assets/LoopRoom/Editor/DemoSetup.cs` の Editor メニューを使用する。実在するメニューは以下のみ：
  - `LoopRoom/1 - Prepare project and scene`
  - `LoopRoom/2 - Configure Quest Link OpenXR`
  - `LoopRoom/3 - Validate settings and model`
  - `LoopRoom/4 - Build Windows demo`
  - `LoopRoom/Run model checks only`
- 受入前に未保存のユーザー編集を保存・保護し、保存確認をキャンセルした場合は準備を中止する。`LoopRoom/1` は既存の `Assets/LoopRoom/Scenes/LoopRoom.unity` を開き、このシーンが存在しない場合だけ作成する。続いて `LoopRoom/2` → `LoopRoom/3` を実行し、`LoopRoom/3` がコンソールで「model checks passed」「XR configuration checked」を報告し、かつ例外を投げないことを確認する。`LoopRoom/3` は OpenXR ローダー設定とモデルチェックのみを検証し、**実機（HMD）での挙動は検証しない**（DemoSetup.cs 内のログ文言のとおり）。
- Windows 実行ファイルで確認する場合は `LoopRoom/4 - Build Windows demo` でビルドしたものを使用する。

## 1. 準備ゲート（readiness gates）の確認

**手順**
1. ビルドまたは Editor Play を起動し、ヘッドセット未接続または Floor 未確定の状態で第零室メッセージパネル（HMD専用表示）を確認する。
2. `DemoRig.CanStart` が false の間（`xrInitializing` 中、または `FloorReady`/`HeadTracked`/`RuntimePresent()` のいずれかが未達）、Enter キーおよびコントローラーの primaryButton（A/X）を押下しても Model.Phase が Ready から変化しないことを確認する。
3. 準備状況に応じて表示文言が以下のいずれかに切り替わることを確認する。
   - `xrInitializing=true` の準備中は、Floor が確定していても「第零室\nVRを準備しています。\n接続とプレイエリアを確認してください。」を表示する。
   - 初期化待ちが終了し、`FloorReady=true` の場合は「第零室\n頭の位置と向きの追跡を待っています。」を表示する。この文言の分岐は頭部追跡や表示ランタイムの有無を直接区別しないため、原因の判断には `HeadTracked` と `RuntimePresent()` も確認する。準備待ちのタイムアウト後も Floor が確定したままならこの文言となり、頭部追跡と表示ランタイムがともに復帰すれば開始ゲートが成立する。
   - 初期化待ちが終了し、Floor が未確定の場合は「第零室\nVRの準備ができませんでした。\n接続とプレイエリアを確認してください。」を表示する。Floor 準備失敗の自動再試行は現行実装にないため、接続確認後に Play またはアプリを再起動して再準備する。

**期待される証拠**: 各状態でのメッセージ文言のスクリーンショットまたは記録、CanStart=false 時に開始操作が無効であったことの記録。

**不合格基準**: CanStart が false であるにもかかわらずセッションが開始する（Model.Phase が Ready から変化する）、または状態に対応する文言が表示されない。

## 2. Floor / 頭部 / 手の追跡確認

**手順**
1. Quest 3 または Quest 3S を装着し、Link/Air Link 接続後にビルドを起動する。
2. Floor 基準（`TrackingOriginModeFlags.Floor`）が確定し、`FloorReady` が true になった後、実際の床面と仮想床面の高さが一致していることを確認する（しゃがむ・立つ動作でずれがないか）。
3. 頭を左右上下に動かし、視点の位置・回転（`HeadTracked`）が実際の動きと一致することを確認する。
4. 両手のコントローラーを動かし、コントローラープロキシ（球体、PrivateLayer 表示）が実際の手の位置・向きに追従することを確認する。
5.（2026-09-24 追記）Quest 3S で実施する場合は、視野角・解像度・レンズの違いによる机・時計・取っ手の視認性、および案内パネル（第零室メッセージ、操作説明）の文字が判読できることを確認する。desktop モードでは案内パネルの文字が Game ビューからはみ出す見た目が確認されている（`Collaboration/evidence/20260924-editor-play`）。VR では見え方が異なる可能性があるため、HMD 内での見え方として別途確認する。

**期待される証拠**: 床面ずれなし、頭部回転・位置追従の目視確認記録、両手のプロキシが実位置と一致する記録（可能であれば映像）。Quest 3S 使用時は視認性・文字サイズの確認結果（使用機種を記録）。

**不合格基準**: 床面に有意なオフセットがある、頭部の回転が反映されない、手のプロキシが表示されない・大きくずれる。Quest 3S で机・時計・取っ手が視認できない、または案内パネルの文字が判読できない。

## 3. デスクトップ `--desktop` 経路の確認

**手順**
1. ビルドを `--desktop` 引数付きで起動する（`DemoRig.Initialize()` が `Environment.GetCommandLineArgs()` から `--desktop` を検出し `forceDesktop=true` とする）。
2. ヘッドセットが接続されていても `IsVR` が false のまま維持され、XR 初期化コルーチンが開始されないことを確認する（`xrInitializing` が発火しない）。
3. `CanStart` が `forceDesktop || desktopFallback` により true になり、メッセージが「第零室  /  操作確認\nEnter：開始　Space：遮蔽　E：出口\n右ドラッグ：見回す」であることを確認する。
4. キーボード/マウス操作（Enter=開始、Space=遮蔽、E=出口、右ドラッグ=視点）が機能することを確認する。

**期待される証拠**: 起動ログまたは画面表示で desktop 経路のメッセージ文言が出ていること、各キー操作に対応する反応（遮蔽の上下、出口ランプの状態変化等）の記録。

**不合格基準**: `--desktop` 指定にもかかわらず XR 初期化が走る、または対応するキー操作が機能しない。

## 4. Play の 3 回開始/停止（Unity Editor）

**手順**
1. `LoopRoom/1 - Prepare project and scene` でシーンを準備済みの状態から、Editor で Play を開始する。
2. Ready → 開始 → セッション進行（脱出または中断のいずれか。タイムアウトは既定では発生しない。第8章の参考手順で `enforcePlayLimit=true` にした場合のみ発生する）→ 終了までを 1 サイクルとし、Play を停止する。
3. 同一 Editor セッションを再起動せずに、これを連続 3 回繰り返す。

**期待される証拠**: 3 回とも Console にエラー・例外が出ないこと、`XR Origin`/`Interaction Manager` などの生成物が Stop 時に正しく破棄され、次の Play で重複や警告が出ないこと。

**不合格基準**: いずれかの回で例外・エラーが出る、オブジェクトが破棄されず残る、XR ローダーの停止/解放（`DemoRig.OnDestroy` の `StopSubsystems`/`DeinitializeLoader`）が失敗し次回 Play に影響する。

## 5. 3 回の死亡ループと XR 原点非再割当の確認

**手順**
1. セッションを開始し、遮蔽を上げずに攻撃を受けるなどして実際の死亡を、アプリを再起動せずに連続 3 回発生させる。各回で Playing → Blackout → Playing と `Model.LoopId` の増加を確認する。タイムアウト（既定では発生しない。第8章の参考手順で `enforcePlayLimit=true` にした場合のみ発生する）はセッション終了であり、死亡によるループ再開として数えない。
2. `LoopId` 変化のたびに `rig.ClearSelection()` とチャイム音（`room.Chime`）が発生することを確認する（`LoopDemo.Update` の該当分岐）。
3. **最重要確認**: `room.Root` の位置・回転は `Begin()`（`Model.Start()` 呼び出し前）で一度だけ、その時点のプレイヤー実位置・向きに基づき設定される。ループ 2 回目・3 回目でユーザーが物理的に移動・再キャリブレーションを求められないこと、また `room.Root` や Floor 原点が変化しないことを確認する。

**期待される証拠**: 3 回とも室内の仮想位置が物理空間に対して不変であることの記録（目視または位置ログ）、ユーザーが再配置を求められなかった旨の記録。

**不合格基準**: いずれかのループでルームや原点が物理空間に対してずれる・再キャリブレーションが要求される・ユーザーが物理的に移動を強いられる。

## 6. 追跡中断（tracking interruption）の確認

**手順**
1. VR モードで Playing または Blackout フェーズ中に、ヘッドセットを外す・Link 接続を切断するなどして `HeadTracked` または `RuntimePresent()` を偽にする。
2. 0.3 秒未満の瞬断では `rig.ClearSelection()` により選択入力が解除され、モデル時間の進行と新しい操作の処理が停止し、`trackingLost>0` により Blackout 表示が有効になることを確認する。追跡が復帰すれば同じセッションが再開し、瞬断だけでは Interrupted に遷移しない。
3. 0.3 秒（`trackingLost>.3f`）を超えて継続した場合、`Model.Interrupt()` が呼ばれ、`trackingLost` が 0 に戻り、メッセージが「体験を中断しました。\n接続と周囲を確認してください。」に変わることを確認する。この時点では Interrupted フェーズかつ `trackingLost=0` のため、持続する Blackout 表示を期待しない。
4. 中断後にセッションログ（`outcome=Interrupted`）が保存されることを確認する。

**期待される証拠**: 瞬断時に入力（グリップ操作）が無効化される記録、0.3 秒超の遮断で Interrupted 状態に遷移する記録。

**不合格基準**: 追跡喪失中も操作が受理される、0.3 秒超の遮断でも中断しない、または瞬断（0.3 秒未満）で誤って中断される。

## 7. 観客画面のネタバレマスキング確認

`RoomVisuals.BuildSpectator()` は非ステレオの観客カメラを生成し、`Spectator.cullingMask=~(1<<PrivateLayer)` により PlayerOnly（layer 8）を除外する。`UpdatePublic()` は VR モードで観客カメラと公開プロキシを有効にし、頭部位置はそのまま、両手位置は各軸を約 1/3m 単位へ丸めて表示する。このソース上の構成と、実際の PC 観客表示に漏洩がないことを別々に確認する。

**手順**
1. レイヤー構成（layer 8 = PlayerOnly、layer 9 = SpectatorOnly、`DemoSetup.Prepare()` で設定）を確認する。HMD カメラ（`View.cullingMask = ~(1 << 9)`）は SpectatorOnly を描画しない設定になっている。
2. 準備/終了メッセージパネルおよびコントローラープロキシは `RoomVisuals.PrivateLayer`（PlayerOnly）に配置されている。観客用画面（PC画面/観客カメラ出力）にこれらが表示されないことを目視確認する。
3. プレイ中、遮蔽・出口の操作手順や攻略上のヒントを示すテキスト・ハイライトが観客画面に露出しないことを確認する。

**期待される証拠**: 観客画面のスクリーンショットで PlayerOnly レイヤーのオブジェクト（メッセージパネル、コントローラープロキシ等）が写っていないこと。

**不合格基準**: 観客画面に準備/終了メッセージ、コントローラープロキシ、その他 PlayerOnly 専用オブジェクトが表示される、または攻略手順を示す情報が見える。

**追加確認**: VR の観客表示では F2 の運営表示を無効にしておく。F2 を有効にすると `LoopDemo.OnGUI()` が攻略キーと経過時間を PC 画面に描くため、観客へ見せない。公開の手プロキシ、音、影、その他の表示から攻略操作が推測できないかも実機で確認する。ソース確認だけで観客表示の合格とは扱わない。

## 8. セッション時間の記録（2026-09-24: 180 秒の合否基準を撤廃し改題。2026-09-23 のユーザー決定を反映）

時間上限は当面なし（`LoopRules.enforcePlayLimit=false`）。合否基準は時間の長さではなく、セッションが脱出（Escaped）または運営の中断（Interrupted、Esc/追跡喪失）のいずれかで終わり、`TimedOut` が出ないこととする。

**手順**
1. 装着と準備が完了した後、開始操作により最初の `Model.Start()` が実行される時点（最初の開始チャイムを目印にできる）を計測起点とする。Ready の準備待ち時間は含めない。
2. `Model.Phase` が Finished に遷移するまでの実時間を外部ストップウォッチで計測する。Escaped/Interrupted への遷移後の 8 秒の終了演出も含め、これらの結果フェーズへの遷移時点で計測を止めない。
3. 開始・Finished 到達時刻と合計秒数、`outcome`（Escaped または Interrupted）を記録する。`Model.TotalTime` は参考値に留め、追跡瞬断によるモデル停止などを含む実時間の代わりにしない。F2 の運営表示を使う場合は観客に見せない。
4. セッションログ（`Application.persistentDataPath/Sessions/<sessionId>.json`）の `elapsed` と `outcome` を記録し、`outcome=TimedOut` が出ていないことを確認する。あわせて `timings.enforcePlayLimit` が `false` であることを確認する。シーンに `playLimit=172` が保存されていても、`enforcePlayLimit=false` の間は使用されない。

**期待される証拠**: 開始・終了時刻、合計秒数、`outcome`、セッションログの `elapsed` と `timings.enforcePlayLimit` の記録。

**不合格基準**: `outcome` が `TimedOut` になる、Escaped/Interrupted 以外で終わる、`timings.enforcePlayLimit` が `true` になっている、または計測不能（クラッシュ・フリーズ等）。

**参考: 時間上限を再導入した場合の確認手順**（`LoopRules.enforcePlayLimit=true` に設定して確認する場合のみ使用する。既定では実施しない）

1. `enforcePlayLimit=true` の設定でビルドまたは Editor Play を実行する。
2. 172 秒経過時点で `Model.Phase` が `TimedOut` に遷移し、以後 8 秒のエンディング演出を経て `Finished` になることを確認する。
3. 合計時間が 180 秒（172+8）を超えないことを確認する。

**不合格基準（参考手順のみ）**: `enforcePlayLimit=true` で合計時間が 180 秒を超える、または `TimedOut` に遷移しない。

## 9. 記録・保存に関する注意

- セッションログは `Application.persistentDataPath/Sessions/<sessionId>.json` に保存される（`sessionId`, `mode`, `outcome`, `elapsed`, `timings`（`enforcePlayLimit` を含む）, `events` を含む）。受入記録に添付する場合は必要な範囲のみを抜粋する。
- 個人情報や接続環境固有の情報を含みうる生ログ・詳細診断ログは受入記録に含めない。
- 各項目について「実施日」「実施者」「対象コミットSHA」「結果（実施済み/未実施、合格/不合格）」「証拠の保存場所」を別紙・別セクションに記録すること。本書自体には結果を書き込まない。
