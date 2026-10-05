using System;
using System.Collections.Generic;
using System.IO;
using Clipper2Lib;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>Runs against the same geometry and triangulator as the shipped game.</summary>
public static class BlocksValidation
{
    private static int assertions;
    public static void Require(bool condition,string message)
    { assertions++;if(!condition) throw new Exception(message); }

    [MenuItem("Blocks/Validate content and gameplay rules")]
    public static void RunAll()
    {
        assertions=0;
        Require(LevelPersistence.GetSumLevel()==50,"Expected 50 contiguous levels");
        int previousCount=0;
        var frame=PuzzleGeometry.Frame(new Rect(-2,-2,4,4));
        var fingerprints=new HashSet<string>();
        var chinese=JsonUtility.FromJson<GameText.Table>(Resources.Load<TextAsset>("Localization/zh-CN").text);
        var english=JsonUtility.FromJson<GameText.Table>(Resources.Load<TextAsset>("Localization/en").text);
        var zh=new Dictionary<string,string>();var en=new Dictionary<string,string>();
        foreach(var entry in chinese.entries) zh.Add(entry.key,entry.value);
        foreach(var entry in english.entries) en.Add(entry.key,entry.value);
        Require(zh.Count==en.Count,"Locale tables differ in key count");
        foreach(var pair in zh) {
            Require(en.ContainsKey(pair.Key),"Missing English key: "+pair.Key);
            var pattern=@"\{[0-9]+(?::[^}]+)?\}";
            var firstMatches=System.Text.RegularExpressions.Regex.Matches(pair.Value,pattern);
            var secondMatches=System.Text.RegularExpressions.Regex.Matches(en[pair.Key],pattern);
            Require(firstMatches.Count==secondMatches.Count,"Locale placeholders differ: "+pair.Key);
            for(int i=0;i<firstMatches.Count;i++) Require(firstMatches[i].Value==secondMatches[i].Value,"Locale placeholder format differs: "+pair.Key);
            Require(!System.Text.RegularExpressions.Regex.IsMatch(en[pair.Key],@"[\u4e00-\u9fff]"),"Untranslated English: "+pair.Key);
        }
        for(int level=1;level<=50;level++) {
            var asset=Resources.Load<TextAsset>("LevelsData/Level_"+level);
            var data=JsonUtility.FromJson<LevelData>(asset.text);
            Require(LevelValidator.Validate(data,out var error),$"Level {level}: {error}");
            Require(zh.ContainsKey("level.Level_"+level+".name"),"Missing level title: "+level);
            var layout=OpeningLayouts.Get(data.levelName);
            Require(layout!=null && layout.positions.Length==data.pieces.Count && layout.order.Length==data.pieces.Count && layout.grabPoints.Length==data.pieces.Count,"Invalid opening layout: "+level);
            var used=new HashSet<int>();
            foreach(int index in layout.order) Require(index>=0 && index<data.pieces.Count && used.Add(index),"Invalid opening order: "+level);
            Require(data.pieces.Count>=previousCount,$"Level {level}: piece count decreases");
            previousCount=data.pieces.Count;
            var paths=new Paths64();
            string signature="";
            foreach(var piece in data.pieces) {
                var p=PuzzleGeometry.Path(piece.vertices,Matrix4x4.identity);paths.Add(p);
                Require(PuzzleGeometry.Area(new Paths64{p})>=.6799,$"Level {level}: fragment too small");
                foreach(var v in piece.vertices) signature+=v.ToString("F5");
                var triangleIndices=new Triangulator(piece.vertices.ToArray()).Triangulate();
                Require(triangleIndices.Length==(piece.vertices.Count-2)*3,$"Level {level}: incomplete mesh");
            }
            Require(fingerprints.Add(signature),$"Level {level}: duplicate geometry");
            Require(PuzzleGeometry.IsComplete(paths,frame,paths.Count),$"Level {level}: correct answer rejected");
            var missing=new Paths64(paths);missing.RemoveAt(0);
            Require(!PuzzleGeometry.IsComplete(missing,frame,paths.Count),$"Level {level}: missing piece accepted");
            var moved=new Paths64(paths);
            var displaced=new Path64();foreach(var v in paths[0]) displaced.Add(new Point64(v.X+5000,v.Y+7000));
            moved[0]=displaced;
            Require(!PuzzleGeometry.IsComplete(moved,frame,paths.Count),$"Level {level}: displaced answer accepted");
            var overlap=new Paths64(paths);overlap[1]=paths[0];
            Require(!PuzzleGeometry.IsComplete(overlap,frame,paths.Count),$"Level {level}: overlap accepted");
            var others=new Paths64(paths);others.RemoveAt(0);
            Require(PuzzleGeometry.CanPlace(paths[0],frame,others),$"Level {level}: valid shared edge blocked");
            Require(!PuzzleGeometry.CanPlace(paths[1],frame,others),$"Level {level}: duplicate placement accepted");
        }
        Require(PuzzleGeometry.SolveNormals(new Vector2(1,1).normalized,.2f,new Vector2(-1,1).normalized,.1f,out var delta),"Diagonal constraint solve failed");
        Require(Mathf.Abs(Vector2.Dot(delta,new Vector2(1,1).normalized)-.2f)<.00001,"Incorrect diagonal snap");
        var gate=new AdRequestGate();int first=gate.Begin();
        Require(gate.Complete(first),"Reward request rejected");
        Require(!gate.Complete(first),"Duplicate callback paid twice");
        int second=gate.Begin();Require(!gate.Complete(first),"Stale callback completed new request");
        gate.Cancel();Require(!gate.Complete(second),"Cancelled request paid a reward");
        var bus=new EventBus<int>();int calls=0;Action handler=()=>calls++;
        bus.Subscribe(1,handler);bus.Get<Action>(1)?.Invoke();bus.Unsubscribe(1,handler);bus.Get<Action>(1)?.Invoke();
        Require(calls==1,"Event unsubscribe failed");
        var config=DouyinConfig.Load();Require(!config.editorSimulation,"Editor ad simulation must be disabled in delivery configuration");
        foreach(var name in new[]{"click","snap","powerUp1","morning_mosaic"})
            Require(Resources.Load<AudioClip>("Audio/"+name)!=null,"Missing audio: "+name);
        foreach(var name in new[]{"logo","background","background-mint","background-apricot","background-lavender"})
            Require(Resources.Load<Texture2D>("Brand/"+name)!=null,"Missing brand texture: "+name);
        string result=$"PASS: 50 levels; {assertions} assertions. Geometry, triangulation, fragment area, unique layouts, invalid solutions, diagonal snapping, ad callback gate and audio imports.";
        Directory.CreateDirectory("ValidationResults");File.WriteAllText("ValidationResults/content-validation.txt",result);
        Debug.Log(result);
    }

    public static void RunBatch()
    {
        try { RunAll();EditorApplication.Exit(0); }
        catch(Exception e) { Debug.LogException(e);EditorApplication.Exit(1); }
    }

    [MenuItem("Blocks/Check Douyin release configuration")]
    public static void ValidateDouyinRelease()
    {
        var c=DouyinConfig.Load();
        if(!c.enabled || string.IsNullOrWhiteSpace(c.appId) || string.IsNullOrWhiteSpace(c.rewardedVideoAdId) || string.IsNullOrWhiteSpace(c.interstitialAdId))
            throw new BuildFailedException("Fill Resources/Config/Douyin.json: enabled, AppID and both ad placement IDs.");
        if(c.editorSimulation) throw new BuildFailedException("Disable editorSimulation before release.");
#if !BLOCKS_DOUYIN
        throw new BuildFailedException("Import official TTSDK 6.2.1+, add its assembly reference to Blocks.Game, and enable BLOCKS_DOUYIN for the target platform.");
#else
        Debug.Log("Douyin code/config checks passed. Complete SDK export and device acceptance before submitting.");
#endif
    }
}

public class BlocksBuildGuard : IPreprocessBuildWithReport
{
    public int callbackOrder => 0;
    public void OnPreprocessBuild(BuildReport report)
    {
        BlocksValidation.RunAll();
        if(DouyinConfig.Load().enabled) BlocksValidation.ValidateDouyinRelease();
    }
}
