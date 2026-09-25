## Findings

### Low — `long` 化後も P95 順位計算が `double` を経由し、巨大件数で順位がずれる

- ファイル: `Assets/LoopRoom/Scripts/FrameStats.cs:79`
- SHA256: `59174398010fed949be68cf94145e85a41ac320d6203db248b89a692bb036ce8`
- 該当コード: `Math.Ceiling(Frames * 0.95)`
- トリガー:
  - `Frames >= 2^53` の領域。
  - 例として `Frames = 9,007,199,254,740,992` では、正しい95%順位は `Frames - Frames / 20 = 8,556,839,292,003,943`。
  - `Frames` を `double` に変換して `0.95` を掛ける現在の式は丸めによって1件小さい順位になり得る。バケット境界がこの1件に重なると、P95が直前のバケット上端を返す。
- 影響:
  - 実時間では極端に長い運転でのみ発生するため重要度は Low。
  - ただし追修正3で件数・度数を `long` にした範囲内の正確性としては未完了で、バケット幅以内というP95誤差保証を理論上破る。
- 修正:
  - オーバーフローも浮動小数点化も避け、次の整数式にする。
  - `long target = Frames - Frames / 20;`
  - 必要なら順位計算を小さなヘルパーに分離し、`2^53` のケースを直接検査する。

`FrameStatsChecks.cs`（SHA256 `aa24ea083167cb1d19bd1c3132b3e9d1dc79ada3b8ba1a2d06dd45ecd0f32f2c`）の12検査と `Program.cs`（SHA256 `cfe180a6bd5b8721a9dd3f4315c08d09d100a45861eb6c55d61bbe24e5b02d28`）の件数指定は整合していますが、この長件数の順位計算は検査されていません。

## 判定

**request_changes**

上記は到達頻度こそ極端に低いものの、追修正3の「件数・度数を `long` にする」という変更の内部整合性に関する具体的な欠陥です。修正は一行で済み、修正後は approve 相当です。

独立判定を確定した後に旧レビューを照合しました。旧Sol報告（SHA256 `26bddba81a2fa3c2d19e8c3b9dd80947becda042db93fd32b08f579af652536b`）にも整数順位式の提案がありますが、現行ソースでは件数の `long` 化だけが反映され、順位式は残っています。旧Sonnet報告（SHA256 `f21b7995682298af78c604e9cab45f90d3277524ad3ecaf2731de38dd356e6da`）は旧SHAを対象としており、現行版への承認にはなりません。

## 確認範囲と未検証事項

対象タスクは SHA256 `b2c0ab8804d95dacfd7fa522bf8a8a8a4cdcffab4b29ec5380cf2d65c7f4ddd0`。手順資料は `AGENTS.md` SHA256 `17a8b55855402be023072b21e66a2378c980ade41e9b08062e7ec498cd36df84`、`CLAUDE.md` SHA256 `fa38f7c0c30475591ff99b65e2e95c9b39538915fb6bd703a3f3b900c68d2a82` です。

- テスト、コンパイル、Unity、batchmode、ビルドは実行していません。
- 証拠テキスト（SHA256 `ba88bc610a296850d29061f387a266123d4be417ca934ef3d62f494ed7ae1c25`）には `20+12+12 PASS` と `csc exit=0` が記載されていますが、提供記録の確認にすぎません。batchmode 0/0の生出力は含まれていません。
- `LoopModel.Tests.csproj` が未提供のため、検査ソースの実際の組込みは未確認です。
- `LoopDemo.cs` と `DemoRig.cs` は完全なソースではなく、差分資料（SHA256 `5b36894142c9e74430e13d85bb77adaeba0fda16593800ba313a1729892c1c9a`）のみです。資料記載の対象SHAはそれぞれ `9b056c077d44d7356f58dc6e31bdf5b7c241d821c5036d851975d861f4006b67`、`425ae38fe0e5f09bbed74072d4d5f42e3406aab9898848b1683993a6ed9eb618` ですが、完全な文脈および実ファイルとの一致は未確認です。
- F2表示がパネル内に収まること、観客側に表示されないこと、Quest 3／Air Linkでのリフレッシュレート取得は未確認です。
- 現行SHAを対象としたSonnet・Sol双方の最終 approve は、 supplied snapshotだけでは成立を確認できません。
