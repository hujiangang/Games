using System;
using UnityEngine;

public static class OpeningLayouts
{
    [Serializable] public class Layout { public string levelName;public Vector2[] positions,grabPoints;public int[] order; }
    [Serializable] class Catalog { public Layout[] levels; }
    private static Catalog catalog;
    public static Layout Get(string levelName)
    {
        if(catalog==null) {
            var source=Resources.Load<TextAsset>("Config/OpeningLayouts");
            if(source) catalog=JsonUtility.FromJson<Catalog>(source.text);
        }
        return catalog?.levels==null?null:Array.Find(catalog.levels,l=>l.levelName==levelName);
    }
}
