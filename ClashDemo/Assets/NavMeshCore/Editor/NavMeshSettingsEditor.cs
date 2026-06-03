using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NavMeshSettings))]
public class NavMeshSettingsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        NavMeshSettings settings = (NavMeshSettings)target;

        GUILayout.Space(10);
        if (GUILayout.Button("打开烘焙窗口", GUILayout.Height(30)))
        {
            NavMeshBakeWindow.ShowWindow(settings);
        }
    }
}