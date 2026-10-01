## Findings

1. **High — `--autostart` / `--autoescape` が明示的な desktop 起動に限定されていない**

   - 場所: `Assets/LoopRoom/Scripts/LoopDemo.cs:45-46, 131-132, 167-171`
   - SHA256: `2a85b749348d7cc179bd8602982a52c7bff1f5cc972f086dc6a6c02f9295c516`
   - 要件: `Collaboration/tasks/016-device-check-readiness.md:11-13`
   - SHA256: `a3006e4adda7d39d7928ee2ec91718ac0936d9818e50af3dcae1291f54754a43`
   - トリガー: `--desktop` なしで `--autostart --autoescape` を指定し、XR設定不在・ローダー初期化失敗などで `DemoRig.desktopFallback=true` になった場合。`rig.IsVR` が false、`CanStart` が trueになるため、自動的に `Begin()` が呼ばれる。
   - 影響: 「VRでは無効」「HMD装着前に始まらない」という要件を満たさない。XR復旧前に意図せずセッションが開始され、`--autoescape` まで実行され得る。
   - 修正: `--desktop` の明示指定を条件に含める。例として、`DemoRig` から明示的な強制desktop状態を公開するか、`LoopDemo` 側でも `--desktop` を読み、`autostart = desktopArg && hasAutostart`、`autoescape = desktopArg && autostart && hasAutoescape` とする。
   - 補足: `autostartUsed` による1回制限と、既存Enter/R分岐を直接潰していない点は静的には確認できる。

2. **Medium — Link再接続手順のトリガーとR操作が実装・詳細手順書に一致していない**

   - 場所: `Docs/DEVICE_QUICKCHECK.md:38, 59-61`
   - SHA256: `2f30798e303fabb1ab3f09a9b037d8565f2a33c4bad963d74c4447e755cfae84`
   - 参照実装: `Assets/LoopRoom/Scripts/DemoRig.cs:20-27`、`RetryPreparation()`、`StartXR()`
   - SHA256: `793caa1e50c3efd52d48f278cc8043598dddb7d68833d395e2e885b737730bbc`
   - 参照手順: `Docs/UNITY_ACCEPTANCE.md:23-29, 82-92`
   - SHA256: `b1b1f081ae69374df6eafabccabc8d90b571d03fbfb26d1747bcc4f5cbd7e9e1`
   - トリガー:
     - HMDを外しても、実装が見る `<XRHMD>/isTracked` と位置・回転追跡状態が必ず失われるとは限らない。
     - 接続が自動復旧して `CanStart=true` になれば `CanRetryPreparation=false` となり、Rは何もしない。
     - 詳細手順書の準備失敗説明は再起動を案内しており、R再準備を受入手順として定義していない。
   - 影響: 同じ状態でも担当者によって結果判定が変わり、R経路を実際に検証できない可能性がある。
   - 修正: 「`HeadTracked` または `RuntimePresent()` がfalseになる操作」を明記する。Rは再準備案内が表示されている場合だけ押す手順とし、自動復旧の場合を別分岐にする。`UNITY_ACCEPTANCE.md` にR再準備の期待状態と証拠を追加して整合させる。

3. **Medium — クイックチェックの章参照と確認基準が不正確**

   - 場所: `Docs/DEVICE_QUICKCHECK.md:22-30, 32-48`
   - SHA256: `2f30798e303fabb1ab3f09a9b037d8565f2a33c4bad963d74c4447e755cfae84`
   - 参照先: `Docs/UNITY_ACCEPTANCE.md:60-107`
   - SHA256: `b1b1f081ae69374df6eafabccabc8d90b571d03fbfb26d1747bcc4f5cbd7e9e1`
   - 具体例:
     - 「遊ぶ → 第4・5章」のうち、足音、遮蔽、脱出、終了後の案内復帰は第4・5章で受入基準が定義されていない。
     - 「運営まわり → 第6・7章」のウィンドウ切替継続やR再準備も、該当章に対応する手順・合否基準がない。
     - 「見え方と聞こえ方 → 第2章 手順5」はQuest 3Sの視認性だけを扱っており、明るさ、ブルーム、カクつき、音量バランスを扱っていない。
     - `DEVICE_QUICKCHECK.md:37` はF2切替と観客秘匿を同じ項目にしているが、`UNITY_ACCEPTANCE.md:107` はF2有効時に攻略キーと経過時間が漏れると明記している。F2を再度オフにする確認がない。
   - 影響: クイックチェックから詳細な合否基準へ追跡できず、観客表示について誤った合格判定をし得る。
   - 修正: 各行に正確な章・手順番号を個別記載するか、不足する音・遮蔽・脱出・フォーカス切替・R再準備の受入節を追加する。F2は「運営表示ONの確認→必ずOFFへ戻す→観客用画面を撮影」と分離する。

4. **Medium — ログフォルダを開くExplorer引数がWindows PowerShell 5.1で安全に引用されていない**

   - 場所: `Start-DeviceCheck.ps1:17`
   - SHA256: `fe3e795893136a04430febf8de11a181dc7420365f61b071b30e70227b3ecfb1`
   - トリガー: ログパスには固定で `The Room Before` という空白が含まれる。`Start-Process explorer.exe (Join-Path ...)` は、PowerShell 5.1で引数をコマンドライン文字列に変換する際、空白を含むパスが分割される可能性がある。
   - 影響: Sessionsフォルダではなく既定のExplorer画面が開く、または無効な複数引数として解釈される。
   - 修正: `Invoke-Item -LiteralPath $sessionsDir` を使うか、Explorerへ渡すパスを明示的に二重引用符で囲む。
   - 追加の軽微な問題: `Start-DeviceCheck.ps1:13` はGitがPATHにないと、`$ErrorActionPreference='Stop'` によりビルド起動前に停止し得る。Git情報は任意取得とし、取得不能時は `commit=unknown` として続行するのが安全。

## Verdict

`request_changes`

自動開始が明示的な`--desktop`に限定されていない点は、task016の主要な安全要件に直接反するため、現状では承認できない。

## 未確認事項

テストやコマンドは実行していない。以下は未確認。

- Unityコンパイル、batchmodeビルド、警告数
- Windows PowerShell 5.1での実行結果と実際の引数伝達
- `LoopModel`／`LoopRules`が未提示のため、t=3の足音、t=6の死亡、出口解放時刻、周回2の自動脱出
- `RoomVisuals`が未提示のため、観客画面で取っ手・ランプ・時計・カードが実際に秘匿されるか
- Quest 3 / 3SでのA/X入力、Floor、頭・両手追跡、0.3秒超の追跡喪失
- Link再接続後の自動復旧とR再準備の実挙動
- 画面切替中の継続、F2表示、実際のセッションログ保存先
- スクリーンショット、セッションログ、180秒超継続を含む証拠一式

確認対象の残りのSHA256は、`AGENTS.md`=`c7a53997e17be6079c1f3c8f675fbca3ebf74757f14665036a7b019b4656f731`、`CLAUDE.md`=`fa38f7c0c30475591ff99b65e2e95c9b39538915fb6bd703a3f3b900c68d2a82`。
