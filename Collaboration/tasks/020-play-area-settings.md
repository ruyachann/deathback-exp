# 020 — 体験空間の設定ファイルと RoomAnchor の設定化（キャリブレーションの土台）

状態: 計画確定（2026-09-24）。計画 Claude Opus 5.5。実装 GPT-5.6 Sol。レビュー 実装とは別セッションの Sol と Claude Sonnet 5。task021（キャリブレーション画面、Sonnet）と並行。

## 目的（ユーザー指定 2026-09-24）

「部屋の間取り（キャリブレーション）を行うシステム。**事前に設定でき**、VR ゴーグルをつけると下方に必要な広さ（自分を初期位置として）が見え、決定するとそこが体験の空間としてキャリブレーションされる」。本タスクは「事前に設定できる」部分と、計算がその設定を使うようにする部分。

## 設計

- 新規 `Assets/LoopRoom/Scripts/PlayAreaSettings.cs`（namespace LoopRoom、`[Serializable]`、UnityEngine の JsonUtility で読み書きできる）:
  - `double areaSize = 1.6`（体験に使う正方形の一辺 m）、`double margin = 0.10`、`double reach = 0.45`、`double halfAngleDeg = 45`、`double searchStepDeg = 15`。
  - `public void Validate()`: 有限値、`0.5 <= areaSize <= 4`、`0 <= margin < areaSize/2`、`0.1 <= reach`、`5 <= halfAngleDeg <= 90`、`1 <= searchStepDeg <= 45`。範囲外は ArgumentException。
  - `public static PlayAreaSettings LoadOrCreate(string path)`（UnityEngine 依存可、この関数だけ）: ファイルがあれば読み込んで Validate、無ければ既定値で作ってファイルに書き出す。読めない・不正なら既定値を使い、警告を1回出す（ファイルは上書きしない）。
- `RoomAnchor`（task017）: 既存の定数と既存のメソッドは**そのまま残す**（既存テストのため）。設定を受け取る**オーバーロード**を追加する:
  - `ForwardRegionFits(double px, double pz, double yawDeg, double cx, double cz, double areaYawDeg, PlayAreaSettings s)`
  - `ChooseFrontYaw(double px, double pz, double headYawDeg, double cx, double cz, double areaYawDeg, PlayAreaSettings s, out bool fits)`
  - 既存の定数版は、既定値の設定で新しいオーバーロードを呼ぶ形にして、結果が変わらないこと。
  - RoomAnchor.cs 自体は UnityEngine に依存しないこと（PlayAreaSettings の純粋部分だけを使う。LoadOrCreate は別ファイルに分けてもよい）。
- 設定ファイルの場所: `Application.persistentDataPath/play-area.json`（運営が編集する）。`Docs/DEVICE_QUICKCHECK.md` への追記は計画担当が行う。

## テスト（RoomAnchorChecks に追加、dotnet でも回る）

1. 既定の設定のオーバーロードが、定数版と同じ結果になる（複数の位置・向きで）。
2. areaSize を 2.4 にすると、1.6 では補正が必要だった位置で補正なしになる。
3. Validate が範囲外・非有限を拒否する。
csproj に PlayAreaSettings.cs の純粋部分を含める（LoadOrCreate を別ファイルにした場合はそのファイルは含めない）。

## 許可ファイル

- `Assets/LoopRoom/Scripts/PlayAreaSettings.cs`（新規、必要なら `PlayAreaSettingsFile.cs` として LoadOrCreate を分ける）
- `Assets/LoopRoom/Scripts/RoomAnchor.cs`、`Assets/LoopRoom/Editor/RoomAnchorChecks.cs`
- `Tests/LoopModel.Tests/LoopModel.Tests.csproj`、`Tests/LoopModel.Tests/Program.cs`

## 受入条件

1. テスト全件 PASS（Unity 付属 Roslyn と CI）、batchmode 0/0。
2. 別セッションの Sol と Sonnet の独立レビュー、交換後に両方 approve。
3. 証拠: テスト出力の画像。
