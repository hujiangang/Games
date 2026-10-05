using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BlocksPreviewBuild
{
    [MenuItem("Blocks/Configure portrait player")]
    public static void ConfigurePortrait()
    {
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToLandscapeLeft=false;
        PlayerSettings.allowedAutorotateToLandscapeRight=false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown=false;
        PlayerSettings.defaultScreenWidth=540;PlayerSettings.defaultScreenHeight=960;
        PlayerSettings.defaultWebScreenWidth=540;PlayerSettings.defaultWebScreenHeight=960;
        PlayerSettings.defaultIsNativeResolution=false;
        PlayerSettings.fullScreenMode=FullScreenMode.Windowed;
        var icon=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Brand/app-icon.png");
        if(icon) {
            var icons=new Texture2D[PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Unknown).Length];
            for(int i=0;i<icons.Length;i++) icons[i]=icon;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown,icons);
        }
        AssetDatabase.SaveAssets();
    }
    public static void BuildBatch()
    {
        try {
            ConfigurePortrait();
            var root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../../"));
            var folder=Path.Combine(root,"Build/WindowsPreview");Directory.CreateDirectory(folder);
            var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes=new[]{"Assets/Scenes/GameScene.unity"},target=BuildTarget.StandaloneWindows64,
                locationPathName=Path.Combine(folder,"ColorBlocks.exe"),options=BuildOptions.None
            });
            var result=$"{report.summary.result}: Windows preview, {report.summary.totalSize} bytes, {report.summary.totalErrors} errors, {report.summary.totalWarnings} warnings.";
            Directory.CreateDirectory(Path.Combine(root,"ValidationResults"));
            File.WriteAllText(Path.Combine(root,"ValidationResults/build-validation.txt"),result);
            EditorApplication.Exit(report.summary.result==BuildResult.Succeeded?0:1);
        } catch(System.Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
    }
}
