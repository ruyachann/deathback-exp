# task029 差分（計画担当が作成）

Assets/LoopRoom/Editor/DemoSetup.cs 4fb91b575a2c4a6810bccd869e081c3039e6afc18d97af8988131a7bc807936c
Start-DeviceCheck.ps1 eba60e9849cbece469d6f05eea7c45244818125529889245df1fd2ad2128592d

```diff
diff --git a/Assets/LoopRoom/Editor/DemoSetup.cs b/Assets/LoopRoom/Editor/DemoSetup.cs
index 75034ff..36b2270 100644
--- a/Assets/LoopRoom/Editor/DemoSetup.cs
+++ b/Assets/LoopRoom/Editor/DemoSetup.cs
@@ -1,4 +1,5 @@
 using System;
+using System.Collections.Generic;
 using System.IO;
 using System.Linq;
 using UnityEngine;
@@ -17,6 +18,17 @@ using LoopRoom;
 public static class DemoSetup
 {
     const string ScenePath="Assets/LoopRoom/Scenes/LoopRoom.unity";
+    const string BuildInfoPath="Builds/Windows/build-info.json";
+
+    [Serializable]
+    class BuildInfo
+    {
+        public string commit="unknown";
+        public bool dirty;
+        public string[] dirtyFiles=Array.Empty<string>();
+        public string builtAt;
+        public string unityVersion;
+    }
 
     [MenuItem("LoopRoom/1 - Prepare project and scene")]
     public static void Prepare()
@@ -129,15 +141,71 @@ public static class DemoSetup
     [MenuItem("LoopRoom/4 - Build Windows demo")]
     public static void Build()
     {
+        var buildInfo=CaptureBuildInfo();
         Prepare(); ConfigureXR(); Validate();
         Directory.CreateDirectory("Builds/Windows");
         var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
             scenes=new[]{ScenePath},locationPathName="Builds/Windows/LoopRoom.exe",
             target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
         if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Build failed: "+report.summary.result);
+        buildInfo.builtAt=DateTimeOffset.Now.ToString("o");
+        File.WriteAllText(BuildInfoPath,JsonUtility.ToJson(buildInfo,true));
         Debug.Log("LoopRoom: Windows build completed.");
     }
 
+    static BuildInfo CaptureBuildInfo()
+    {
+        var info=new BuildInfo{unityVersion=Application.unityVersion};
+        string output;
+        bool gitStarted;
+        if(TryRunGit("rev-parse HEAD",out output,out gitStarted)) info.commit=output.Trim();
+        if(gitStarted && TryRunGit("status --porcelain=v1 -z --untracked-files=all -- Assets ProjectSettings Packages",out output,out gitStarted))
+        {
+            info.dirtyFiles=ParseDirtyFiles(output).Take(20).ToArray();
+            info.dirty=info.dirtyFiles.Length>0;
+        }
+        return info;
+    }
+
+    static bool TryRunGit(string arguments,out string output,out bool started)
+    {
+        output=string.Empty;started=false;
+        try
+        {
+            var startInfo=new System.Diagnostics.ProcessStartInfo{
+                FileName="git",Arguments=arguments,WorkingDirectory=Path.GetDirectoryName(Application.dataPath),
+                UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
+            using(var process=new System.Diagnostics.Process{StartInfo=startInfo})
+            {
+                if(!process.Start()) return false;
+                started=true;
+                output=process.StandardOutput.ReadToEnd();
+                var error=process.StandardError.ReadToEnd();
+                process.WaitForExit();
+                if(process.ExitCode==0) return true;
+                UnityEngine.Debug.LogWarning("LoopRoom: git "+arguments+" failed; build metadata will be incomplete. "+error.Trim());
+            }
+        }
+        catch(Exception exception)
+        {
+            UnityEngine.Debug.LogWarning("LoopRoom: git is unavailable; build metadata will use commit=unknown. "+exception.Message);
+        }
+        return false;
+    }
+
+    static IEnumerable<string> ParseDirtyFiles(string porcelain)
+    {
+        var fields=porcelain.Split(new[]{'\0'},StringSplitOptions.RemoveEmptyEntries);
+        for(int i=0;i<fields.Length;i++)
+        {
+            var entry=fields[i];
+            if(entry.Length<4) continue;
+            var status=entry.Substring(0,2);
+            yield return entry.Substring(3);
+            if((status.IndexOf('R')>=0 || status.IndexOf('C')>=0) && i+1<fields.Length) i++;
+        }
+    }
+
     [MenuItem("LoopRoom/Run model checks only")]
     public static void ModelChecks()
     {
diff --git a/Start-DeviceCheck.ps1 b/Start-DeviceCheck.ps1
index 57c18d9..2851ee5 100644
--- a/Start-DeviceCheck.ps1
+++ b/Start-DeviceCheck.ps1
@@ -1,4 +1,4 @@
-param([ValidateSet('Quest3','Quest3S','Unknown')][string]$Device = 'Unknown', [switch]$Desktop)
+﻿param([ValidateSet('Quest3','Quest3S','Unknown')][string]$Device = 'Unknown', [switch]$Desktop)
 # Start the Windows build for a Quest 3 / 3S device check (Docs/DEVICE_QUICKCHECK.md).
 # If scripts are blocked: powershell -ExecutionPolicy Bypass -File .\Start-DeviceCheck.ps1 -Device Quest3S
 $ErrorActionPreference = 'Stop'
@@ -11,16 +11,41 @@ $sessions = Join-Path $env:USERPROFILE 'AppData\LocalLow\LoopRoomDemo\The Room B
 New-Item -ItemType Directory -Force $sessions | Out-Null
 $note = Join-Path $PSScriptRoot "Collaboration/evidence/$(Get-Date -Format yyyyMMdd)-device-check"
 New-Item -ItemType Directory -Force $note | Out-Null
+$buildInfoPath = Join-Path $PSScriptRoot 'Builds/Windows/build-info.json'
+$buildInfo = $null
+$buildInfoRaw = $null
+if(Test-Path -LiteralPath $buildInfoPath) {
+    try {
+        $buildInfoRaw = Get-Content -LiteralPath $buildInfoPath -Raw -Encoding utf8
+        if([string]::IsNullOrWhiteSpace($buildInfoRaw)) { throw 'build-info.json is empty.' }
+        $buildInfo = $buildInfoRaw | ConvertFrom-Json
+        if($null -eq $buildInfo) { throw 'build-info.json has no JSON object.' }
+    }
+    catch { Write-Warning "build-info.json を読み取れません。古いビルドとして扱います: $($_.Exception.Message)" }
+}
+else { Write-Warning 'build-info なし（古いビルド）' }
 # Git is optional on the check PC; never let it stop the launch.
 $commit = 'unknown'
 if(Get-Command git -ErrorAction SilentlyContinue) {
     $previous = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
-    try { $value = & git -C $PSScriptRoot rev-parse --short HEAD 2>$null; if($LASTEXITCODE -eq 0 -and $value) { $commit = $value } }
+    try { $value = & git -C $PSScriptRoot rev-parse HEAD 2>$null; if($LASTEXITCODE -eq 0 -and $value) { $commit = [string]$value } }
     finally { $ErrorActionPreference = $previous }
 }
 $build = Get-Item -LiteralPath $exe
+$sessionNotes = Join-Path $note 'session-notes.txt'
 "device=$Device started=$(Get-Date -Format s) build=$($build.LastWriteTime.ToString('s')) commit=$commit desktop=$Desktop" |
-    Add-Content -LiteralPath (Join-Path $note 'session-notes.txt') -Encoding utf8
+    Add-Content -LiteralPath $sessionNotes -Encoding utf8
+if($null -ne $buildInfoRaw) {
+    'build-info-begin' | Add-Content -LiteralPath $sessionNotes -Encoding utf8
+    $buildInfoRaw | Add-Content -LiteralPath $sessionNotes -Encoding utf8
+    'build-info-end' | Add-Content -LiteralPath $sessionNotes -Encoding utf8
+}
+if($null -ne $buildInfo) {
+    if($buildInfo.dirty -eq $true) { Write-Warning '未コミットの変更を含むビルドです。build-info.json の dirtyFiles を確認してください。' }
+    if($commit -ne 'unknown' -and $buildInfo.commit -and $buildInfo.commit -ne 'unknown' -and $commit -ne $buildInfo.commit) {
+        Write-Warning "現在の HEAD ($commit) とビルドの commit ($($buildInfo.commit)) が異なります。"
+    }
+}
 if($Desktop) { Start-Process -FilePath $exe -ArgumentList '--desktop' } else { Start-Process -FilePath $exe }
 Invoke-Item -LiteralPath $sessions
 Write-Output "Started $exe ($Device). Checklist: Docs/DEVICE_QUICKCHECK.md. Evidence folder: $note"
```
