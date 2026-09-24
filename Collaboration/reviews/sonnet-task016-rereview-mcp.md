# task016 再レビュー（独立レビュー）

対象SHA256（提供スナップショットより引用）:
- `Assets/LoopRoom/Scripts/LoopDemo.cs`: `540eddf28a19f69b34e572070014a8e7faccc3d1293dbcf2d7a13e9cbf1f077b`
- `Docs/DEVICE_QUICKCHECK.md`: `4135766ea60a46e4f7e9b7cdb735c08005bcb7543483282d689bcbaf925c61b7`
- `Start-DeviceCheck.ps1`: `fee188babb3741d7890689918d9459ab1ca41ab126336a860d8acd39841eae7a`
- `Docs/UNITY_ACCEPTANCE.md`: `b1b1f081ae69374df6eafabccabc8d90b571d03fbfb26d1747bcc4f5cbd7e9e1`
- `Collaboration/tasks/016-device-check-readiness.md`: `305baacbb1ed6d66356dc8342db60ff001c31ba8c0d4c4c70af9587ad58da0c0`

---

## (a) `--autostart`/`--autoescape` の `--desktop` 依存

`LoopDemo.cs` `Start()`:

```csharp
desktopArg = Array.IndexOf(args, "--desktop") >= 0;
autostart = desktopArg && Array.IndexOf(args, "--autostart") >= 0;
autoescape = autostart && Array.IndexOf(args, "--autoescape") >= 0;
```

`autostart`自体が`desktopArg`でゲートされているため、`--desktop`を付けずに`--autostart`だけを渡した場合、desktopフォールバック（`rig.IsVR==false`）が発生しても`autostart`はfalseのまま固定される。`Update()`内の`autoTrigger = autostart && !autostartUsed && !rig.IsVR`もこの`autostart`に依存するため、「`--desktop`なしの`--autostart`で30秒待ってもセッション開始しない」という受入条件は**ソースコード上は満たされている**。task016の「追修正」指摘（Solの指摘：IsVRだけで判定すると desktop fallback でも自動開始してしまう）は本版で解消されている。

ただし `DemoRig.cs`（`rig.IsVR`／`CanStart`／desktop fallback の実装）は本スナップショットに含まれておらず、実際に「`--desktop`なし起動でXR初期化に失敗した場合に`rig.IsVR`が確実にfalseになる」保証はこのファイル単体では検証できない。

---

## (c) `Docs/DEVICE_QUICKCHECK.md` の記述

### 章参照の照合
表中の「参照」列（第1〜8章）は`UNITY_ACCEPTANCE.md`の該当章とおおむね一致している（起動/準備ゲート→第1章、床・頭・手→第2章手順2〜4、Quest 3Sのフレネルレンズ/視野端→第2章手順5の2026-09-24追記、死亡と周回→第5章、追跡中断→第6章、観客マスキングとF2→第7章）。「本書」表記の項目（足音・射撃音・チャイム、遮蔽/出口の具体操作、Escでの中断、部屋の明るさ・霧・ブルーム、音量バランス）も、`UNITY_ACCEPTANCE.md`に対応する章記述が無いことと整合しており、章参照の誤りは見当たらない。

### 指摘: #12「Linkケーブル抜き差し→Rの案内」の手順に不備（中）
- **箇所**: `Docs/DEVICE_QUICKCHECK.md` 「3. 運営まわり」#12
- **発生条件**: #11の指示どおり運営表示を**F2でOFFに戻した状態**のまま#12（Link切断→再接続）に進んだ場合。
- **問題**: `LoopDemo.cs` `OnGUI()` の該当行

  ```csharp
  bool showRetryHint=(!rig.IsVR || privateOverlay) && rig.CanRetryPreparation;
  ...
  if(rig.CanRetryPreparation) { GUI.Label(new Rect(32,y,340,26),"R: VR 再準備（運営）",small); y+=24; }
  ```
  の描画自体が `!rig.IsVR || privateOverlay` の条件でガードされている。VRモードで運営表示（`privateOverlay`）がOFFのままだと「R: VR 再準備（運営）」のラベルはPC画面に一切描画されない。したがって#11の手順どおりF2をOFFに戻した状態で#12を行うと、運営は「Rの案内が出ているかどうか」をPC画面で確認する手段がない。
- **修正案**: #12の記述に「PCの運営表示（F2）をONにして『R』の案内の有無を確認する」旨を明記する、または#11と#12の順序・F2状態の扱いを見直す。

### 補足確認（問題ではないが未検証扱いとすべき点）
- #12「つなぎ直したら：自動で開始案内に戻る場合はそのままA/Xで開始」の分岐は`rig.CanStart`／`rig.CanRetryPreparation`（`DemoRig.cs`）の挙動に依存し、本スナップショットでは検証できない。
- Start-DeviceCheck.ps1の使用例（`-Device Quest3S`、Desktopスイッチなし）はVRモード起動と一致しており、`--desktop`を付けないため(a)の自動開始ロジックはこの経路では作動しない（意図通り、実機チェックはA/X手動開始）。

---

## `Start-DeviceCheck.ps1` の確認

- Gitは任意: `Get-Command git -ErrorAction SilentlyContinue`でガードされ、無い場合`$commit='unknown'`のまま継続。`$ErrorActionPreference='Stop'`の影響を受けないよう`try/finally`でローカルに`Continue`へ退避しており妥当。
- `Invoke-Item -LiteralPath $sessions`: 事前に`New-Item -ItemType Directory -Force`しているため失敗しない。DEVICE_QUICKCHECK.mdの「セッションログのフォルダも開く」と一致。
- ビルド無し時の`throw`メッセージに`-logFile`付きのUnityコマンド例が含まれ、DEVICE_QUICKCHECK.mdの「表示されるUnityのコマンドでビルド」という案内と一致。
- 構文: `param([ValidateSet(...)]...,[switch]$Desktop)`、`Join-Path`、`Test-Path -LiteralPath`、`Get-Item -LiteralPath`、`Add-Content -Encoding utf8`、`Start-Process -ArgumentList`など、いずれもPowerShell 5.1互換の構文であり、目視上の構文エラーは無い（実行検証はしていない）。

---

## 判定

**request_changes**

理由: (a)の実装は要求どおり修正されており問題なし。(c)は章参照・全体構成は良好だが、#12の手順が「F2をOFFに戻した直後」という文脈と矛盾し、実機チェック時に運営がRの案内を確認できない手順不備がある。これは実機受入の実運用に直接影響するため修正を求める。

## 未検証事項

- `Assets/LoopRoom/Scripts/DemoRig.cs`（`IsVR`／`CanStart`／`CanRetryPreparation`／`RetryPreparation()`／`PollMode`のdesktop fallback挙動）は本スナップショットに含まれず未検証。
- `LoopModel.cs`（`Interrupt()`後のInterrupted→Finished遷移、`ExitAvailable`等）も未提供のため未検証。
- 実行・実機・ビルドによる動作確認は行っていない（静的レビューのみ、テストは未実施）。
- 他レビュー（Sol等）の報告は本レビュー確定前に参照していない。