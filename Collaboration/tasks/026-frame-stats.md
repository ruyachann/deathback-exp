# 026 — フレーム時間の記録（実機確認で負荷を数字で見る）

状態: **受入（2026-09-25、reviews/task026-exchange-20260925.md）**。計画 Claude Opus 5.5。実装 Claude Sonnet 5。レビュー 実装とは別セッションの Sonnet と GPT-5.6 Sol。

## 背景

ユーザーが Air Link で実機確認をする。task023（天井灯の影）や task025（質感）の負荷、Air Link の安定性を、感覚だけでなく数字で見られるようにする。

## 設計

1. 新規 `Assets/LoopRoom/Scripts/FrameStats.cs`（namespace LoopRoom、**UnityEngine に依存しない**純粋な C#）:
   - `FrameStats(double targetHz)`（有限・正でなければ例外）、`Add(double dtSeconds)`（非有限・負は無視して数えない）、`Reset()`。
   - 読み取り: `Frames`、`MeanMs`、`P95Ms`、`MaxMs`、`Dropped`（dt が目標の 1 フレーム時間の **1.5 倍を超えた**回数）、`TargetHz`。
   - 長いセッションでもメモリが増えないよう、全フレームを保存せず**固定の区間の度数分布**（例 0.25ms 刻みで 0〜100ms、超えたものは最後の区間）で P95 を求める。P95 は区間の上端で近似してよい（誤差は区間幅以内）。
2. `Assets/LoopRoom/Editor/FrameStatsChecks.cs`（LoopModelChecks と同じ形の `Run()`）と `Tests/LoopModel.Tests`（Program.cs、csproj）に検査を追加:
   - 一定の dt で平均・P95・最大が正しい、目標の 1.5 倍を超えた dt だけが Dropped になる、非有限・負を無視、Reset で空に戻る、目標 Hz が不正なら例外、フレーム 0 のとき各値が 0。
3. LoopDemo:
   - セッション開始（Begin）で Reset。Playing と Blackout の間だけ `Time.unscaledDeltaTime` を Add。
   - 目標 Hz: VR では XR の表示のリフレッシュレート（`XRDisplaySubsystem.TryGetDisplayRefreshRate`。取れなければ 72）、desktop では画面のリフレッシュレート（取れなければ 60）。取得は DemoRig に最小の関数を足してよい。
   - セッションログ（Sessions/*.json）に `frames` の項目を追加: `targetHz, frames, meanMs, p95Ms, maxMs, dropped`。
   - F2 の運営表示（観客には出さない）に1行: 「フレーム 平均 xx.x ms / P95 xx.x ms / 落ち n（目標 xx Hz）」。セッション中だけ更新。
   - ログの終わりに Debug.Log で1行（証拠用）。
4. `Docs/DEVICE_QUICKCHECK.md` への追記は計画担当が行う。

### 追修正（2026-09-25、独立レビューと撮影の指摘。計画担当 Opus 5.5）

Sonnet approve、Sol request_changes（`sol-task026-independent-mcp.md`）。採用:
1. （中、Sol）P95 が最後の区間（100ms 以上）に入ると最大値を返す → 100ms 以上にも有限幅の区間を足す（例 100〜1000ms を 10ms 刻み、1000ms 以上は最後の区間で上端 1000ms に飽和）。P95 は該当区間の上端を返し、最大値は返さない。
2. （中、Sol）Begin 直後の最初の dt に開始前の時間が入る（証拠の max 743ms と整合）→ セッション開始後の最初のフレームは記録しない。
3. （低、Sol）区間の判定を Advance 後の状態だけで決めている → **Advance 前**の状態が Playing/Blackout のフレームの dt を記録する（その Update の間に経過した時間はそのセッションのもの）。
4. （低、Sol）有限だが巨大な dt で添字が負になり得る → ms 換算後の有限性を確かめ、添字計算の前に時間の値で最後の区間へ飽和させる。
5. （撮影、計画担当）desktop の運営表示で「フレーム …（目標 165 Hz）」の行がパネルの右端からはみ出す（evidence/20260925-frame-stats/overlay-before-fix.png）→ 行を短くする（例「フレーム 0.9 / P95 1.5 ms・落ち 2・165Hz」）か、パネル幅を広げて収める。
6. 検査を追加: 100ms 超が 5% 以上のときの P95、1件だけ極端な値があるときの P95、巨大な有限値、1.5 倍ちょうどは Dropped にならない境界。
7. （Sonnet 低）バケット数のコメントを実装に合わせる。
見送り: セッション終了後も F2 に前回の値が残る（運営が終了後に確認できるので残す）。

### 追修正2（2026-09-25、交換後の指摘。計画担当 Opus 5.5）

Sonnet approve、Sol request_changes（`sol-task026-exchange-mcp.md`、低2件）。採用:
1. `Add(double.MaxValue)` のように ms 換算で無限大になる入力で平均・最大が非有限になる → 秒の値を先に上限（例 3600 秒）で飽和させてから ms に換算し、sum・max・平均が常に有限になるようにする。`double.MaxValue` を入れても MeanMs・MaxMs が有限である検査を足す。
2. 「1.5 倍ちょうど」の検査が境界そのものではない → 二進数で厳密に表せる組み合わせ（例: 目標のフレーム時間と 1.5 倍の値がどちらも 2 の累乗の分数で表せる Hz と dt）で、境界ちょうど・直下・直上を別々に検査する。必要なら実装側の比較を「dt(ms) > 閾値(ms)」で閾値を1回だけ計算して保持する形にし、検査と同じ式で比較されることを保証する。

### 追修正3（2026-09-25、最終レビューの指摘。計画担当 Opus 5.5）

Sonnet approve、Sol request_changes（`sol-task026-final-mcp.md`、低2件）。採用（どちらも小さい）:
1. 目標 Hz が極端に低いと 3600 秒の飽和と Dropped の判定が矛盾する → コンストラクタで目標 Hz を **1〜1000** に制限し、範囲外は例外（実際の表示は 60〜165 Hz 程度）。範囲外で例外になる検査を足す。
2. 件数とヒストグラムが int で長期稼働であふれ得る → Frames・Dropped・度数を long にする。

3. （計画担当）1 の制限で、DemoRig.GetTargetHz が 1 未満や 1000 超を返すと Begin で例外になりセッションが止まる → GetTargetHz は 1〜1000 の範囲外なら既定値（VR 72、desktop 60）を返す。

### 最終レビューの扱い（2026-09-25、計画担当 Opus 5.5）

Sonnet approve、Sol request_changes（`sol-task026-final2-mcp.md`、低1件: P95 の順位計算が double を経由し巨大な件数でずれる）。**見送り**: double が整数を正確に表せなくなるのは 2^53（約 9×10^15）フレームからで、165 Hz でも約 170 万年かかる。展示のセッションは数分なので実害はない。コードは変更せず、Sol に理由を示して再判定を求める。

## 許可ファイル

- 新規 `Assets/LoopRoom/Scripts/FrameStats.cs`、新規 `Assets/LoopRoom/Editor/FrameStatsChecks.cs`
- `Tests/LoopModel.Tests/Program.cs`、`Tests/LoopModel.Tests/LoopModel.Tests.csproj`
- `Assets/LoopRoom/Scripts/LoopDemo.cs`、`Assets/LoopRoom/Scripts/DemoRig.cs`（リフレッシュレートの取得だけ）

## 受入条件

1. batchmode 0/0、テスト全件 PASS（新しい検査を含む）。
2. 証拠: desktop の自動実行（`--autoescape` で終わらせる）のセッションログの `frames` と、F2 表示の画面（計画担当が撮影）。
3. 別セッションの Sonnet と Sol の独立レビュー、交換後に両方 approve。
