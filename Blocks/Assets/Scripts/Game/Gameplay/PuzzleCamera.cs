using UnityEngine;

/// <summary>Keep the complete board and tray visible on narrow portrait devices.</summary>
[RequireComponent(typeof(Camera))]
public class PuzzleCamera : MonoBehaviour
{
    void Awake() { GetComponent<Camera>().backgroundColor=new Color(.91f,.95f,.97f); }
    void LateUpdate()
    {
        PuzzleScreen.FitCamera(GetComponent<Camera>());
    }
}
