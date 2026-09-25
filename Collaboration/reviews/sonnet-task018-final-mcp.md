# task018 独立レビュー（追修正3中心）

**対象**: `Assets/LoopRoom/Scripts/LoopDemo.cs`
**SHA256**: `27ab23bb8c5d97edc28b36f929a24320afdf5d18a9038290ef0877e41c838791`（提供SNAPSHOT記載値をそのまま引用）

## 追修正3-1（ResetAlignmentでフラグと3値を同時初期化）の検証

```csharp
void ResetAlignment() { aligned=false; alignCx=0; alignCz=0; alignAreaYaw=0; }
```
`aligned`・`alignCx`・`alignCz`・`alignAreaYaw`の4つが1メソッド内で同時に初期値へ戻されており、要求どおり実装されている。呼び出し箇所も、VR/desktop切替検出時（`Update()`冒頭付近、`rig.IsVR!=wasVR`）とR押下によるVR再準備時の2箇所で漏れなく呼ばれている。**問題なし。**

## 追修正3-2（Cの処理を開始判定より前に移動）の検証

`Update()`内の順序は以下のとおり:
1. `idle`判定・`rig.PollMode`・VR切替検出（ResetAlignment）
2. **Cキー判定（`idle && rig.CanStart`）→ align値の更新**
3. Enter/VR開始/autoTrigger判定 → `Begin()`

C処理がEnter判定より前に置かれているため、同一フレームでEnter+Cが同時に押されても、`Begin()`がPhaseを変更する前にC処理が完了する。`idle`変数はフレーム冒頭の値を使い回しているが、C処理が`Begin()`より前にあるためPhaseの陳腐化は発生せず、「開始後にCが受理される」問題は解消されている。**設計要求を満たしている。**

## 軽微な指摘（低優先度）

1. **`fitsWarned`の扱い**（LoopDemo.cs:該当フィールド宣言部および`ResetAlignment()`）: `ResetAlignment()`はVR/desktop切替やR再準備時に呼ばれるが、`fitsWarned`はリセットされない（Cキー押下時のみ`fitsWarned=false`）。切替後にCで再位置合わせしないまま次周回に入り、かつ以前`fitsWarned=true`だった場合、`fits=false`の警告が一度も出ない状態になり得る。追修正2-3の文言（「Cで再位置合わせしたら再び警告してよい」）とは矛盾しないが、切替直後の未検証状態でも警告を出したい場合は意図確認が必要。
   - 修正案: 必要なら`ResetAlignment()`内で`fitsWarned=false`も合わせてリセットする。

2. **Enter+C同時押しの挙動**（LoopDemo.cs、C判定ブロックとBegin判定ブロック）: 同一フレームでC+Enterを押すと「その場で位置合わせしてから即開始」という動作になる。要求は満たしているが、運営が意図せずC押下と同時にEnterへ触れた場合、意図しない位置での位置合わせが成立したまま開始してしまう可能性がある。仕様上は許容範囲と思われるため、参考情報として記載。

## 未確認事項

- `RoomAnchor.ChooseFrontYaw`の実装（本SNAPSHOTに含まれず、`alignCx=alignCz=alignAreaYaw=0`時の扱いを含めロジック未検証）。
- `DemoRig`の`PollMode`・`CanStart`・`IsVR`・`View`・`SimulateDesktopDrift`の実装（SNAPSHOT未提供、許可ファイルだが確認不可）。
- `RoomVisuals.Build`・ラグ追加分の実装（SNAPSHOT未提供）。
- Unity batchmodeコンパイル、モデルテスト、実機/Editor実行結果はいずれも未実施（本レビューはソース読解のみ）。
- 受入条件2の証拠画像・ログ（evidence配下）の内容は本レビュー対象外のため未確認。

## 判定

**approve**（フォーカス範囲＝追修正3の2点はLoopDemo.cs内で要求どおり実装されており、明確な欠陥は見当たらない）。ただし上記の低優先度指摘2点は任意の改善余地として申し送り、依存ファイル・Unity実行系は未検証である点を留意されたい。