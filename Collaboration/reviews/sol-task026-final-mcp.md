## 判定

**request_changes**

前回の2指摘（ms変換時の無限大、1.5倍境界検査）は解消されています。ただし、`Dropped` の契約に反する入力ケースが残っています。

## Findings

### 1. Low — 3600秒の飽和によって `Dropped` を過少計上する

- ファイル: `Assets/LoopRoom/Scripts/FrameStats.cs:44-61`
- SHA256: `3d306e13107607ad5a8854196e68ce21cb38aaeb000bdfa1cf8093a22dd31aef`
- トリガー例:
  - `new FrameStats(1.0 / 3600.0)`
  - `Add(6000.0)`
- 問題:
  - 本来の目標フレーム時間は3600秒、Dropped閾値は5400秒なので、6000秒はDroppedです。
  - 現実装は先に3600秒へ飽和し、3600000msと5400000msを比較するため、`Dropped == 0` になります。
  - コンストラクタが「有限かつ正」の全targetHzを許可している一方、契約である「dtが目標フレーム時間の1.5倍を超えた回数」を満たしません。
- 修正:
  - 平均・最大・ヒストグラムには飽和後の値を使用する。
  - Dropped判定だけは元の有限な`dtSeconds`で行う。例として`droppedThresholdSeconds = 1.5 / targetHz`をコンストラクタで保持し、`dtSeconds > droppedThresholdSeconds`を比較する。
  - 上記トリガーの回帰検査を追加する。

### 2. Low — 長期稼働で件数とヒストグラムが符号付き整数オーバーフローする

- ファイル: `Assets/LoopRoom/Scripts/FrameStats.cs:21,26-31,49-60,73-84`
- SHA256: `3d306e13107607ad5a8854196e68ce21cb38aaeb000bdfa1cf8093a22dd31aef`
- トリガー:
  - 累計フレーム数、Dropped数、または同一バケットの件数が`Int32.MaxValue`を超える。
  - 165Hzならフレーム総数は約151日の連続計測で到達します。
- 問題:
  - `Frames`、`Dropped`、`int[] buckets`がuncheckedで負数へ回り、平均とP95が破綻します。
  - 180秒制限は撤廃されており、「長いセッションでも固定メモリ」という設計目的に対する潜在的な上限が明記されていません。
- 修正:
  - 件数とバケットを`long`にするか、明示的な飽和方針を設ける。
  - P95順位はオーバーフローや浮動小数点化を避け、例えば`N - N / 20`で求める。

## 確認できた修正

現行の検査ソース（SHA256 `b3cb0de16c3497d31f6ab10c659b6aba040c008d85c8ed5bc6cbe5292e2a9b8f`）では、128Hzの二進数で厳密な境界と隣接doubleを検査しています。`double.MaxValue`も3600秒へ飽和してからmsへ変換されるため、今回の追修正2の直接の2指摘は解消済みです。

テスト入口（SHA256 `5c21c1f54105450bda32790263dd25fef669df22161bf1ca34f0debdbd6418a7`）も11件を要求しており、証跡本文（SHA256 `6f8f893079efaee58b434cb97086344d2c046da5b50830e9266b067c5978ef5b`）の20+12+11 PASSおよび対象3ファイルのSHA256と一致します。ただし、これは提供された記録の確認であり、私自身の実行結果ではありません。

過去レビュー（SHA256 `61e432e28c0d33b03377d83d42a7a5453c532711b4519f2bc6e290ad76d6f0bb`、`ff8a67bcbd5099df8fb2b899bdaf90f609a32d23069386bcf3be325f78b696ba`）は旧ソースSHA・10検査版を対象としているため、現行版への承認とは扱っていません。

## 未確認事項

- テスト、Unity、batchmode、ビルドは実行していません。
- 提供された`tests.txt`には独立コンパイルとテスト成功はありますが、batchmode 0/0の生出力は含まれていません。
- `LoopModel.Tests.csproj`が未提供のため、新規検査の通常ビルドへの組込み。
- `LoopDemo.cs`、`DemoRig.cs`、`task026-diff.md`が未提供のため、最初のdt除外、Advance前判定、JSON出力、F2表示、観客非表示を現行ソースから確認できません。
- F2表示のスクリーンショット、Quest 3／Air Link実機、長時間稼働。
