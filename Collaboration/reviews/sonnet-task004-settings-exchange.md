# Sonnet task004 settings exchange (2026-09-24)

modelUsage: claude-sonnet-5

DemoSetup.cs のコードを確認しました。指摘した各項目は同ファイル内の明示的な処理に対応しており、Sol の根拠とも一致します。

## 1. 指摘ごとの最終見解

| 指摘 | 判定 | 理由 |
|---|---|---|
| fullscreenMode 1→3 | 解消 | DemoSetup.cs:28 `PlayerSettings.fullScreenMode=FullScreenMode.Windowed;` で明示的に設定。Prepare実行時の意図的な値であり副作用ではない。 |
| GraphicsAPI 手動指定(D3D11) | 解消 | DemoSetup.cs:31-32 で `SetUseDefaultGraphicsAPIs(false)` → `SetGraphicsAPIs(Direct3D11)`。自動選択を避け単一APIに固定する意図が明確。 |
| runInBackground 0→1 | 解消 | DemoSetup.cs:29 で明示設定。VR実行中に運営者が別ウィンドウを操作する運用要件（LoopDemo側の `Application.runInBackground=true` とも整合）に合致。 |
| AlwaysIncludedShaders GUID 2件 | 解消 | DemoSetup.cs:43-51 で `Shader.Find("Universal Render Pipeline/Lit"/"Unlit")` を追加。GUIDがURPパッケージのLit/Unlit `.shader.meta` と一致確認済みで、RoomVisualsが名前検索する実行時経路のためのビルド除外防止と説明がつく。 |
| OculusTouchControllerProfile(Standalone) 有効化 | 解消 | DemoSetup.cs:98 で有効化、Validate:116-117 が未有効なら例外を投げる必須項目として扱っており、意図的かつ検証で担保されている。 |
| PlayerOnly/SpectatorOnly レイヤー | 解消 | DemoSetup.cs:38-39 で明示的に設定。推測ではなくコードで裏付けられた。 |
| AndroidMinSdkVersion 22→25 | 保留（ブロッカーではない） | DemoSetup.csはAndroidに触れておらず、Unity 6000.3移行によるシリアライズ変更と考えるのが妥当。本プロジェクトはPCVR(Windows)専用でAndroidビルドはSTATE.md記載の通り範囲外のため、この差分は承認を妨げる理由にならない。 |
| EditorBuildSettings.asset の loader_settings 追加 | 解消 | DemoSetup.cs:68-73 の `AddConfigObject` 経路で生成される正規のconfig object参照であり、Solの確認（GUID/fileIDがGeneralSettings.assetのmetaと一致）とも整合。 |

Quest 3S / MetaQuestTouchPlus プロファイルの件は task006 B-7 で別途扱う対象であり、本タスク(task004)の判定には影響しません。

## 2. 最終判定

**approve** に変更します。

## 3. 補足

前回の「意図確認が必要」という留保は、DemoSetup.csの実コードを未確認のまま設定ファイルのdiffのみで評価していたために生じたものでした。今回Prepare/ConfigureXR/Validateの該当箇所を読み、各差分がコード上の明示的な意図（PCVR単一ウィンドウ起動、D3D11固定、バックグラウンド実行維持、シェーダー除外防止、Oculus Touch必須化、レイヤー追加）に一対一で対応することを確認したため、判定を変更します。AndroidMinSdkVersionの一件のみ根拠コードが存在しませんが、範囲外プラットフォームのシリアライズ副作用と判断でき、承認を妨げるものではありません。
