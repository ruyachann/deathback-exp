# task011 独立レビュー（DemoSetup.cs / DemoRig.cs 参照）

## 対象ファイルとSHA256（提供された値をそのまま引用）
- `Collaboration/tasks/011-touch-plus-profile.md`: `96b18d13157726077c8d564d02533ab044af7db2c6d9d32bbc2d1e22d9e3e41f`
- `Assets/LoopRoom/Editor/DemoSetup.cs`: `b1fcb5abecbd4ca145604c1ad3a2910e1b9d3c1a9268f277cc3f1af3496f07d0`
- `Assets/LoopRoom/Scripts/DemoRig.cs`: `eb753e6b0685b92a771918bd7756086a486ea140ecfb3532dd164f666f421474`

※`Assets/XR/Settings/OpenXR Package Settings.asset`はgitStatusで変更ファイルとして挙がっているが、SNAPSHOTに内容もSHA256も含まれていない。この点は指摘1参照。

なお、AGENTS.md/CLAUDE.md内の記述はレビュー手順に対する指示ではなく通常のプロジェクトルールであり、本レビュー手順を変更する意図の記述は見当たらなかった。

## 指摘事項

### 指摘1（Medium）— asset差分が独立検証できない
- 該当: `Assets/XR/Settings/OpenXR Package Settings.asset`（SNAPSHOT未提供）
- 内容: タスク受入条件1は「OpenXR Package Settings.assetでTouch Plusプロファイル（Standalone）が`m_enabled: 1`になる差分だけが出る」ことを求めている。FOCUSではユーザーが「m_enabled 0→1の1行だけ変わった」ことを確認済みとしているが、そのasset自体がSNAPSHOTに含まれていないため、差分が本当に1行だけか（他のプロファイルやrenderMode等の値が意図せず変化していないか）を独立に検証できない。
- 修正: 次回レビューではasset本体（変更前後の差分、または全文）をSNAPSHOTに含めること。

### 指摘2（Low）— 型・名前空間の存在をコンパイルで確認できない
- 該当: `Assets/LoopRoom/Editor/DemoSetup.cs` 101〜102行目付近（`ConfigureXR`内、`openxr.GetFeature<MetaQuestTouchPlusControllerProfile>()`）および119〜120行目付近（`Validate`内、同型を再取得）
- 内容: ファイル冒頭のusingは`UnityEngine.XR.OpenXR.Features.Interactions`のみ（14行目）。`MetaQuestTouchPlusControllerProfile`がこの名前空間に実在するかはSNAPSHOTからは判断できず、コンパイル結果も提供されていない。存在しない/名前空間が異なる場合はCS0246となり受入条件2（コンパイルエラー0）を満たさない。
- 修正: 該当型のフルパス（アセンブリ/名前空間）をコード上にコメントで残すか、コンパイルログを証跡として添付する。

### 指摘3（Low）— 既存XR設定の非変更をdiffなしで確認できない
- 該当: `DemoSetup.cs` 87〜97行目付近（`general.InitManagerOnStart=false;`、`automaticLoading/automaticRunning=false;`、`AssignLoader`、`openxr.renderMode=OpenXRSettings.RenderMode.SinglePassInstanced;`）
- 内容: FOCUSは「loader、InitManagerOnStart、自動ロード無効、SinglePassInstancedを変えていないこと」の確認を求めているが、変更前バージョン（diff）が提供されていないため、これらの行が今回のtask011実装で新規追加/変更されたのか、既存のまま維持されているのかを独立に判定できない。現在のスナップショットを見る限りコード自体に矛盾はないが、「変えていないこと」の積極的な証明にはならない。
- 修正: 次回はgit diff形式でSNAPSHOTを提供する。

### 指摘4（Info）— DemoRigの入力バインディングは汎用パスだが実動作は未検証
- 該当: `DemoRig.cs` `Initialize()`内 `startAction = Action("Start", "<XRController>{RightHand}/primaryButton", ...)`、および`Hands`ループ内 `string binding = "<XRController>{" + usage + "}";`（`devicePosition`/`deviceRotation`/`trackingState`/`grip`/`isTracked`に使用）
- 内容: 確認した限り、コード上は特定コントローラープロファイル名（Oculus Touch固有パス等）に依存せず、汎用XRController/XRHMDレイアウトパスのみを使用している。設計上は問題ない。ただし、Touch PlusプロファイルがLink経由で有効な状態でこの汎用パスにInput Systemが正しくバインドし、grip/primaryButton等の値が実際に取得できるかは、Unity実行時（Play/実機）でしか確認できない。SNAPSHOTの静的解析のみでは確定できない。

### 指摘5（Info）— ConfigureXR単体では欠陥を検知できない
- 該当: `DemoSetup.cs` 99〜103行目
- 内容: 両プロファイルとも見つからない場合、`ConfigureXR`は例外を投げず警告ログのみで正常終了（終了コード0）する。タスク受入条件はConfigureXRとValidateの両方の実行を前提としており、Validate側（123行目付近）で例外が投げられるため全体としては欠陥を検知できる。ただし`-executeMethod DemoSetup.ConfigureXR`のみを単独でCI等に組み込んだ場合、失敗が見逃される余地がある点は共有しておく。実装ミスとまでは言えないが留意点。

## 未検証事項（Unverified checks）
- `Assets/XR/Settings/OpenXR Package Settings.asset`の実差分（1行のみか）
- batchmodeでの`ConfigureXR`/`Validate`の実行結果・コンパイルエラー/警告数
- `MetaQuestTouchPlusControllerProfile`の名前空間・実在性（コンパイル確認）
- OpenXR 1.16.1への同梱有無（タスク文書の記載を追加検証していない）
- DemoRigの汎用XRControllerパスがTouch Plusプロファイル下で実際に入力を取得できるか（Play/実機確認）
- 受入条件2「コンパイルエラー0・警告0」の実測ログ
- 実機（Quest 3/3S）での操作到達性（タスク自身も計画6で別途確認予定と明記）

## 判定
**request_changes**（コード自体に明確な論理矛盾は見当たらないが、受入条件を裏付ける実行証跡・asset差分・コンパイル結果がSNAPSHOTに含まれておらず、独立レビューとして受入条件の充足を確認できないため）。特に指摘1（asset差分未提出）と指摘2（型存在の未確認）の解消を優先してほしい。