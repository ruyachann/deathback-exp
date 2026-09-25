# 002 — XR開始をStart完了後にし、Floor準備後に開始を許可する

状態: Astra設計に基づきSolが実装担当。最新の実施状況はCollaboration/STATE.mdを参照。001の依存解決後に着手する。

## 問題と範囲

`DemoRig.Initialize()` が `LoopDemo.Start()` から呼ばれ、明示的なフレーム待機なしにXR手動初期化が始まる。XR Management 4.5.4 `XRManagerSettings.InitializeLoader` はStart完了後の呼出しを要求している。またCore Utils 2.6.0は入力Subsystemがない時の原点初期化を成功扱いするため、遅れてできたSubsystemにFloor要求が適用される保証がない。

変更許可: `Assets/LoopRoom/Scripts/DemoRig.cs` と、必要な開始許可/準備表示のための `Assets/LoopRoom/Scripts/LoopDemo.cs` の最小変更。その他は提案にとどめる。PackageCache、ProjectSettings、部屋配置、モデル時刻、既存シーンは変更しない。

## 実装する順序

1. 既存のInputAction生成・有効化を保持する。XR初期化Coroutineでは、loader初期化より前に少なくとも1フレーム明示的にyieldする。
2. XROrigin参照を保持する。Subsystemができる前のFloor要求だけを頼りにせず、loader/Subsystem開始後に準備フェーズでFloor要求を適用する。
3. Floorが実際のrunning XRInputSubsystemで確認され、HMD位置・回転追跡が利用できるまではVRのセッション開始を許可しない。表示上も準備中/未準備と開始可能を区別する。DesktopのEnter開始は保持する。
4. Floorに未対応・設定失敗・Subsystemなしの場合、無期限の主スレッド待機や毎フレームの再センターを行わない。準備未完として既存ゲームの開始を止め、診断可能な状態を1回報告する。デバッグ出力は毎フレーム出さない。
5. 原点設定の適用はXR起動の準備処理に限定する。BeginLoop/死亡/通常Playing/Blackoutへ原点設定やoffsetの再設定を追加しない。

## 受入条件

- 001更新後の実APIでコンパイルが通る。
- XR初期化経路の明示的なyieldをソースレビューで確認する。
- 通常起動と遅延起動で、Floorの実確認前にはA/X/EnterがVR開始を起こさない。
- Quest 3で床・頭・左右手の高さが一致し、到達可能な既存取っ手を握れる。
- 死亡を3回繰り返し、XR Origin / Tracking Offsetにアプリ由来の位置・回転変更がない。
- Play開始/停止3回でXR二重開始・終了後入力の例外がない。`--desktop` は従来のEnter/Space/Eを維持する。
- UI/機器が使えない検証は未実施と明記する。ソースレビュー合格を実機合格に代用しない。
- 変更ファイルSHA256、Sol/Sonnet両方の独立レビューと指摘対応を残す。

## リスクと戻す判断

Floorの確定待ちが環境固有で設計範囲を超える、またはXR起動済み/外部管理ケースの所有権変更が必要なら実装を拡大せずAstraへ返す。Frame待機追加だけで床基準まで検証済みとしない。既存`ownsXR`の所有権を保ち、自分が開始していないloaderを無条件に解放しない。
