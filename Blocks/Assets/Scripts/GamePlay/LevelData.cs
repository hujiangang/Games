using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class PieceData {
    public List<Vector2> vertices;
    public Color color;
}

[System.Serializable]
public class LevelData {
    public string levelName;
    public List<PieceData> pieces = new List<PieceData>();
}