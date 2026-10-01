# モデル/CIの追加

Solサブエージェントが設計したrunnerを管理側が適用。子エージェントでは移行先書込許可が共有されず拒否されたため、実際の適用を分けて記録。
実ソースのlinked Compileで既存11チェックと無操作172/180秒境界・終了後時刻/記録不変の追加確認を実行する。
CIはPython3.12管理検証と.NET8モデルを分離。contents:read、旧PR実行キャンセル、各10分上限。
ActionsのSHAは公式GitHub APIでv4/v5タグが指すcommitを取得して確認済み。推測SHAなし。
ローカル.NET8 SDKは未導入。PowerShellのAdd-Type検証と.NET SDK/CI検証は別に記録する。Unity統合/Play/Quest3は未検証。
