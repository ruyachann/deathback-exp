# 新正本のパッケージ解決エラーと再試行

2026-09-17。Unity6000.3.15f1、新正本C:/deathback/deathback-exp。

1. CodexのCLI経由で起動したEditorはPackage Manager IPC接続に失敗しコンパイル前に停止した。その試行で起動したPID30460のみ停止し、ユーザーが通常Hubから開いた。
2. 通常起動のUPM30944はcom.unity.pipelineの一時packageフォルダーからPackageCache内の解決先へのrenameでEPERM。ユーザー画面とEditor.logの同じエラーを確認。Library/PackageCacheの明示的な拒否ACLや古いUPMの残存は確認できなかった。これだけでは原因は特定できない。
3. ユーザーが一度Retry。再起動PID26120 / UPM26920でログにDone resolving packages in 2.78 seconds、Tundra build success、Assembly-CSharpとEditorの更新を確認。ユーザーも通常画面到達を確認。今回のインポート阻害は解消したが原因・再発防止策の確定ではない。
4. 旧cwdからのCLI statusは未検出だった。正本cwdでstatusを実行するとPID26120/state readyへ接続できた。正常起動後の未検出をsandbox遮断と断定しない。

キャッシュのソース変更、削除、ACL変更、セキュリティ機能の停止、Unity版や依存の更新は行っていない。Libraryと生ログは公開対象から除外。

再発時は同じEditorのRetry前後で新しいログ・PID・解決先の存在を確認する。エラーが再現する場合は保存して通常終了し、必要ならMicrosoft Process Monitorで対象PackageCacheパスのrename結果・競合ハンドルを観測する。EPERMだけから管理者起動や除外設定の追加を決めない。

参考: UnityはWindowsで同様の一時package renameの非決定的EPERMを報告している。ただし今回と同じ原因・適用可能な修正版は未確認。
- https://issuetracker.unity3d.com/issues/package-installation-fails-non-deterministically-with-errors-eperm-operation-not-permitted-when-installing-packages
- https://learn.microsoft.com/en-us/sysinternals/downloads/procmon
