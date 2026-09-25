using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
using LoopRoom;

public static class DemoSetup
{
    const string ScenePath="Assets/LoopRoom/Scenes/LoopRoom.unity";
    const string BuildInfoPath="Builds/Windows/build-info.json";

    [Serializable]
    class BuildInfo
    {
        public string commit="unknown";
        public bool dirty;
        public string[] dirtyFiles=Array.Empty<string>();
        public string builtAt;
        public string unityVersion;
    }

    [MenuItem("LoopRoom/1 - Prepare project and scene")]
    public static void Prepare()
    {
        if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        Directory.CreateDirectory("Assets/LoopRoom/Scenes");
        PlayerSettings.companyName="LoopRoomDemo"; PlayerSettings.productName="The Room Before";
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        PlayerSettings.runInBackground=true;
        PlayerSettings.colorSpace=ColorSpace.Linear;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,new[]{GraphicsDeviceType.Direct3D11});
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone,ScriptingImplementation.Mono2x);
        var playerSettings=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
        var input=playerSettings.FindProperty("activeInputHandler");
        if(input!=null){input.intValue=1;playerSettings.ApplyModifiedPropertiesWithoutUndo();}
        var tags=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        tags.FindProperty("layers").GetArrayElementAtIndex(8).stringValue="PlayerOnly";
        tags.FindProperty("layers").GetArrayElementAtIndex(9).stringValue="SpectatorOnly";
        tags.ApplyModifiedPropertiesWithoutUndo();
        // Preserve dynamically requested shaders in player builds.
        var graphics=new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset")[0]);
        var shaders=graphics.FindProperty("m_AlwaysIncludedShaders");
        foreach(var name in new[]{"Universal Render Pipeline/Lit","Universal Render Pipeline/Unlit","LoopRoom/Text"})
        {
            var shader=Shader.Find(name);
            if(shader==null) throw new InvalidOperationException("Missing shader: "+name);
            bool found=false;
            for(int i=0;i<shaders.arraySize;i++) if(shaders.GetArrayElementAtIndex(i).objectReferenceValue==shader) found=true;
            if(!found){int index=shaders.arraySize;shaders.InsertArrayElementAtIndex(index);shaders.GetArrayElementAtIndex(index).objectReferenceValue=shader;}
        }
        graphics.ApplyModifiedPropertiesWithoutUndo();
        if(!File.Exists(ScenePath))
        {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("LoopRoom Demo").AddComponent<LoopDemo>();
            EditorSceneManager.SaveScene(scene,ScenePath);
        }
        else EditorSceneManager.OpenScene(ScenePath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
        AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        Debug.Log("LoopRoom: scene prepared. Run menu 2 to configure PC OpenXR.");
    }

    [MenuItem("LoopRoom/2 - Configure Quest Link OpenXR")]
    public static void ConfigureXR()
    {
        Directory.CreateDirectory("Assets/XR/Settings");AssetDatabase.Refresh();
        if(!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey,out XRGeneralSettingsPerBuildTarget perTarget))
        {
            perTarget=ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
            AssetDatabase.CreateAsset(perTarget,"Assets/XR/Settings/GeneralSettings.asset");
            EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey,perTarget,true);
        }
        var general=perTarget.SettingsForBuildTarget(BuildTargetGroup.Standalone);
        if(general==null)
        {
            general=ScriptableObject.CreateInstance<XRGeneralSettings>();
            AssetDatabase.AddObjectToAsset(general,perTarget);
            perTarget.SetSettingsForBuildTarget(BuildTargetGroup.Standalone,general);
        }
        if(general.Manager==null)
        {
            var manager=ScriptableObject.CreateInstance<XRManagerSettings>();
            AssetDatabase.AddObjectToAsset(manager,perTarget);general.Manager=manager;
        }
        // DemoRig creates input bindings, then explicitly initializes XR.
        general.InitManagerOnStart=false;
        general.Manager.automaticLoading=false;general.Manager.automaticRunning=false;
        if(!XRPackageMetadataStore.AssignLoader(general.Manager,"UnityEngine.XR.OpenXR.OpenXRLoader",BuildTargetGroup.Standalone))
            throw new InvalidOperationException("Could not assign OpenXR loader; inspect XR Plug-in Management.");
        EditorUtility.SetDirty(perTarget);EditorUtility.SetDirty(general);EditorUtility.SetDirty(general.Manager);
        AssetDatabase.SaveAssets();
        var openxr=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
        if(openxr!=null)
        {
            openxr.renderMode=OpenXRSettings.RenderMode.SinglePassInstanced;
            var profile=openxr.GetFeature<OculusTouchControllerProfile>();
            if(profile!=null){profile.enabled=true;EditorUtility.SetDirty(profile);}
            else Debug.LogWarning("Enable Oculus Touch Controller Profile in Project Settings > XR Plug-in Management > OpenXR (Windows), then run menu 3.");
            var touchPlusProfile=openxr.GetFeature<MetaQuestTouchPlusControllerProfile>();
            if(touchPlusProfile!=null){touchPlusProfile.enabled=true;EditorUtility.SetDirty(touchPlusProfile);}
            else Debug.LogWarning("Enable Meta Quest Touch Plus Controller Profile in Project Settings > XR Plug-in Management > OpenXR (Windows), then run menu 3.");
            EditorUtility.SetDirty(openxr);AssetDatabase.SaveAssets();
        }
        else Debug.LogWarning("Open Project Settings > XR Plug-in Management > OpenXR (Windows) to create the OpenXR settings, add Oculus Touch Controller Profile and Meta Quest Touch Plus Controller Profile, then run menu 3.");
        Debug.Log("LoopRoom: OpenXR loader configured; machine runtime and headset connection are not changed by this menu.");
    }

    [MenuItem("LoopRoom/3 - Validate settings and model")]
    public static void Validate()
    {
        var results=LoopModelChecks.Run();
        var general=XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
        if(general==null || general.Manager==null || !general.Manager.activeLoaders.Any(x=>x.GetType().FullName=="UnityEngine.XR.OpenXR.OpenXRLoader"))
            throw new InvalidOperationException("OpenXR loader missing. Run menu 2.");
        if(general.InitManagerOnStart) throw new InvalidOperationException("Automatic XR startup must be disabled: input is created before manual initialization.");
        var settings=OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Standalone);
        var profile=settings!=null?settings.GetFeature<OculusTouchControllerProfile>():null;
        var touchPlusProfile=settings!=null?settings.GetFeature<MetaQuestTouchPlusControllerProfile>():null;
        bool oculusTouchEnabled=profile!=null && profile.enabled;
        bool touchPlusEnabled=touchPlusProfile!=null && touchPlusProfile.enabled;
        if(!oculusTouchEnabled && !touchPlusEnabled) throw new InvalidOperationException("Enable Oculus Touch Controller Profile or Meta Quest Touch Plus Controller Profile in OpenXR settings.");
        Directory.CreateDirectory("TestResults");File.WriteAllLines("TestResults/model-checks.txt",results);
        Debug.Log("LoopRoom: controller profiles: Oculus Touch="+oculusTouchEnabled+", Meta Quest Touch Plus="+touchPlusEnabled+".");
        Debug.Log("LoopRoom: "+results.Count+" model checks passed; XR configuration checked. HMD behavior still requires playtesting.");
    }

    [MenuItem("LoopRoom/4 - Build Windows demo")]
    public static void Build()
    {
        var buildInfo=CaptureBuildInfo();
        Prepare(); ConfigureXR(); Validate();
        Directory.CreateDirectory("Builds/Windows");
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{
            scenes=new[]{ScenePath},locationPathName="Builds/Windows/LoopRoom.exe",
            target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
        if(report.summary.result!=BuildResult.Succeeded) throw new Exception("Build failed: "+report.summary.result);
        buildInfo.builtAt=DateTimeOffset.Now.ToString("o");
        File.WriteAllText(BuildInfoPath,JsonUtility.ToJson(buildInfo,true));
        Debug.Log("LoopRoom: Windows build completed.");
    }

    static BuildInfo CaptureBuildInfo()
    {
        var info=new BuildInfo{unityVersion=Application.unityVersion};
        string output;
        bool gitStarted;
        if(TryRunGit("rev-parse HEAD",out output,out gitStarted)) info.commit=output.Trim();
        if(gitStarted && TryRunGit("status --porcelain=v1 -z --untracked-files=all -- Assets ProjectSettings Packages",out output,out gitStarted))
        {
            info.dirtyFiles=ParseDirtyFiles(output).Take(20).ToArray();
            info.dirty=info.dirtyFiles.Length>0;
        }
        return info;
    }

    static bool TryRunGit(string arguments,out string output,out bool started)
    {
        output=string.Empty;started=false;
        try
        {
            var startInfo=new System.Diagnostics.ProcessStartInfo{
                FileName="git",Arguments=arguments,WorkingDirectory=Path.GetDirectoryName(Application.dataPath),
                UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true};
            using(var process=new System.Diagnostics.Process{StartInfo=startInfo})
            {
                if(!process.Start()) return false;
                started=true;
                output=process.StandardOutput.ReadToEnd();
                var error=process.StandardError.ReadToEnd();
                process.WaitForExit();
                if(process.ExitCode==0) return true;
                UnityEngine.Debug.LogWarning("LoopRoom: git "+arguments+" failed; build metadata will be incomplete. "+error.Trim());
            }
        }
        catch(Exception exception)
        {
            UnityEngine.Debug.LogWarning("LoopRoom: git is unavailable; build metadata will use commit=unknown. "+exception.Message);
        }
        return false;
    }

    static IEnumerable<string> ParseDirtyFiles(string porcelain)
    {
        var fields=porcelain.Split(new[]{'\0'},StringSplitOptions.RemoveEmptyEntries);
        for(int i=0;i<fields.Length;i++)
        {
            var entry=fields[i];
            if(entry.Length<4) continue;
            var status=entry.Substring(0,2);
            yield return entry.Substring(3);
            if((status.IndexOf('R')>=0 || status.IndexOf('C')>=0) && i+1<fields.Length) i++;
        }
    }

    [MenuItem("LoopRoom/Run model checks only")]
    public static void ModelChecks()
    {
        Debug.Log(string.Join("\n",LoopModelChecks.Run()));
    }
}
