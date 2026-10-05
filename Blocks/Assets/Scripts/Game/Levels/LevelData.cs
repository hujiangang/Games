using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public class PieceData { public List<Vector2> vertices;public Color color; }
[System.Serializable]
public class LevelData
{
    public string levelName,displayName,designNote;
    public int chapter=1,difficulty=1;
    public float boardHalfSize=2,snapDistance=.32f;
    public List<PieceData> pieces=new();
}
