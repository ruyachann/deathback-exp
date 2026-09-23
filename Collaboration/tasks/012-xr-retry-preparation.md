# 012 — XR 準備の再試行（task006 B-4）

状態: 計画確定（2026-09-24）。計画 Claude Opus 5.5。実装 Claude Sonnet 5（今回は Claude 側が実装し、Sol と別セッションの Sonnet がレビュー）。

## 目的

`DemoRig.StartXR()` は Floor 準備の失敗（259/266/283行付近）やタイムアウトの後に再試行しない（2026-09-24 Sonnet 確認）。展示中に Link の接続が一時的に切れると、アプリを再起動するしかない。運営が手動で再準備できるようにする。

## 設計

- `DemoRig` に「準備が終わっていて開始できない」状態を表す `public bool CanRetryPreparation => !xrInitializing && !CanStart && !forceDesktop;` と、`public void RetryPreparation()` を追加する。再試行は、既存のサブシステムを停止・破棄してから `StartXR()` をもう一度走らせる。フラグ（floorPrepared、preparationReported、desktopFallback など）の初期化漏れがないようにする。
- `LoopDemo.Update`: Ready（または Finished）で `!rig.CanStart` のとき、運営キー **R** で `rig.RetryPreparation()`。Playing / Blackout 中は無視する（周回中の再準備はしない）。
- 案内文言（PreparationMessage）の「準備ができませんでした」に「R: 再準備（運営）」を加える。OnGUI の運営表示にも R を加える。
- 詳細（XR の停止・破棄の手順）は実装者が XR Management の API を確認して決め、報告する。

### 設計の修正（2026-09-24、計画担当 Opus 5.5。初回実装の報告を読んで判明）

初回の設計 `CanRetryPreparation => !xrInitializing && !CanStart && !forceDesktop` では、XR の準備に失敗して **desktop fallback になった状態では CanStart が true になるため R が効かない**。「起動時に Link が外れていて desktop fallback → 再接続して VR に戻したい」という最も起こりやすい場面で使えない。計画の穴なので次のように直す。
- `CanRetryPreparation => !xrInitializing && !forceDesktop && (desktopFallback || !CanStart);`
- desktop fallback 中（開始はできる）でも、運営表示（OnGUI の2行目以降）に「R: VR 再準備（運営）」を出す。長さが収まらなければ行を分ける。PreparationMessage は変えない（desktop fallback 中は表示されないため）。
- RetryPreparation の中身（自分が起動した XR だけ停止、フラグと origin・floorInputs の初期化、xrInitializing を先に立ててからコルーチン再開）は初回実装のままでよい。OnDestroy からの `StopOwnedXR()` 抽出と `origin.enabled=false` の追加も承認する。
- 受入2の Editor Play（HMD なし、desktop fallback）で、R を押すと再準備が走って再び desktop fallback に戻ることを確認できるようになる。

## 許可ファイル

- `Assets/LoopRoom/Scripts/DemoRig.cs`
- `Assets/LoopRoom/Scripts/LoopDemo.cs`

## 受入条件

1. batchmode コンパイル エラー0・警告0。
2. Editor Play（desktop、HMD なし）: 起動時の準備が desktop fallback になったあと R を押すと再準備が走り、例外なく desktop fallback に戻る。Playing 中の R は無視される。Console に新しいエラーが出ない。スクリーンショットを証拠に残す。
3. 別セッションの Sol と Sonnet の独立レビュー、交換後に両方 approve。
4. 実機（Link 切断→再接続→R で再準備）は計画6で確認（未確認と明記）。
