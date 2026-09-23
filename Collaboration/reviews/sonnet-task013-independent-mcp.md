# task013 独立レビュー

対象: `Docs/UNITY_ACCEPTANCE.md`（SHA256: `e69867a955abb3237cdac7971edbdbc83e026eb41ddeed4ec0939bb18016d3d6`）、`Collaboration/tasks/013-acceptance-doc-time-limit.md`（SHA256: `5193a9f54c12502f3c89d1335079086cfa705e85be1c50e472dfdaaec20f8df7`）、参照: `AGENTS.md`（SHA256: `c7a53997e17be6079c1f3c8f675fbca3ebf74757f14665036a7b019b4656f731`）、`Collaboration/tasks/007-remove-session-time-limit.md`（SHA256: `b68f8be11b02b8642387c07d5b881ec59d594044396390de82390bfa8c4b6fc2`）、`Collaboration/evidence/20260924-editor-play/README.md`（SHA256: `ac386658666c482e227c73dcc377dfcd97bc1ff142a0437e65e1c23f00afdc23`）。

## 指摘

**1. 重要度: 中 — `Docs/UNITY_ACCEPTANCE.md` 第4章「Play の3回開始/停止」手順2**
記述: 「Ready → 開始 → セッション進行（脱出またはタイムアウトまたは中断のいずれか）→ 終了までを1サイクルとし…」
発生条件: 本書の既定前提（0節・8節）は `LoopRules.enforcePlayLimit=false` であり、task007の設計どおりこの既定構成では `TimedOut` に遷移し得ない。にもかかわらず第4章はタイムアウトを通常の終了パターンの一つとして併記しており、8節で新たに定めた「`TimedOut` が出ないことが合格基準」という記述と矛盾する。実施者がこの章を読んで「タイムアウトも正常な終了例」と誤解し、`enforcePlayLimit=true` の特殊構成を意図せず使う、または既定構成でタイムアウトが起きないことを見落とすおそれがある。
修正案: 「脱出または中断のいずれか」に修正し、タイムアウトを扱う場合は「（`enforcePlayLimit=true` の参考構成の場合のみ）」等の限定を付す。

**2. 重要度: 低 — 証拠（受入条件3）の確認不能**
`git status` には `Collaboration/evidence/20260924-task013/` が新規ディレクトリとして存在するが、その中身（変更前後の差分画像）は本レビューに供給されたスナップショットに含まれていない。受入条件3「証拠: 変更前後の該当箇所の差分画像」が満たされているかは今回の情報だけでは判定できない。

**3. 重要度: 低 — 第1章（準備ゲート）の据え置き確認が不可**
FOCUSで問われている「段階1で承認済みのF1/F2記述（文言分岐の注記、遅延表示の注記）が変わっていないか」は、変更前バージョンの本文が提供されていないため、今回のスナップショット（変更後の全文のみ）と突き合わせて機械的に確認することができない。第1章の本文自体（`xrInitializing`/`FloorReady`/`HeadTracked`/`RuntimePresent()` に関する分岐説明）は内容として矛盾は見当たらないが、「変更されていないこと」の照合は不可。

**4. 重要度: 情報 — 9節のセッションログ記載項目に `enforcePlayLimit` が明記されていない**
task007の設計（`Collaboration/tasks/007-remove-session-time-limit.md`）では `SessionLog.timings` に `enforcePlayLimit` が加わるとされているが、`Docs/UNITY_ACCEPTANCE.md` 9節のログ内容説明は「`sessionId`, `mode`, `outcome`, `elapsed`, `timings`, `events`」とだけ記載し、`enforcePlayLimit` に個別言及していない。必須ではないが、8節の合否基準（`TimedOut` が出ないこと）の裏付けとして `enforcePlayLimit` の値も記録項目に明示した方が受入記録との対応が明確になる。

## 良好点（設計との一致確認）

- 0節・8節とも180秒を合否基準とする記述は撤廃され、8節の合否基準は「Escaped/Interrupted で終了し `TimedOut` が出ないこと」に一本化されている。参考手順（`enforcePlayLimit=true` 時の172+8秒確認）は「既定では実施しない」と明記した上で残されており、task013設計・受入条件1と一致する。
- Quest 3 / Quest 3S 併記、Touch Plus の記載、2節手順5の3S視認性確認（視野角・解像度・レンズ差、案内パネル文字の判読性）、期待証拠・不合格基準への反映は task013 の設計と整合している。
- 案内パネルのはみ出し（`evidence/20260924-editor-play`）を「VRでの見え方は別途確認が必要」という扱いにとどめ、desktop実測結果をHMD側の不合格と即断していない点は妥当な設計判断。
- AGENTS.md の「体験の不変条件」がQuest 3のみで3S未反映のままである点は、UNITY_ACCEPTANCE.md 0節に「機種確定後に本書とAGENTS.md/STAGE_PLANを更新する」と明記されており、矛盾ではなく設計上の意図した未実施として扱われている。

## 判定

**request_changes**

理由: 指摘1（第4章のタイムアウト記述）は8節で新設した合否基準と直接矛盾し、受入条件1「180秒を合否基準にする記述が残っていない」の趣旨（タイムアウトを既定の正常終了として扱わない）に反する実質的な欠陥であるため。

## 未確認事項

- `Collaboration/evidence/20260924-task013/` の中身（差分画像）— 本レビューのスナップショットに含まれず未確認。
- 変更前の `Docs/UNITY_ACCEPTANCE.md`（特に第1章）との行単位diff — 変更前バージョン未供給のため未確認。
- 各ファイルのSHA256値がリポジトリ実体と一致するかの独自再計算は未実施（提供値をそのまま引用）。
- Unityでの実機/Editor動作、`dotnet` テスト実行結果は本レビューでは未実施（指示により実施不可）。