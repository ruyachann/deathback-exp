# task028 独立レビュー（LoopRoom）

## 検証したSHA256
- `Collaboration/tasks/028-no-fit-operator-warning.md`: `1a38c69ad7586123051b61b92ed7fd021205154078a567aa0c7bf764fb64e9af`
- `Collaboration/reviews/task028-diff.md`（対象 LoopDemo.cs のベースハッシュを内包）: `8ab8da8345f116795546f71659f968644efb4d848a5a1d4f4af3a70cdfe273bf`（差分ヘッダ記載の元LoopDemo.cs: `92c771341d58a3c52863fa9d68dad4863c26ecb6c7ae959da7752846aeeb1a91`）
- `Collaboration/evidence/20260925-no-fit/player-log-lines.txt`: `d850d05602f221bc3d543909d2054197137b067a11745bc19617a3cb736862dd`

ツールは使用せず、上記本文のみで判定した。

---

## 指摘

### 1. 【重大】証拠ログとコードの整合性が取れていない（noFitPlacements / maxNoFitDistance が None）
- 該当: `Collaboration/evidence/20260925-no-fit/player-log-lines.txt` 最終行
- 内容: `PlaceRoom ... fits=False` → 警告ログ `distance=0.65m` の後、セッション終了時の記録が `{'outcome': 'Escaped', 'noFitPlacements': None, 'maxNoFitDistance': None}` になっている。
- 差分（`task028-diff.md` の `@@ -481,7 +501,8 @@` 付近）では `SessionLog` に `noFitPlacements=noFitPlacements,maxNoFitDistance=maxNoFitDistance` を明示的に渡しており、`int`/`double` のpublicフィールドなので `JsonUtility.ToJson` は必ず数値を出力するはずで、`null`/`None` になり得ない。
- トリガー: fits=false が1回発生したセッションを `Escaped` で終了させ、保存済みJSONをパースする（このevidenceの手順そのもの）。
- 影響: 受入条件2「セッションログの `noFitPlacements`・`maxNoFitDistance`…は変わらない」ことを裏付ける証拠になっていない。むしろ、証拠取得に使ったビルドがこの差分を反映していない（旧ビルドで検証した）か、保存経路に別の問題がある可能性を示唆する。
- 修正: 差分が実際に適用されたビルド／Editorで再度セッションを走らせ、保存されたJSONの生の中身（`noFitPlacements`, `maxNoFitDistance` の数値）を証拠として添付し直すこと。原因がビルド未反映でないなら、`SaveLog` 経路のどこかでこれらのフィールドが上書きされていないか要確認。

### 2. 【中】evidenceが受入条件の「fits=falseが2回起きる」検証をカバーしていない
- 該当: `Collaboration/evidence/20260925-no-fit/player-log-lines.txt` 全体（5行のみ）
- 内容: ログ中で `fits=False` は1回しか出現しておらず（3行目）、直後の4行目で `fits=True` に復帰している。今回のレビュー対象説明では「areaSize 1.2 と `--simulate-drift` で fits=false が2回起きて、表示が毎回次の周回で消える」ことを確認する想定だが、このevidenceだけでは「2回目の遷移」や「表示が次の周回で消える」ことの反復性が確認できない。
- 修正: 2回以上の fits=false→true 遷移を含むログ（または該当箇所のスクリーンショット）を追加提出すること。

### 3. 【軽微・未確認】distance算出の基準点（px, pz）の意味論
- 該当: `task028-diff.md` `@@ -412,7 +424,15 @@` 内 `double distance=Math.Sqrt((px-alignCx)*(px-alignCx)+(pz-alignCz)*(pz-alignCz));`
- 内容: task028.md は「体験者の基準点（頭の床面投影）が中心から外れる距離」を求めているが、`px, pz` が `PlaceRoom` ログの `pos=(...)`（部屋の配置位置）由来なのか、体験者の頭位置由来なのかはこの差分だけでは判別できない。`PlaceRoom` 関数の全体（`px`, `pz` の算出元）が提供されていないため未確認。
- 修正: `PlaceRoom` 全体の実装を提示し、`px, pz` が体験者頭部由来であることを確認する必要あり。

### 4. 【軽微】`fitsWarned` は `Begin()` でリセットされず、周回をまたいだ fits=false 継続時の扱いに非対称性がある
- 該当: `task028-diff.md` `@@ -390,6 +401,7 @@`（`Begin()` は `noFitPlacements=0; maxNoFitDistance=0;` のみリセットし `fitsWarned` は据え置き）と `@@ -412,7 +424,15 @@`（`fitsWarned` によるログ抑制）
- 内容: 前の周回が fits=false のまま終了し、次の周回でも fits=false が続く場合、警告ログは（`fitsWarned` が持ち越されるため）出力されないが、`noFitPlacements` カウンタは周回ごとに0から数え直される。ログ出力は「周回をまたいだ継続」として1回扱い、カウンタは周回単位で独立、という仕様上の非対称。意図的である可能性はあるが、task説明に明記がなく確認が必要。
- 修正: 仕様として問題ないかタスク文書に一文追記するか、`Begin()` で `fitsWarned` もリセットするか、方針を明確化すること。

### 5. 【軽微】GUIStyle の一時変更が例外時に復元されないリスク
- 該当: `task028-diff.md` `@@ -510,8 +531,10 @@`
  ```
  var prevStyle=small.fontStyle; small.fontStyle=FontStyle.Bold;
  GUI.Label(...);
  small.fontStyle=prevStyle; ...
  ```
- 内容: `GUI.Label` 呼び出し自体で例外が起きる可能性は低いが、`try/finally` を使っていないため、間に例外が入れば以降のフレームで `small` スタイルが太字のまま他のラベルに影響する。実害は小さいが、共有 `GUIStyle` を書き換える手法自体は壊れやすい。
- 修正: 必須ではないが、`try/finally` にするか、専用の `GUIStyle` インスタンスを別途用意する方が安全。

---

## 未確認事項（ツール不使用のため）
- batchmode 0/0、テスト全件 PASS の実行結果そのもの（本文からは検証不能）。
- Editor/実機での GUI 表示（文言「安全な向きなし: 中央へ / C で再設定」が Rect(340px) 内で折返し・はみ出しなく収まるか、太字化での右端はみ出しが本当に解消されたか）。
- `PlaceRoom` 関数全体（`px, pz` の算出元、体験者頭部位置との対応関係）。
- `privateOverlay`/F2切替時の実際のVR表示、観客画面に警告が漏れていないことの実機確認。
- 上記指摘1・2で疑義のあるevidenceの再取得結果。

---

## 判定
**request_changes**

理由: コード差分自体（`Collaboration/reviews/task028-diff.md`）は設計方針（挙動不変・運営限定表示・遷移ごとのログ1回・セッションログへのカウンタ追加）と概ね整合しているように見えるが、提出された唯一の実行証拠（`player-log-lines.txt`）が `noFitPlacements`/`maxNoFitDistance` を `None` として示しており、受入条件2を満たす証拠になっていない。この矛盾（コードは値を書き込むはずなのに証拠ではNone）を解消しない限り、実装が実際に意図通り動いていることを確認できないため、承認を保留する。