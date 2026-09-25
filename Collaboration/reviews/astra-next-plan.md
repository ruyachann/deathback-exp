# Astra 最終所見と次計画

確認日: 2026-09-17 JST。対象 run: `20260917-005011-1c17bc9f`。

## 判定

001 の manifest 更新は適用済み。Unity 統合コンパイルと受入は未完了のままとする。現時点で 1.17.0 に対する新しいコンパイル結果は得られていないため、依存解決・コンパイル検証を最優先の受入課題とする。UI操作が利用不能でユーザーのEditor状態確認待ちであることを主担当から受領したため、待機中に依存関係のない003の限定実装を進める設計を承認する。

- 現物 `Packages/manifest.json` は Input System 1.17.0。最終更新は 00:56:00。
- `Editor.log` の最終更新は 00:34:53。ログ中のエラーは `InputSystemPluginControl.cs(47,25): error CS0117: BuildTarget does not contain a definition for ReservedCFE` が3行、同じ1件の重複。これらは manifest 更新前の記録。
- ログに読み込まれた Input System は 1.12.0。`Library/PackageCache/com.unity.inputsystem@920b46832575/package.json` の version も 1.12.0。他の Input System cache は確認されなかった。
- `Packages/packages-lock.json` 最終更新は 00:03:35。
- ログは Safe Mode への遷移を記録している。現在の UI 状態・Editor 稼働状態は本確認では取得していない。

従って「更新後にも ReservedCFE が再発した」あるいは「コンパイルが成功した」のどちらとも報告してはいけない。

## 優先するUnity受入タスク（UI利用不能のため確認待ち）

**001-V: manifest 1.17.0 適用後の Unity 依存解決と統合コンパイルの検証。**

目的: 更新前のログと更新後の結果を区別し、コンパイル阻害の有無を確定する。

1. 既存の Unity Editor がこのプロジェクトを開いているか確認する。開いている間は別 Editor を同じプロジェクトで起動しない。
2. 既存 Editor の通常の Package Manager resolve / Refresh 経路を使用し、依存解決と再コンパイルを待つ。未保存編集を閉じたり上書きしたりしない。
3. 実行開始時刻と終了時刻、manifest の SHA256、解決された Input System version、新しいコンパイルログ区間、Console の状態、Safe Mode の状態を `Collaboration/reviews/` に保存する。
4. 新たな compiler error が残る場合は、最初の原因と該当ソース行を記録して Astra へ返す。別の依存更新やゲームコード修正へ範囲を広げない。

変更許可は検証報告のみ。Unity 自身による lockfile/生成物の更新は通常の依存解決として許容する。ゲームコード、Library / Temp / PackageCache の直接編集、lockfile の手編集は禁止。既に適用済みの manifest を再編集する必要はない。現在の証拠だけで追加の実装修正を正当化できないため、まずこの検証を完了させる。

合格条件: manifest の候補 SHA256 が維持され、Unity が実際に Input System 1.17.0 を解決し、今回の更新後に走った統合コンパイルの compiler error が0件であること。単に古い Console を消したことや候補JSONが妥当であることは合格証拠にならない。デスクトップ操作、Quest 3、HMD/手/グリップは別の未確認項目として残す。

## Sol に今渡す単一の限定実装タスク（承認済み）

**003: 非有限 timing 値をモデル入口で拒否する。** 001-VのUI確認待ち中に独立して実施してよい。既存 `Collaboration/tasks/003-finite-timings.md` の設計範囲を確認し、現物 `LoopRules.Validate()` の NaN 比較抜けをソース上で再確認した。

- 変更許可: `Assets/LoopRoom/Scripts/LoopModel.cs` と `Assets/LoopRoom/Editor/LoopModelChecks.cs` の2ファイルだけ。
- Validate の先頭で全7値 `firstShot, searchShot, exitOpens, exitCloses, blackout, playLimit, endingLength` の NaN / ±Infinity を拒否する。既存の `double.IsNaN` / `double.IsInfinity` を使用し、新依存は追加しない。
- 大小関係、既定値、イベント、状態遷移は維持する。21ケースは毎回新しいRulesを生成し、引数例外での拒否を既存形式の1チェックにまとめる。
- 受入証拠: 既存10チェック＋追加1チェックの独立.NET試験、無操作モデルのTotalTime=180かつFinished/TimedOut、2ファイルの候補SHA256、変更範囲の差分。
- Sol と Sonnet が同一候補を独立レビューし、報告交換・必要な簡易修正後に Astra が最終報告と次計画を確認する。旧タスクに残る Astra/Claude 独立レビュー指定は、新ユーザー指示の Sol/Sonnet 独立レビューと Astra 最終確認へ読み替える。
- Unity統合試験は未確認、001受入は保留、002は未着手、003は実装済みであっても統合完了とは扱わない。

003の2ファイルは002のDemoRig/LoopDemoと重ならない。今回のAstra調査ではゲームコードを編集していない。

## 過去レビューの hash 指摘の再検証

Python hashlib と automation.py に定義された同じシリアライズ方式で再計算した。証拠は `astra-next-plan-hashes.json`。

| 対象 | 再計算結果 |
|---|---|
| baseline 集合 | `3078d77f331363dadef21182dea6b5de0e1b3b3b5d8555f8e3af3bf4e694edbd` |
| 元 manifest バイト列 | `a984627e25bad0b69f9eb184bac1ec5134b60f46d3d501d6c89708e2bf60ce62` |
| design | `318f5b39f2ebd07e7bc65f4122665dc06a29afe8839b316850dbf6e8961ab956` |
| candidate-2 と現物 manifest | `b8714962dc551ad6ce87e03b0f1bca69797d1e3dbed60758a90fc276018364c4` |

全て記録された期待値と一致した。baseline_sha256 は **64文字**であり、reviews-2 の「62桁」という指摘は誤り。baseline_sha256 はファイルパスからファイルhashへの辞書を `json.dumps(..., ensure_ascii=False, sort_keys=True).encode('utf-8')` で符号化した集合hash。base_sha256 は元 manifest の生バイト列hashであり、異なることが正常である。

`manifest.before.json` は baseline.json が記録した元manifest hashと一致し、candidate-2 は現在のmanifestと一致した。再検証時点では baseline 対象ファイルのうち変化したものは manifest のみで、変化後hashも候補と一致する。過去の「hash不一致の理由不明」「62桁」の懸念は今回の再計算で解消した。過去のレビュー原本は監査のため書き換えていない。

## 管理スクリプトへの示唆

新役割は Sonnet 5 / GPT-5.6 Sol が実装・独立レビュー・簡易修正、GPT-6 Astra が最終報告確認と次計画。Astra 最終 gate には、各hashの意味と実測長さ・照合結果、候補一致、Sonnet/Solの同一候補への報告、残る未確認項目を構造化して渡すと、今回のような文字数の目視誤認を防げる。

最終 gate は静的変更承認とUnity受入の状態を分ける。今回のように Unity が未実行なら `applied_pending_unity_validation` を維持する。hashが合うことだけでゲーム動作確認済みにしない。
