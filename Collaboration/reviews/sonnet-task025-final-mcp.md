# 独立レビュー(最終・追修正2適用後): Task025 手続き生成テクスチャ

対象SHA256（本レビューで実際に解析したソース）
- `Assets/LoopRoom/Scripts/RoomVisuals.cs`: `bdc4fd3eb8a7a50a1ab682a405058e3eee549f4a2a3ab05b5f0e040da253e5fb`
- `Collaboration/tasks/025-procedural-textures.md`: `029f35e06d4a2748cbbbecfcf60ca1c5ed8a7f00ffb6fdfc817b69b1158a4ea0`
- `AGENTS.md`: `17a8b55855402be023072b21e66a2378c980ade41e9b08062e7ec498cd36df84`
- `CLAUDE.md`: `fa38f7c0c30475591ff99b65e2e95c9b39538915fb6bd703a3f3b900c68d2a82`

`RoomVisuals.cs` のSHAは前回交換後レビュー時（`04d9aea3855900d5e69da836965cb071f3f372b7c15c2e7df56ee88cccf44a4b`）から変化しており、追修正2が適用された新しいバージョンであることをまず確認しました。

## 追修正2-1（壁の基準値253→249、クリップ解消）の検証

`RoomVisuals.cs:166-178` `WallTexture()`

```
float plaster=Mathf.Sin(px)*Mathf.Sin(py)*3f+Mathf.Sin(px*3f+py*2f)*1.5f;
float value=249f+plaster+(PeriodicNoise(x,y,size,32,71u)-.5f)*2.5f;
```

- 基準値は249。`plaster` の理論上限/下限は ±(3+1.5)=±4.5、`PeriodicNoise` 項は範囲[0,1]の補間値から`(v-.5)*2.5`で±1.25。
- `value` の理論範囲は **243.25〜254.75**。`Gray()`（`RoomVisuals.cs:122-126`）は0〜255でクランプするが、この範囲は上下どちらの境界にも到達しないため**クリップは発生しません**。
- FOCUSに記載の実測値（244.3〜253.9、平均249.1）は上記理論範囲に収まっており、矛盾はありません。追修正2の指摘「基準値を249〜250程度に下げ、±5程度でクリップしない」は満たされていると判断します。

## 追修正2-2（Build()冒頭でキャッシュを空にする）の検証

`Build(Transform owner, DemoRig rig)` メソッドの最初の行に `materialCache.Clear();` が追加されています。`materialCache` は `static readonly Dictionary<MaterialKey, Material>`（クラス冒頭、フィールド宣言部）であり、`Build()` 呼び出しのたびに先頭でクリアされるため、前回指摘の「複数回`Build()`を呼ぶと古いマテリアル/テクスチャがキャッシュに蓄積する」問題は解消されています。

## 新規に見つけた軽微な指摘

1. **[低・情報] `FabricTexture()` で極めて稀にクリップし得る**  
   `RoomVisuals.cs:180-192`  
   `value=251f+(warp==2?2f:...)+(weft==6?2f:...)` の最大側は `251+2+2=255`、これに `PeriodicNoise` 項の理論上限 `+0.6` が加わると `255.6` となり、`Gray()` で255にクランプされます。発生条件は `x%8==2 かつ y%8==6` の格子点（全体の1/64）かつノイズが最大付近という稀な組合せに限られ、影響は視認上ほぼ無視できるレベルです。受入をブロックする欠陥ではありません。修正するなら基準値を1〜2下げれば解消します。

2. **[低] `materialCache` が static のためインスタンス間で共有される**  
   `RoomVisuals.cs:22`（フィールド宣言）  
   `RoomVisuals` の複数インスタンスが同時に存在し、一方が `Build()` を呼ぶと他方のキャッシュも消去されます。現状の使用パターン（1シーンにつき1インスタンス）では実害はありませんが、設計上の注意点として記録します。

追修正1（周期性）・追修正2の額縁木目・`--desktop-pitch` については、コード上のロジック（`FloorTexture`/`WoodTexture`/`WallTexture`/`FabricTexture` の整数係数`Sin`と`PeriodicNoise`の格子折返し、`Picture frame` へのテクスチャ付与）に前回レビュー時点から変化はなく、既存の確認結果（`sonnet-task025-exchange-mcp.md` 記載の解析）と矛盾する新たな欠陥は見当たりません。

## 判定

**approve**

主要な2つの修正（壁テクスチャのクリップ解消、`Build()` 冒頭でのキャッシュクリア）はコード上正しく反映されており、新たな重大な退行も見当たりません。見つかった2点（FabricTextureの稀なクリップ、materialCacheの静的スコープ）は低重要度で受入をブロックしません。

## 未確認事項

- Unityコンパイル、batchmode 0/0、テスト全件PASS、ビルド0/0、例外0は実行していません。
- `Collaboration/evidence/20260925-textures/` の画像（変更前後比較、床・机・壁の近接構図、`--desktop-pitch 40`時の床）はデータが提供されておらず、目視確認はできていません。
- `Assets/LoopRoom/Scripts/DemoRig.cs` は今回のスナップショットに含まれていないため、「前回から変更なし（前回交換後レビューに記録されたSHA `240a043975c167832f18e1e84d48360fe6de864ca1ae411bbc946b737cf910f2`）」という前提は直接検証できていません（この値は今回私が算出したものではなく、`Collaboration/reviews/sol-task025-exchange-mcp.md` からの引用です）。
- Quest 3実機でのちらつき・モアレ、異方性フィルタの効果、Cube各面のUV実測での伸び方。
- `LookAtFloorForCalibration` の呼び出し経路（DemoRig.cs未提示のため）。