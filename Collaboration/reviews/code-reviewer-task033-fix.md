# code-reviewer（claude-sonnet-5-5）task033 追修正の再レビュー

# task033 追修正の再レビュー結果

**判定: approve。** P2 は解消しており、テストの修正は意図を変えていません。Opus 判断が必要な論点はありません。

## 対象ファイルと SHA256
SHA256 は自分では計算できませんでした。Bash は無効で、desktop-commander の `start_process` も権限未付与でした。下の値は依頼文の記載で、検証していません。

| ファイル | SHA256（依頼文の記載、未検証） |
|---|---|
| `Assets/LoopRoom/Scripts/LoopModel.cs` | 63bb1433f9ac88eb85d5037aa60e00cd7e67966926c2b02d68d563bfe8598083 |
| `Assets/LoopRoom/Editor/LoopModelChecks.cs` | b9b642d6d5266089aca6a73fd53b531ea3280a5b4467164c3d2694f5dbbaa6b9 |
| `Tests/LoopModel.Tests/Program.cs` | 記載なし |

## 前回指摘の解消

- **P2（出口が窓の狙撃を飛ばせる）: 解消。**
  - `LoopModel.cs:45` に `exitOpens < windowShot - IntervalTolerance` の拒否が入りました。これで `exitOpens=6.5, exitCloses=7` のような Rules は `Validate()` で弾かれます。
  - 開錠音（`LoopDemo.cs:364`）は `ExitOpens` 基準です。常に窓の狙撃以降になるので、ランプや出口の時刻との不整合も出ません。
  - `exitOpens == windowShot` は許可されます。この場合、窓の処理は同じスライスの終端で先に行われます。
  - `windowShot - firstShot` と `searchShot - windowShot` の下限は元から検査されています。
- **P3（`Kill(id, null)`）: 解消。**
  - `LoopModel.cs:137` の `cause = cause ?? "unknown"` で、記録と連続回数の両方が "unknown" として扱われます。

## テスト修正が意図を変えていないか

- **最小間隔の境界（有効側）**
  - `exitOpens` を `MinInterval`、6.0、6.5 から、それぞれ窓の時刻（0.1、6.05）と 11.95 に移しただけです。
  - どれも「間隔ちょうど MinInterval は有効」という境界の検査のままです。
  - 浮動小数点では `0.15-0.1` が約 0.04999…ですが、`1e-9` の許容で通ります。
- **最小間隔の境界（無効側）**
  - 「exit interval」の項目（`exitOpens=11.9`）は、窓以降に収まる値です。出口の間隔だけを単独で検査する形が保たれています。
  - 「search interval」の項目は、実際には `searchShot-windowShot < MinInterval` を検査しています。前回指摘のとおり名前と実体が少しずれますが、意図（MinInterval 未満の間隔を拒否する）は保たれています。
  - `windowShot` の 2 つの下限違反は、専用テスト（`windowShot just after firstShot` など）が単独で押さえています。
- **極小値と MaxStep の境界**: `exitOpens` を窓の時刻に移しただけで、検査の目的は同じです。
- **新規テスト（`exitOpens` が窓より前の拒否）**
  - 8.99、6.5/7.0、窓の前に収まる出口、窓の半間隔前、の 4 件があります。
  - 受理側は `exitOpens=9.0`（境界ちょうど）です。
  - `new LoopModel(rules)` 経由で拒否を確認しており、`Validate` の呼び出しも含みます。
- **新規テスト（`Kill(null)`）**
  - "unknown" の記録と、"unknown" が連続したときの連続回数の増加を確認します。
  - 続けて名前付きの死因で 1 に戻ることも確認します。
- **件数**
  - `LoopModelChecks` は 29 件です。元の 19 件に 027 の 8 件と今回の 2 件を足した数で、一致します。
  - `Program.cs` の期待値 29 は正しく、締めの 1 件（180 秒の検査）を足して「30 model checks」になります。

## 実行した検証
- ソース 3 ファイルの通読と、境界値（浮動小数点を含む）の机上追跡。
- 既存の検証記録 `model-run.txt` の確認: `csc exit=0`、テスト 30 件相当の PASS、`test exit=0`。
- 同じ記録のセッションログも確認しました。2 周目で `shield_raised` と `blinds_closed`（t=1）、`shot_blocked`（6）、`window_blocked`（9）、`Escaped`（9.5）の順に並び、因果順は保たれています。
- シーンの値（`windowShot: 9`、`exitOpens: 9.5`）は Assets 内の他の場所に旧値が残っていません。

## 体験の不変条件
- 因果順、死亡処理の一意性、旧周回の入力の拒否は、今回の変更で動いていません。
- `Validate` の拒否が増えただけで、`Advance` や `Kill` の経路は触れていません。
- 観客表示への影響はありません。

## 残る P3
- `exitOpens` が `windowShot` より 1e-9 以内で手前の Rules は、許容誤差のせいで通ります。
- そのうえ、Advance の途中でちょうどその 1e-9 の幅に LoopTime が止まった場合だけ、窓の前に `ExitAvailable` が true になり得ます。
- 実際には起きないので、対応は不要です。
- 厳密にするなら `ExitAvailable` に `windowResolved` を足す手があります。

## 確認できなかった事項
- SHA256 の検証。
- 私自身によるテストの実行。
- Unity のコンパイル。
- VR と実機の挙動。
- 小さな刻みでの 9.5 付近の境界（前回の P3 補足のまま、追加テストはありません）。

ファイルは変更していません。