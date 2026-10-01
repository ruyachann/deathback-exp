# 独立レビュー: task014（クオリティ第1弾：部屋の見た目と雰囲気）

対象SHA256（提供スナップショットより）:
- `Assets/LoopRoom/Scripts/RoomVisuals.cs`: `5e30a7380ff6a18cd7d88af42c705fb1e0bda24f9a1eefb73c81bd78d1145b5a`
- `Assets/LoopRoom/Shaders/LoopRoomText.shader`: `ba0b47dba01f227317a2db719f27e700b5ca1b1cda9973d450d0622b9d77c996`
- `Assets/LoopRoom/Editor/DemoSetup.cs`: `cd827835ae28d895813d4d18ae572c57c5fe152c97a6e3b20b5a7bd2f962b2ed`
- `Collaboration/tasks/014-quality-visual-atmosphere.md`: `e7579dff4c9adae6c5ec39606d64c5759b6379dc1bc46e7041c5d4d52052545d`

## 指摘事項

### [High] LoopRoomText.shader: `#pragma target 2.0` とステレオ/インスタンシングマクロの不整合
- 場所: `Assets/LoopRoom/Shaders/LoopRoomText.shader` 28行目付近（`#pragma target 2.0`）、`UNITY_VERTEX_INPUT_INSTANCE_ID` / `UNITY_VERTEX_OUTPUT_STEREO` / `multi_compile_instancing` の各使用箇所
- 再現条件: Quest 3 PCVR、OpenXR Single Pass Instanced でこのシェーダーが描画される場面（文字全般）
- 問題: Unity公式仕様上、GPU Instancing（したがってSingle Pass Instancedのステレオ出力も含む）には Shader Model 3.5 以上が必要（デフォルトの`#pragma target`は2.5だが、それすら不足とされる）。本シェーダーは明示的に`target 2.0`を指定しており、`multi_compile_instancing`や`UNITY_VERTEX_OUTPUT_STEREO`が期待通りにコンパイル・動作しない可能性が高い。片目のみ描画される、インスタンシングが無効化される、あるいはインポート時に警告が出る等のリスクがあり、task014の受入条件1（batchmodeコンパイル警告0）およびVRでの正しい表示に直結する。
- 修正案: `#pragma target 2.0` を `#pragma target 4.5`（またはURPの他のUnlitシェーダーに倣った値、最低でも3.5以上）に変更する。

### [Medium] RoomVisuals.cs: `TextMesh.font` 再設定時の `sharedMaterial` 自動上書きリスク（未確認箇所への波及）
- 場所: `Assets/LoopRoom/Scripts/RoomVisuals.cs` の `Text()` メソッド（TextMesh生成部）と `TextMaterial()`
- 再現条件: 実行時に時計（Clock）など既存TextMeshの`font`プロパティが再代入される場合（`text`のみの更新なら該当しない）
- 問題: UnityのTextMeshコンポーネントは `font` プロパティ設定時に付随するRendererのマテリアルを自動更新する既知の挙動がある。本ファイル内の初期構築コードでは `mesh.font = font;` の後に明示的に `renderer.sharedMaterial = TextMaterial(font);` を設定しており初期化時点では問題ないが、もし別ファイル（`LoopDemo.cs`、今回のスナップショットに含まれず未確認）で時計更新時に`font`自体を再代入していれば、`LoopRoom/Text`マテリアルが`font.material`（既定のZTest挙動）へ静かに戻され、追修正3〜4で意図した奥行き描画が壊れる。
- 修正案: `LoopDemo.cs`側で`Clock.text`のみを更新し`Clock.font`は再設定しないことを確認する。念のため`RoomVisuals`側で定期的に`sharedMaterial`を再アサートするか、コメントで契約を明示する。
- 備考: `LoopDemo.cs`は本スナップショットに含まれておらず、実際に該当コードがあるかは**未確認**。

### [Low] RoomVisuals.cs: `Pendant shade` の `shadowCastingMode` 設定方法の不統一
- 場所: `Assets/LoopRoom/Scripts/RoomVisuals.cs`（`Detail("Pendant shade", ...)` の行）
- 問題: 他の非表示影オブジェクトは`hidden`引数や`Shape`/`Detail`の統一経路で`shadowCastingMode`を制御しているが、この行だけ`GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off`を戻り値に対して直接呼び出しており、設計の一貫性を欠く。機能的な欠陥ではないが、以後の変更時に見落としやすい。
- 修正案: 必須ではないが、他の物体と同様に引数経由で制御する形に揃えると保守性が上がる。

### [Low] RoomVisuals.cs: `BuildPostProcessing` の `VolumeProfile` / 各 `VolumeComponent` の破棄なし
- 場所: `Assets/LoopRoom/Scripts/RoomVisuals.cs` の `BuildPostProcessing()`
- 問題: `ScriptableObject.CreateInstance<VolumeProfile>()` および `profile.Add<Bloom>/<Vignette>/<Tonemapping>/<ColorAdjustments>` で生成したインスタンスは明示的な破棄処理がない。`RoomVisuals.Build()`が周回（Loop）ごとに複数回呼ばれる設計であれば、呼び出しのたびにリークする。
- 修正案: `Build()`が1シーンにつき1回のみ呼ばれることを確認する（未確認、`LoopDemo.cs`依存）。複数回呼ばれる場合は生成済みの`Volume`/`VolumeProfile`を再利用するか明示的に`Destroy`する。

## 検証できたこと（静的解析ベース）
- `RenderSettings.fog` の密度は追修正2の指示値（.012）と一致。
- `Tonemapping.mode = Neutral`、`postExposure = .4f` は追修正2の指示と一致。
- 観客用の`publicHead/Left/Right`（layer 9）に対する `shadowCastingMode=Off` / `receiveShadows=false` は追修正5の指摘通りに実装済み。
- `DemoSetup.cs` の常時同梱シェーダー一覧に `"LoopRoom/Text"` が追加されており、見つからない場合は例外を投げる設計になっている（task014追修正4の指示通り）。
- `TextMaterial()` は `unity_GUIZTestMode` 方式を廃し、専用シェーダーへの差し替えに変更されている（設計方針としては追修正4の指示に沿う）。

## 未検証事項（Unity/実機での確認が必要）
- FOCUSに記載の `text-depth-crop.png` 等の実際の画面証跡内容（本レビューでは画像自体を参照できない）。
- Unity batchmodeでの実際のコンパイル結果（エラー0・警告0）。特に `#pragma target 2.0` によるシェーダーインポート警告の有無。
- モデルテスト全件PASSの実行結果。
- 計測回転当たりの検証（ビルド 0/0、平均値0、文字が遮蔽・パネルに正しく隠れるか）はビルド実行なしでは確認不能。
- `LoopDemo.cs` 内での `Clock`（TextMesh）更新方法（`text`のみか`font`も再設定するか）。
- `RoomVisuals.Build()` の呼び出し回数（周回ごとに複数回呼ばれないか）。
- HMD内の見え方・フレームレート（タスク自体が実機未確認と明記）。

## 判定

**request_changes**

理由: LoopRoomText.shaderの`#pragma target 2.0`はVRのSingle Pass Instanced/GPU Instancingマクロと技術的に整合しない可能性が高く、task014の核心（VRで文字が正しい奥行きで描画されること）に直接影響しうるため、修正または動作確認（実機/Play Modeでのステレオ描画確認）が必要と判断する。