# task014 独立レビュー（最終判定）

対象SHA256（引用元スナップショット）:
- `Collaboration/tasks/014-quality-visual-atmosphere.md` = `633dce9e98fb5de70cd7bf457347da63bf3be5b8b479f4baa2283320d49e5236`
- `Assets/LoopRoom/Shaders/LoopRoomText.shader` = `97edd79e2a9f4db25ed5f48d6d61f9e596c88fd4f74b463d1d93fb79f47fffd2`
- `Assets/LoopRoom/Scripts/RoomVisuals.cs` = `5e30a7380ff6a18cd7d88af42c705fb1e0bda24f9a1eefb73c81bd78d1145b5a`

## FOCUS項目1: 追修正6（#pragma target 2.0→3.5）の確認

`LoopRoomText.shader` の該当行:
```
#pragma target 3.5
#pragma vertex Vert
#pragma fragment Frag
#pragma multi_compile_instancing
```
`3.5` に修正済みで、指摘は反映されている。Single Pass Instanced対応の定石マクロ（`UNITY_VERTEX_INPUT_INSTANCE_ID`／`UNITY_VERTEX_OUTPUT_STEREO`／`UNITY_SETUP_INSTANCE_ID`／`UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO`／`UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX`）もVert/Frag双方に揃っており、構成上の欠落は見当たらない。

## FOCUS項目2: 不採用・見送り判断の妥当性

- **font再代入で素材が戻る問題（不採用）**: `RoomVisuals.cs:78` `mesh.font = font;` の直後、`RoomVisuals.cs:81` `renderer.sharedMaterial = TextMaterial(font);` でカスタムマテリアルに上書きしている。順序が正しいため、計画担当の「1回だけで、その後に素材を設定している」という説明は本ファイル内では裏付けられる。ただし `LoopDemo.cs` は今回のスナップショットに含まれておらず、そちら側で `Clock.font = ...` のような再代入が新規に発生していないかは未確認（下記参照）。
- **Pendant shadeの影設定の書き方（見送り・低）**: `.GetComponent<Renderer>().shadowCastingMode=ShadowCastingMode.Off;` をワンライナーで後付けしているだけで、動作上は問題なし。見送り妥当。
- **VolumeProfileの破棄（見送り・低）**: `BuildPostProcessing` 内で `ScriptableObject.CreateInstance<VolumeProfile>()` を生成後、明示的な `Destroy` がない。`Build()` がシーン生成時に1回のみ呼ばれる設計であれば実害は小さいが、周回リセット等で複数回呼ばれる設計の場合はリークしうる。呼び出し回数はLoopDemo.cs側の制御であり本ファイルからは確認できないため、見送り判断自体は妥当だが根拠はLoopDemo.cs側の検証待ち。

## その他の発見事項

| severity | file/line | trigger | fix |
|---|---|---|---|
| Low | `Assets/LoopRoom/Editor/DemoSetup.cs`（本レビューのスナップショットに未収録） | 追修正4は「常時同梱シェーダー一覧に `"LoopRoom/Text"` を追加」をDemoSetup.csの変更として要求しているが、当該ファイルが今回のSNAPSHOTに含まれていない | 別途DemoSetup.cs差分を提示してレビューする必要あり。未提示のまま「タスク014全体を最終判定」はできない |
| Info | `RoomVisuals.cs` TextMaterial呼び出し箇所（Card/Clock/ShieldMark/ExitMark/RoomNumber/RoomDetail、計6箇所） | `Text()` 呼び出しごとに `new Material(font.material)` で個別インスタンスを生成 | SRP Batcher効率がわずかに下がるが対象数が少なく実害は軽微。指摘のみ、修正不要 |

## verdict

**approve（条件付き）**

コード（RoomVisuals.cs、LoopRoomText.shader）自体に機能的な欠陥は見当たらず、FOCUSの主眼である追修正6（`#pragma target 3.5`）と追修正5（publicHead/Left/Right の `shadowCastingMode=Off`／`receiveShadows=false`、`RoomVisuals.cs` `BuildSpectator()` 内で確認済み）は正しく反映されている。ただし下表の「DemoSetup.cs未提示」はタスクの受入条件・追修正4の範囲に直結するため、当該ファイルの差分確認なしに「task014全体（追修正1〜6）の最終受入」を完了扱いにはできない。

## unverified checks（未検証事項）

- Unity batchmodeコンパイルのエラー0・警告0、モデルテストPASSはログ未提示のため未検証。
- ビルドでのシェーダーエラー0・コンパイル0/0・例外0は本レビューでは実施不可（実行環境なし、ログ未提示）。
- 変更前後のdesktop画面スクリーンショット、観客カメラ画面での手がかり非表示は画像未提示のため未検証。
- `Assets/LoopRoom/Editor/DemoSetup.cs` の常時同梱シェーダー一覧への追加は該当ファイル未提示のため未検証。
- `LoopDemo.cs` 側でのTextMesh `font` 再代入の有無（マテリアル巻き戻りリスク）は当該ファイル未提示のため未検証。
- HMD内の見え方・フレームレートはタスク文書内でも「実機で確認（計画6、未確認）」と明記されている通り未検証。