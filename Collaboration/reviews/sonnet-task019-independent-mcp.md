# task019 独立レビュー（RoomVisuals.cs）

対象SHA256:
- `Assets/LoopRoom/Scripts/RoomVisuals.cs`: `94d1335e055135b192c50faca1377a772324dbb74f6d772c72f363bba0f25132`
- `Collaboration/tasks/019-one-room-apartment.md`: `7f0557e44e9008f8128f1bef9e2f577789c2e39d1ad75cfbdb1ea672d7d1eeeb`

## 指摘事項

**[Medium] 扉（Door）が既定レイヤーの要求に反してPrivateLayerに設定されている**
- 該当: `Door = new GameObject("Entry door")...; Door.gameObject.layer=PrivateLayer;` 以下、Door配下の全パーツ（Door slab / upper panel / lower panel / knob）。
- トリガー: task019設計は「家具（机、椅子、時計、窓とカーテン、扉、スイッチ、ベッド、壁掛け小物）… すべて当たり判定なし、既定レイヤー（攻略に関わらない）」と明記しているが、扉だけがPrivateLayer(8)になっており、観客カメラの`cullingMask=~(1<<PrivateLayer)`から除外され、観客には扉自体が見えなくなる。
- 実装側コメント「The moving door remains private because its state is an escape clue.」から意図的な逸脱と分かるが、これは設計文書との不一致であり、計画担当（Opus/Astra）の確認・承認なく実装者が独断で行うべき判断ではない。「普通のワンルーム」に見せる企画意図とも矛盾しうる（扉が全く見えない部屋になる）。
- 修正案: 扉自体は既定レイヤーで可視化し、攻略のヒントとなる状態（開閉・ノブの向き等）だけを別要素として分離しPrivateLayerにする。もしくは計画担当に本設計変更の是非を確認し、承認をタスク文書に記録する。

**[Low] 「残すもの」の座標・ロジック不変性をSHA256差分で直接確認できない**
- 該当: 取っ手（ShieldHandle/ExitHandle）・遮蔽板（Barrier）・出口ランプ（ExitLamp）・カード（Card）・敵（Enemy）とその経路・観客カメラ（Spectator）・暗転（Blackout）・ラグ（Reach rug）。
- トリガー: 本レビューには直前コミット（53c5c00）時点のRoomVisuals.csが提供されておらず、diffが取得できない。目視した限り座標値・命名・ロジックに不自然な変更は見当たらないが、「変わっていないこと」の断定的な保証はできない。
- 修正案不要（レビュー手続き上の制約）。実装側でdiffを添付するか、レビュー依頼時に旧版も併せて提供することを推奨。

**[Info] GameObject名の重複**
- 該当: `Detail("Window daylight", ...)`（壁の明るい面）と`new GameObject("Window daylight").AddComponent<Light>()`（フィルライト）が同名。
- 機能的影響はないが、Hierarchy上でデバッグ時に紛らわしい。

**[Info] コメントの言語不統一**
- 英語コメントが複数残存（"Small desk at the established interaction position...", "The moving door remains private...", "Open curtains and a plain bright exterior...", "Quantize hands..."）。プロジェクトの他文書は日本語中心だが、機能的問題ではない。

## 設計適合の確認（コードレベル）
- 霧オフ（`RenderSettings.fog=false`）、看板・腰板/金色手すり相当のオブジェクトなし、吊り照明→天井の丸い照明（Point Light, shadows=Soft、影を落とすのはこの1灯のみ）、Vignette未追加、彩度0（元に戻す）、Bloom intensity=.06（弱）— いずれも設計通り。
- 観客用アバターの影は`shadowCastingMode=Off`かつ`receiveShadows=false`で維持。
- 家具類（机・椅子・ベッド・窓枠・カーテン・額縁等）は`Detail()`経由でコライダー破棄・既定レイヤーとなっており、Door以外は設計通り。
- 攻略要素（取っ手・カード・時計文字・敵各部・遮蔽板）は`hidden=true`によりPrivateLayer(8)で一貫。

## 未確認事項（unverified）
- batchmodeコンパイルのエラー0・警告0
- モデルテスト全件PASS
- `Collaboration/evidence/20260924-oneroom/` の画像内容（明るさの適否、観客視点に攻略の手がかりが写っていないかの目視確認）
- Player.logの例外0
- HMD実機での見え方・到達範囲
- 直前バージョンとの座標・ロジック差分の直接比較

## 判定

**request_changes**

理由: 扉のレイヤー設定が設計文書（家具は既定レイヤー）と食い違っており、観客への見え方に影響する意図的な仕様変更が計画担当の承認を経ずに行われている疑いがある。この点の確認・是正を求める。他の項目（明るさ・色調・消すもの・守ることの大部分）はコード上設計に適合している。