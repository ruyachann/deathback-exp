# task028 独立レビュー（再レビュー：証拠差し替え後）

## 検証したSHA256
- `Collaboration/tasks/028-no-fit-operator-warning.md`: `1a38c69ad7586123051b61b92ed7fd021205154078a567aa0c7bf764fb64e9af`
- `Collaboration/reviews/task028-diff.md`（記載のベース `LoopDemo.cs`: `92c771341d58a3c52863fa9d68dad4863c26ecb6c7ae959da7752846aeeb1a91`）: `8ab8da8345f116795546f71659f968644efb4d848a5a1d4f4af3a70cdfe273bf`
- `Collaboration/evidence/20260925-no-fit/player-log-lines.txt`（新evidence）: `2a57f814c102f9a401786d09afd16d9191dd9494ddd844da1ab90cd0a74688e4`
- 参考（前回evidence、Sol/Sonnetが指摘した旧ファイル）: `d850d05602f221bc3d543909d2054197137b067a11745bc19617a3cb736862dd`

コードはtask028-diff.mdと同一SHAであり、独立レビュー時から変更なしと確認した。ツールは使用せず、本文のみで判定した。

---

## 指摘

### 1.【解消】旧evidenceのHigh指摘（noFitPlacements/maxNoFitDistanceがNone）は新evidenceで解消
- 該当: `player-log-lines.txt`「実行2」のセッションJSON全文
- 内容: 旧evidence（SHA `d850d0...`）ではSol/Sonnetが共に「セッションJSONの`noFitPlacements`/`maxNoFitDistance`がNone」と指摘していたが、新evidence（SHA `2a57f8...`）の実行2では以下の通り数値が記録されている。
  ```
  "noFitPlacements": 1,
  "maxNoFitDistance": 0.6519202473774663
  ```
- 整合性確認: 実行2ログの `fits=False` 直後の警告 `distance=0.65m` と、JSON内 `maxNoFitDistance=0.6519202473774663`（四捨五入で0.65m）が数値的に一致しており、`fits=false` が1回発生したセッションで `noFitPlacements=1` になっている点もコード（差分 `@@ -412,7 +424,15 @@`）の挙動と整合する。旧evidenceは対象差分を反映していないビルド／古いJSONを読んだものだった可能性が高く、今回で解消されたと判断できる。

### 2.【解消】fits=falseが2回発生する証跡・警告ログの遷移ごと1行の証跡が追加
- 該当: `player-log-lines.txt`「実行1」
- 内容: 30秒の実行で `fits=False` が2回（3行目 distance=0.65m、6行目 distance=0.72m）発生し、その間に `fits=True`（5行目）を挟んで警告ログが再度出力されている。これは「fits=falseに変わった瞬間ごとに1行、連続する周回では出し続けない」という差分ロジック（`if(!fits){...if(!fitsWarned){...}} else fitsWarned=false;`）と一致しており、Sol/Sonnetが指摘していた「2回起きたはずが1回しかログにない」問題は解消されている。

### 3.【軽微・未確認のまま】px, pzの意味論
- 該当: `task028-diff.md` `@@ -412,7 +424,15 @@` の `distance=Math.Sqrt((px-alignCx)^2+(pz-alignCz)^2)`
- 内容: Sonnetの指摘同様、`px, pz` が体験者頭部の床面投影由来か、`PlaceRoom` が算出した部屋配置位置由来かは、`PlaceRoom` 関数全体が提供されていないため今回も判別できない。ただし新evidenceの数値（`pos=(0.55,0.35)` → `distance=0.65m` は `sqrt(0.55²+0.35²)` と一致、`pos=(-0.60,-0.40)` → `distance=0.72m` は `sqrt(0.60²+0.40²)` と一致）から、`px,pz` は `alignCx=alignCz=0` を基準とした値であることは確認できるが、それが体験者位置と一致する設計かどうかはコード全体の確認が必要。
- 修正: 必須ではないが、次回レビューで `PlaceRoom` 全体（`px,pz` の算出元）を提示してほしい。

### 4.【軽微・仕様通りと判断】fitsWarnedが`Begin()`でリセットされない非対称性
- 該当: `task028-diff.md` `@@ -390,6 +401,7 @@`（`Begin()`は`noFitPlacements=0;maxNoFitDistance=0;`のみリセット）
- 内容: タスク文書に「fits=false に変わった瞬間ごとに1行（**連続する周回では出し続けない**）」と明記されており、周回をまたいでも`fitsWarned`を保持する現在の実装は仕様通りである。`noFitPlacements`が周回単位（Begin()ごと）でリセットされる一方、警告ログ抑制は周回をまたぐという非対称性はあるが、これはログ（1回だけ知らせる）とセッション集計（回数を数える）で目的が異なるため、バグとは言えない。修正必須ではない。

### 5.【軽微・修正不要】GUIStyleの一時変更がtry/finallyで保護されていない
- 該当: `task028-diff.md` `@@ -510,8 +531,10 @@`
- 内容: `small.fontStyle`の一時変更を`try/finally`で保護していないが、`GUI.Label`は通常例外を投げないため実害は極めて低い。必須修正ではない。

---

## 未確認事項（ツール不使用、本文のみで判定のため）
- batchmode 0/0、テスト全件PASSの実行結果そのもの。
- 「安全な向きなし: 中央へ / C で再設定」の文言が実際にRect幅340px内で太字化してもはみ出さず表示されるかの実機/スクリーンショット確認（本文にスクリーンショットの記述はあるが画像自体は本レビューに含まれていない）。
- VRのF2運営表示、観客側画面への非表示の実機確認。
- `PlaceRoom`関数全体（`px,pz`の算出元、体験者頭部位置との対応関係）。
- 警告が次の適合周回・再キャリブレーションで実際に消えることの継続的な確認（今回のログでは`fits=True`復帰は確認できるが、GUI表示の消去自体はログからは検証不能）。

---

## 判定
**approve**

理由: 前回Sol/Sonnetが共通して指摘していた重大な不備（セッションJSONの`noFitPlacements`/`maxNoFitDistance`がNone）は、新evidence（`player-log-lines.txt` SHA `2a57f8...`）で数値的整合性をもって解消されている。`fits=false`が2回発生する証跡、警告ログが遷移ごとに1行だけ出る証跡も追加された。コード自体は前回レビュー時から変更がなく（同一SHA）、ロジック上の新たな重大欠陥は見当たらない。残る軽微な指摘（px/pzの意味論、GUIStyle復元）は実害が小さいか仕様通りと判断でき、承認を妨げるものではない。ただしGUI表示の実機/スクリーンショット確認は依然として未実施であり、これは別途（実機検証フェーズで）確認すべき事項として記録する。