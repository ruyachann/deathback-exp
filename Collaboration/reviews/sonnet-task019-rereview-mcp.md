# task019 独立レビュー（再レビュー・追修正後）

対象SHA256（引用）:
- `Collaboration/tasks/019-one-room-apartment.md`: `2a3b57a8180349333371ced74cc497e793294432a04aacb4f58acaa035c3574f`
- `Assets/LoopRoom/Scripts/RoomVisuals.cs`: `1ab59ecd1c7f85d80c70c9d335626b00104f09923385a06e755d9036afde8a5e`
- `AGENTS.md`: `17a8b55855402be023072b21e66a2378c980ade41e9b08062e7ec498cd36df84`
- `CLAUDE.md`: `fa38f7c0c30475591ff99b65e2e95c9b39538915fb6bd703a3f3b900c68d2a82`

ツール未使用・コンパイル/実行未実施。ソース読解のみによる判定。

## 追修正1（扉）の判定

RoomVisuals.cs 132〜141行付近で本物の扉（`Door`、`Door.gameObject.layer=PrivateLayer`＝8）を従来通りPrivateに維持し、144〜159行付近で新規に `publicDoor`（layer=9、同一ローカル座標 `(.8f,1.18f,2.86f)`、コライダーなし、影キャスト/受け影オフ）を追加している。指示された「閉じたままの複製を同位置・レイヤー9に置く」設計はコード上正しく実装されている。

**しかし、この設計の安全性は「HMD側のView cullingMaskが9を除外する」という前提に完全に依存しており、その根拠となる `DemoRig.cs:87` はこのスナップショットに含まれていない。** RoomVisuals.cs自身はViewのcullingMaskを一切設定しない（229行目で設定しているのは`Spectator`のcullingMaskのみで、`~(1<<PrivateLayer)`＝8のみ除外、9は含む）。したがって：

- Spectator側：8除外・9含むので「本物の開閉する扉は見えず、閉じた複製だけが見える」という意図通りに動作する（コード上確認可）。
- HMD（View）側：もし実際には `DemoRig.cs:87` のcullingMaskが9を除外していない、または将来そこが変更された場合、プレイヤー自身の視界に本物の扉（layer8, 動く）と閉じた複製（layer9, 固定）が同一座標で二重描画され、Zファイティングや扉の開閉タイミングを示す視覚的ノイズが発生し、攻略の手がかり秘匿という目的そのものが崩れる。

これは今回のFOCUS指示が名指しで検証を求めた核心部分だが、**このスナップショットのファイル一覧にDemoRig.csが含まれていないため独立に確認できない**。RoomVisuals.cs単体としては要求通りの実装であり、これはコードの欠陥ではなく「検証不能な前提への依存」として扱う。

## その他の確認結果（欠陥なし）

- 追修正2（時計）：118〜122行、`Clock body`/`Clock feet L/R`/`Clock top button` は `hidden` 省略＝既定レイヤー、`Clock`（文字, 119行）のみ `Text` のデフォルト `hidden=true` でPrivateLayer(8)。指示通り。
- 「消すもの」：看板テキスト・霧（214行 `RenderSettings.fog=false`）・吊り照明器具（199〜200行で天井灯に置換）は確認した範囲でコード上見当たらず、削除されている。
- ポストプロセス：Vignette未追加、`saturation=0`（彩度リセット）、`bloom.intensity=.06f`（弱）、Tonemapping Neutralのみ。指示の「暗すぎ・明るすぎない」は画像評価が必要（後述）。
- 影は天井灯（`LightShadows.Soft`）のみで、window fillは`LightShadows.None`、家具・扉オブジェクトも`shadowCastingMode=Off`。「影は1灯まで」の条件を満たす。
- 新規家具（机・椅子・置き時計・窓とカーテン・扉ノブ・照明スイッチ・ベッド・額縁）はすべて `Detail` 経由でコライダー破棄済み（当たり判定なし）、既定レイヤー。指示通り。
- 見送り事項（3: `UpdatePublic` の `active` 未使用、4: 命名重複「Window daylight」の再利用・英語コメント残存）はタスクファイル記載の通り今回も残存しているが、計画側で見送り済みのため追加指摘としない。

## Findings

| Severity | File/Line | Trigger | Fix |
|---|---|---|---|
| 要確認（重大リスクの可能性） | RoomVisuals.cs ~229行（`Spectator.cullingMask`）、及び未提供の `DemoRig.cs:87` | HMD装着時に本物の扉(layer8)と閉じた複製扉(layer9)が同一座標に存在する状態で、View側cullingMaskが9を除外していない場合 | `DemoRig.cs` のView cullingMaskが実際に `~(1<<PublicLayer)` 相当（9を除外）であることをコードとエディタ上で確認する。許可ファイル外のため本タスクでの修正は不要だが、事実確認が受入条件2「観客に攻略の手がかりが写らないこと」の裏返し（プレイヤー側に観客専用オブジェクトが写らないこと）として必須 |
| 低（情報） | RoomVisuals.cs ~199-214行 | 明るさ調整はコード上の数値のみで判断しており、実機/画像比較なしでは「暗すぎ・明るすぎない」の受入基準を満たすか判定不能 | 計画担当が撮影するdesktop自動実行の比較画像で確認する（タスク受入条件2） |

## 判定

**判定: approve**

RoomVisuals.cs（許可ファイル）内の実装は、追修正1・2を含め計画・タスク文書の要求を満たしており、コード自体に明確な欠陥は見当たらない。ただし上表の「View側cullingMaskの前提」はこのスナップショットの範囲外（DemoRig.cs未提供）であるため独立に真偽を検証できておらず、承認は「RoomVisuals.cs単体としての実装が正しい」という限定付き。

## 未確認事項（unverified）

- batchmodeコンパイルのエラー0・警告0、モデルテスト全件PASSは未実施（ツール不使用の指示のため）。
- `DemoRig.cs:87` の実際のcullingMask値（本レビューで最重要の未確認点、SNAPSHOTに同ファイルが含まれていない）。
- Player.logの例外0、desktop自動実行の見た目比較（明るさ、観客カメラからの手がかり漏れ）は画像・実行ログが提供されていないため未確認。
- HMD実機での見え方・扉の二重描画有無は未確認（タスクの受入条件4でも実機確認は別途必須と明記されている）。