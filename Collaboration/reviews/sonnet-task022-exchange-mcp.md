# 独立レビュー（task022 — autostart時のフォーカス喪失非中断／`OnApplicationPause` 追修正）

対象スナップショットの SHA256（本レビューで使用した値）:
- `Assets/LoopRoom/Scripts/LoopDemo.cs`: `af96c2ff60f25d7573ee8164056ea55e63f2aa1579afb57e348a224d10a8c954`
- `Collaboration/tasks/022-autostart-focus.md`: `01794814a32c6dbd7d006f5f529fbc2b2497e4398fc0d22c604988245ab55be9`
- `AGENTS.md`: `17a8b55855402be023072b21e66a2378c980ade41e9b08062e7ec498cd36df84`
- `CLAUDE.md`: `fa38f7c0c30475591ff99b65e2e95c9b39538915fb6bd703a3f3b900c68d2a82`

他の独立レビュー（`sol-task022-independent-mcp.md`, `sonnet-task022-independent-mcp.md`）は読んだうえで判定を下していない。以下はコードそのものを読んで独立に確認した結果であり、両報告の結論には依拠していない。

## Findings

**重大度: 低（情報）— レビュー証跡の不整合**
- ファイル: `Collaboration/reviews/sol-task022-independent-mcp.md` / `Collaboration/reviews/sonnet-task022-independent-mcp.md`
- 内容: 両報告が引用する `LoopDemo.cs` の SHA256 は `78c61e27e48c616e9ef04b2e2570961a0e9dcf494be78b09214af47a6de672ff` であり、本スナップショットの `af96c2ff...954` と一致しない。`task022-autostart-focus.md` の引用ハッシュ（`37e4d1479bc9df0078e5d5e7cd70658b0dd50bb0494d22233ea273ed2d475b20`、sonnet版は末尾が `ea273e` を重複させた破損文字列）も同様に本スナップショットの `01794814...be9` と一致しない。
- 発生条件: これは「追修正3-1（`OnApplicationPause` に `!autostart` を加える）」が適用される**前**のコードに対する独立レビューだったことを示す（task ファイルの追修正記述と整合）。つまり、今レビューしている `af96c2ff...` 版コードは、まだどちらの独立レビューにも一致する SHA で承認されていない。
- 修正案: 今回の追修正後コード（`af96c2ff...`）に対して、実装者と別セッションの Sol・Sonnet が改めて独立レビューを行い、正しい SHA256 を引用して承認を記録する必要がある。コード自体の欠陥ではなく、受入条件3（両者 approve）の証跡が現時点のハッシュに紐づいていないというプロセス上の指摘。

**具体的なコード欠陥: 検出なし**
- `LoopDemo.cs:431` `void OnApplicationPause(bool paused) { if(paused && Model!=null && !rig.IsVR && !autostart) Model.Interrupt(); }`
  sonnet 側が旧版で指摘した「`OnApplicationPause` 経路が `!autostart` を考慮していない」問題は、本スナップショットでは解消済みであることを確認した。`autostart` が真の間はこの分岐が常に偽になり `Model.Interrupt()` は呼ばれない。
- `LoopDemo.cs:244` `if (!autostart && !rig.IsVR && !Application.isFocused && !Application.isEditor && !idle) Model.Interrupt();`
  `Update()` 側のフォーカス喪失判定も同様に `!autostart` で正しく抑止されている。
- `LoopDemo.cs:240` Esc（`Model.Interrupt()`）、`LoopDemo.cs:245-255` VR追跡喪失処理、`LoopDemo.cs:274-278` `--autoescape` 処理は、いずれも `autostart` フラグと無関係に従来どおり動作しており、要求「それ以外の中断は従来どおり」を満たす。
- `LoopDemo.cs:78-84` `autostart` は `--desktop` 併用時のみ有効（既存条件維持）、通知ログ `Debug.Log("LoopRoom: --autostart is active; ignoring focus loss.")` は `Start()` 内に1回だけ記述されており、コンポーネントのライフサイクル上1回のみ実行される。要求「ログ1行」を満たす。

以上より、`OnApplicationPause` とフォーカス喪失判定の両経路が一貫して `!autostart` で保護されており、他の中断経路（Esc・追跡喪失・`--autoescape`）への副作用も確認できない。

## 判定

**approve**（コード変更範囲に具体的な欠陥は見当たらない。ただし上記の証跡不整合は受入条件3の記録更新が必要）

## 未確認事項

- Unity batchmode のエラー・警告 `0/0`（ツール実行なしのため未確認）
- テスト全件 PASS（未確認）
- `--desktop --auto-calibrate --autostart` 実行中に別ウィンドウを前面化・最小化しても `LOOP 02` まで継続する実機証拠、およびログが実際に1行だけ出ること
- `Application.runInBackground = true` 設定下で、証拠撮影ツールの操作（前面化 vs 最小化）が実際に `OnApplicationPause(true)` を発火させるかどうかの実機確認
- 本追修正後のコード（`af96c2ff...`）に対する、実装者と別セッションの Sol・Sonnet 双方からの新たな独立 approve の記録
- `RoomVisuals.cs` の変更内容（task023 範囲、本レビューの対象外）