# 企画・計画・コード レビュー（2026-09-18）

- レビュー実施: Claude Fable 5.1（ユーザーがこのセッションで直接依頼）。AGENTS.md/CLAUDE.md が定める「claude-sonnet-5 による独立レビュー」ではない。相互レビューの片側として数えるかはユーザーの判断に委ねる。
- 前提: `Collaboration/PAUSE.json` が存在するため、新しいモデル呼出し・実装は行っていない。本書はファイル読解と、ローカルで実行可能な検証のみに基づく。
- 修正・実装は本書では行わない（ユーザー指示: 後で実施）。以下は指摘と修正案。

## 対象ファイルと SHA256

| ファイル | SHA256 |
| --- | --- |
| Assets/LoopRoom/Scripts/LoopModel.cs | 67599928845de6a3159fcd7ff2688f7ff783dbb5b0653b0d40e4f988e9cad55e |
| Assets/LoopRoom/Scripts/LoopDemo.cs | 449b19bd9694afa8cb5eab145a70b46dadbab41a362404900923e893f172d39f |
| Assets/LoopRoom/Scripts/DemoRig.cs | eb753e6b0685b92a771918bd7756086a486ea140ecfb3532dd164f666f421474 |
| Assets/LoopRoom/Scripts/RoomVisuals.cs | a9cb72a4e78a2b83f76a62b15434b93f433cab975bac29b6a475a655153d332f |
| Assets/LoopRoom/Editor/DemoSetup.cs | b8a42cb4698bc3bea7773832ed2bd9be1b52fdce3f0eeb222410109ec04873fb |
| Assets/LoopRoom/Editor/LoopModelChecks.cs | 3585f347546a97a92d2ffa74d526893f2ae7dca82624a522c1378ea25db80e61 |
| Tests/LoopModel.Tests/Program.cs | 307156c9648bb9d6bbb6287bfd8ff7d849a769a40fab467cb759851b68045aba |
| Tools/automation.py | cf17becf0541f980f1386a81baa8e45713924b105aafc1711592cb9a092e8b33 |
| Tools/sonnet_packet.py | c3778a799753a5df89153203c7a98d14fed61f48a4fa9c89cec075af94941406 |
| Tools/windows_job.py | 5041aae843c9aa2eac2063f511128cb58f56804c5d8336cbdd302dbda4ed5021 |
| Tools/check_dependencies.py | 64def4e1e6c98e90087e4e1f1d30ed8d4ceb83319509020f8a9ddb81d3f10e98 |
| Packages/manifest.json | 0f336bf41ff7dd0305ae2d0c459a37eac24b872d8cfd02115ceb6322ecb26c3a |
| Docs/Plans/VR_demo_plan_v0.1.md | e3c220bfa3406bfcae66aa397be893f65ba5f21e6d955cca0c0373e4c1d3334e |
| Docs/Plans/STAGE_PLAN.md | ffbb6fe4d6707ebd5fca818d06d9bb0591f13cbe4d6a0476a0aebdbf4abd9c2a |
| Docs/Plans/VR_demo_first_milestone.md | 4a113f7860925310634e422c6572aa8d51d7cef94466e66f4a533421a7103f57 |
| Docs/Plans/VR_demo_implementation_status.md | 63484765db983af58c4339553dc02047e2cc07223ea057522b4ddeb6c1dfbf0b |
| Docs/UNITY_ACCEPTANCE.md | 5553a388f8776ebd4bc8e6800502c1c400c586e818a165eeb321b32e7776b70d |
| AGENTS.md | 31c8309a77928da369e0e03d882419c631f2fec96a3e2a9e65d18d9c93e39d4a |

## 実行した検証

| 検証 | 結果 |
| --- | --- |
| `python -m unittest discover -s Tools -p test_automation.py` | 26件 PASS（Windows, Python 3.12） |
| `dotnet run` によるモデル12チェック | 未実行。このPCに .NET SDK なし。CI合格記録（STAGE01_RESULTS.md）は別途存在 |
| Unity 統合コンパイル / Play / Quest 3 | 未実行（PAUSE中、Editor起動せず） |
| ProjectSettings と DemoSetup.Prepare の整合 | `activeInputHandler: 1`、layer 8/9 = PlayerOnly/SpectatorOnly、runInBackground=1、fullscreenMode=3 を確認 |
| シーンのスクリプト参照 | LoopRoom.unity の m_Script guid が LoopDemo.cs.meta と一致 |
| XR設定アセット | InitManagerOnStart=0、AutomaticLoading/Running=0 を確認。Oculus Touch profile の有効状態はアセット上で断定できず（Validate合格記録に依拠） |

---

## Part A: 企画・計画への所見

### A-1（高）実時間180秒の定義が計画とコードで一致していない

- 場所: STAGE_PLAN.md 段階2「実時間<=180秒の実機記録」、VR_demo_plan_v0.1.md §5、LoopModel.cs
- 現状: モデルは `playLimit + endingLength <= 180` をモデル時間で保証する。追跡瞬断中はモデル時間が止まり（LoopDemo.Update の早期 return）、実時間は伸びる。task003 で「壁時計3分上限」は明示的に非対象とした。
- 影響: 展示の回転率要件（1人3分）が守れない可能性を仕様として認めていることになる。
- 修正案: STAGE_PLAN 段階2または3に「壁時計上限」を Issue として明記し、方針を1つ決める。案A: 壁時計 180 秒でモデルの残り時間を打ち切る（Interrupted とは別の TimedOut 扱い）。案B: 実時間超過は運営判断で中断とし、要件は「モデル時間180秒」に緩める。どちらでも良いが未決のままにしない。

### A-2（高）観客表示の設計が「全部隠す」で止まっており、段階4・5の検証ができない

- 場所: VR_demo_plan_v0.1.md §10、RoomVisuals.cs Build/BuildSpectator
- 現状: 敵・扉・遮蔽板・出口・ランプ・暗転はすべて PrivateLayer で観客カメラから除外される。観客が見えるのは床・壁・机・頭と量子化した手の球のみ。
- 影響: 計画が求める「死に戻りの存在・テンポ・上達は見せる」が成立せず、段階4の「観戦後の別場面で体験を検証」が評価不能。
- 修正案: 段階2のうちに「表示物 → HMD表現 / 観客表現」の対応表を ARCHITECTURE.md か新規 Docs に置く。例: 敵=武器なしシルエット、扉=表示、遮蔽板=無ラベルの板、ランプ・ラベル・カード=非表示、暗転=観客側は画面全体の短い減光。実装は段階5でよいが、表は今決める。

### A-3（中）身体寸法の調整（計画§6）が実装にもタスクにも無い

- 現状: 取っ手は 1.01 m 固定、頭から前方 0.36 m 固定。計画は「開始前に身長・腕の長さに合わせ調整し、開始後は固定」と書く。
- 修正案: 段階2の開始ゲートに「両方の取っ手に一度触れてから A/X で開始」を追加する。到達確認と操作説明を兼ね、寸法調整なしでも到達不能を事前に検出できる。調整そのものは段階3へ。

### A-4（中）運営の復旧手順が段階3の一行にしか無い

- 現状: XR準備が10秒以内に成立しないと、アプリ再起動以外の復帰手段が無い（UNITY_ACCEPTANCE.md §1 も再起動を案内）。Link接続前にアプリを起動する運用では毎回起きうる。
- 修正案: 段階2の完了条件に「アプリ再起動なしで準備をやり直せる（運営キー）」を追加。実装は DemoRig に「準備再実行」を1つ足すだけで済む（Part B-8 参照）。

### A-5（中）計画文書の陳腐化

- VR_demo_implementation_status.md: `LoopRoomUnity/Collaboration`、`CLAUDE_REVIEW_REQUEST.md`（存在しない）、「設計 Astra・実装 Sonnet」など2026-09-17以前の分担を記述。
- VR_demo_first_milestone.md: 冒頭の状態文「Package Manager の IPC 接続問題で未完了」は解消済み（UPM_RECOVERY.md）。
- 修正案: 両ファイル冒頭に「履歴文書。現状は Collaboration/STATE.md と Docs/Plans/STAGE_PLAN.md」の一行を追加し、本文は変更しない（証跡維持）。README の一覧にも「履歴」と明記。

### A-6（中）シナリオ時刻がコードに分散しており、段階4（S02以降）の前提が崩れる

- 現状: LoopRules は7値だが、足音3秒・扉の開き3〜3.65秒・敵の回り込み8〜11.5秒は LoopDemo.RefreshWorld/Update にハードコード。firstShot を変えると音・演出と致死判定がずれる。
- 修正案: Issue4（可変Rules）の範囲に「演出時刻を Rules または ScenarioDefinition へ移す」を含める。段階2では変更不要。

### A-7（中）manifest に計画に無い実験パッケージがある

- `com.unity.pipeline: 0.7.0-exp.1` が直接依存として宣言されている。どの計画・タスクにも根拠が無く、古い test-framework 1.1.33 と newtonsoft に依存する。
- 修正案: Issue3 で必要性を確認し、不要なら Package Manager（Client API）経由で削除。手編集はしない。

### A-8（低）「同時刻の優先規則」が文書化されていない

- 計画§14は「判定時刻と同時刻の優先規則を固定する」を要求。コードは Update 内の順序（Advance → Operate/TryExit）で「同一フレームでは致死が先」を実現しているが、ARCHITECTURE.md に規則として書かれておらず、専用チェックも無い。
- 修正案: ARCHITECTURE.md の LoopDemo 行に「同一フレームでは時間進行（致死）を先に確定し、その後に入力を渡す」を追記。LoopModelChecks に `Advance(searchShot)` 直後の `TryExit` 拒否を1項目追加。

### A-9（提案）レビュー運用の負荷

- 段階1の最終受入が「Sonnet 再確認の timeout」で止まっている。文書のみの変更にコードと同じ二重独立レビューを課すと、この停止が繰り返される。
- 提案: 対象が `Docs/**`, `Collaboration/**` のみの変更は独立レビュー1件で受入可とし、二重レビューは `Assets/**/*.cs`, `Tools/*.py`, `Packages/*.json`, `.github/**` に限定する。AGENTS.md の「作業の進め方」に一行追加すれば足りる。判断はユーザー。

---

## Part B: コードレビュー

優先度: 高=段階2受入前に直す / 中=段階2〜3で直す / 低=任意。

### B-1（高）URP+XR で観客カメラが HMD に描画される可能性

- 場所: RoomVisuals.cs:116-117（`stereoTargetEye=None`, `depth=10`）
- 根拠: task004 で「SRP で stereoTargetEye 非対応」警告を観測済み。URP では XR 描画の可否は `UniversalAdditionalCameraData.allowXRRendering` で決まり、既定 true。depth 10 は HMD カメラ（0）より後に描く。
- 再現条件（推定）: Quest 3 で VR モード開始。HMD 内に俯瞰視点が上書きされる、または両眼へ二重描画される。
- 修正案: `using UnityEngine.Rendering.Universal;` を追加し、BuildSpectator で `Spectator.GetUniversalAdditionalCameraData().allowXRRendering = false;` を設定。同時に PC ウィンドウ側の XR ミラー表示が観客カメラと競合しないか実機で確認（UNITY_ACCEPTANCE §7 に追記）。
- 未確認: 実機描画。ソースからは高確率と判断するが断定しない。

### B-2（高）ウィンドウのフォーカス喪失でセッションが中断される

- 場所: LoopDemo.cs:83 `if (!Application.isFocused && !Application.isEditor && !idle) Model.Interrupt();`
- 再現条件: ビルド版で VR セッション中に運営者が別ウィンドウ（Meta Quest Link アプリ、観客モニターの別アプリ等）をクリックする。HMD 側は runInBackground=true で動き続けるのに、体験は Interrupted で終了する。
- 修正案: 条件に `!rig.IsVR` を追加する（デスクトップ確認モードだけの安全策にする）。VR 中の停止は Esc と追跡喪失に限定。

### B-3（高）3秒の足音（Latch）が毎周回鳴らない

- 場所: LoopDemo.cs:110 と RefreshWorld:160
- 根拠: `enemyAudio` は Enemy GameObject 上にあり、Enemy は `t>=3` で RefreshWorld が有効化する。Update 内では PlayOneShot（110行）が RefreshWorld（126行）より先に走るため、LoopTime が初めて3を超えたフレームでは Enemy がまだ非アクティブ。非アクティブな AudioSource の PlayOneShot は無音で警告のみ。
- 影響: 計画§5の「3秒: 扉の鍵・足音 = 危険の方向を知る手がかり」が消える。初回死亡の因果理解に直結する。
- 修正案: enemyAudio を常時アクティブな別 GameObject（扉位置）に置く。または Latch 再生の前に `room.Enemy.gameObject.SetActive(true)` を保証する。Play/Stop 中の Console 警告 "Can not play a disabled audio source" の有無で確認できる。

### B-4（中）XR 準備の再試行手段が無い

- 場所: DemoRig.cs:259, 266, 283（`xrInitializing=false` のみ。desktopFallback も再試行もなし）
- 再現条件: Link 未接続でアプリ起動 → 10秒経過 → その後接続しても CanStart は false のまま。
- 修正案: `public void RetryPreparation()` を追加し、`!xrInitializing && !CanStart` のとき `preparationReported=false; floorPrepared=false; xrInitializing=true; StartCoroutine(StartXR());` を実行。LoopDemo で運営キー（例: R）に割当。頭部追跡待ちのタイムアウトは撤廃してもよい（Floor 確定済みなら現状でも後から開始可能）。

### B-5（中）机の近縁が頭位置から 0.17 m にあり身体と交差する

- 場所: RoomVisuals.cs:72（Desk 中心 z=0.52、奥行 0.7）、LoopDemo.Begin:137（頭の xz を Root 原点にする）
- 影響: 立位の胴体が仮想の机に埋まる。計画§6「実体のない机への寄り掛かりを誘発しない」に反する配置。取っ手 0.36 m は計画の初期値内。
- 修正案: Root 原点を頭位置から前方に 0〜0.1 m ではなく、机の近縁が 0.30〜0.35 m になるよう Desk/取っ手/時計を一括で +0.15 m 程度ずらす。実機で到達確認。

### B-6（中）LoopRules の共有参照と再検証なし（Sonnet 指摘の再確認）

- 場所: LoopDemo.cs:11, 41、LoopModel.cs:61-65
- 現状: Inspector の `timings` と `Model.Rules` が同一参照。Play 中に Inspector で書き換えると Validate を通らない値が Advance に入る。
- 修正案（最小）: `Model = new LoopModel(timings)` の前に `timings` を複製して渡す（`JsonUtility.FromJson<LoopRules>(JsonUtility.ToJson(timings))`）。Issue4 の設計で恒久対応。

### B-7（中）Quest 3 のコントローラープロファイル

- 場所: DemoSetup.cs:98, 117（OculusTouchControllerProfile のみ）
- 現状: Assets/XR/Settings に `MetaQuestTouchPlusControllerProfile` が存在し無効。Quest 3 は Touch Plus。Link 経由では Oculus Touch プロファイルで動く報告が多いが、公式に一致するのは Touch Plus。
- 修正案: ConfigureXR で Touch Plus も有効化し、Validate では「どちらか一方が有効」を合格条件にする。バインディングは `<XRController>{RightHand}/...` の汎用なので変更不要。実機で A/X・grip を確認。

### B-8（中）Build-Demo.ps1 が既存 Editor の起動を確認しない

- 場所: Build-Demo.ps1:12
- 影響: AGENTS.md「ユーザーが Unity を開いている間は同じプロジェクトを別 Editor で開かない」に反し、未保存編集を壊すおそれ。
- 修正案: 実行前に `Temp/UnityLockfile` の存在を確認し、あれば中止する。

### B-9（中）Program.cs がチェック件数を固定している

- 場所: Tests/LoopModel.Tests/Program.cs:11 `existing.Count == 11`
- 影響: LoopModelChecks に1項目足すたびに CI が落ちる。
- 修正案: `>= 11` にするか、件数ではなく期待する名前の包含で検証する。

### B-10（低）XROrigin の Awake 警告

- 場所: DemoRig.cs:70-72
- 現状: `AddComponent<XROrigin>()` の時点で Awake が走り Camera 未設定の警告（task004 で観測）。
- 修正案: 生成先 GameObject を一時的に `SetActive(false)` にしてから AddComponent し、Camera/offset を設定してから有効化。

### B-11（低）その他

- LoopDemo.cs:81 Esc は確認なしで即中断。運営用として許容するが、UNITY_ACCEPTANCE に「Esc は運営専用」と明記。
- LoopDemo.cs:196 OnGUI は VR 中も PC 画面に LOOP 番号と状態を描く。計画§10は周回数の観客表示を許容しているので問題なし。ただし F2 有効時の攻略キー表示は既に受入項目で扱われている。
- RoomVisuals.cs:128 `UpdatePublic(bool vr, bool active)` の `active` 未使用。
- RoomVisuals.cs:104 暗転 Quad は PrivateLayer のため観客は死亡の瞬間を視認できない（A-2 と同じ論点）。
- Font.CreateDynamicFontFromOSFont が3箇所で別々に呼ばれる。1つに共有してよい。
- DemoSetup.cs:129 `BuildOptions.Development` は展示版では外す。段階5。
- DemoSetup.cs:33 Mono2x は試作では妥当。展示版で IL2CPP にするかは段階5で判断。
- Tools/sonnet_packet.py:51 の指示文「assigned by the GPT-6 Astra design coordinator」は現行分担（Sol 管理）と不一致。文字列を更新する。
- Tools/automation.py `run_task` は task001 専用（TARGET/TASK/1.12→1.17 が定数）。文書は既にそう扱っているので、`run-001` を「archived」と CLI help に明記するだけでよい。
- Tools/automation.py:72 `local()` は `resolve() != absolute()` でリンクを拒否する。Windows で大文字小文字や 8.3 名が絡むと誤拒否しうる。現状テストは通っているため記録のみ。

### 指摘なし（確認済み）

- LoopModel.Advance の境界分割は妥当。slice が 0 になっても `LoopTime >= next - ε` で必ず前進し無限ループしない。exitCloses == searchShot の既定では 12 秒ちょうどで出口が閉じ同時に回り込み致死となり、`ExitAvailable` の `<` と整合する。
- 旧周回入力の拒否（expectedLoop）、死亡の一意性、Blackout 中の TimedOut、Finished 後の Start は既存12チェックとソースで整合。
- DemoRig.Operate の needsRelease により、暗転をまたいで握り続けたグリップが次周回で再解釈されない。計画§7-5 に一致。
- 隠しオブジェクトは shadowCastingMode=Off で影漏れなし。
- CI の Actions は commit SHA で固定、permissions は contents:read、concurrency あり。
- .gitignore は生ログ・PAUSE・usage・runs を除外。

---

## Part C: 未確認事項

- Quest 3 実機での全項目（Floor、頭・手追跡、到達、3回死亡、3回 Play、外部計測180秒、観客秘匿、ビルド）。
- B-1 の実際の描画結果。B-3 は Editor Play 中の Console 警告で確認可能。
- OpenXR 経由での `InputDevices.SendHapticImpulse` の動作（DemoRig.Haptic）。
- `com.unity.pipeline` の用途。
- `--tools ''` オプションを含む Claude CLI 呼出しは STATE の nonce 確認記録に依拠し、今回は再実行していない。

## Part D: 推奨する着手順（修正時）

1. B-2（1行）、B-3、B-1 を段階2の最初の実装タスクとしてまとめる。同一 SHA で Sol/Sonnet レビュー。
2. B-4 + A-4 を「運営復旧」タスクに。
3. A-1 の方針決定（ユーザー判断）。決めた方針を STAGE_PLAN と ARCHITECTURE に反映。
4. A-5 の履歴文書バナー、B-9、sonnet_packet の文言は文書・低リスク変更として1件にまとめる。
5. B-5、B-7 は実機接続後に確認しながら調整。
