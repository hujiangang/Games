using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Quiet background and clear play surfaces, without per-frame spawning.</summary>
public class BackgroundComponent : MonoBehaviour
{
    [Serializable] public class Catalog { public string[] paths; }
    private readonly Dictionary<string,Sprite> sprites=new();
    private readonly System.Random random=new();
    private string[] paths;
    private int index=-1;
    public string CurrentPath=>index<0?null:paths[index];
    public int AvailableCount=>paths.Length;
    private SpriteRenderer backdrop;
    void Awake()
    {
        var catalog=Resources.Load<TextAsset>("Config/Backgrounds");
        paths=catalog?JsonUtility.FromJson<Catalog>(catalog.text).paths:new[]{"Brand/background"};
        var go=new GameObject("Pastel backdrop");go.transform.SetParent(transform,false);
        backdrop=go.AddComponent<SpriteRenderer>();backdrop.sortingOrder=-100;
        backdrop.color=new Color(1,1,1,.8f);
        var canvas=new GameObject("Play surfaces",typeof(RectTransform),typeof(Canvas));
        canvas.transform.SetParent(transform,false);canvas.transform.position=Vector3.zero;
        var ui=canvas.GetComponent<Canvas>();ui.renderMode=RenderMode.WorldSpace;ui.sortingOrder=-10;
        Panel(canvas.transform,"Board paper",new Vector2(0,2.15f),new Vector2(4.20f,4.20f),new Color(.985f,.994f,1),.12f);
    }
    void OnEnable() { GameEvents.RegisterBasicEvent(GameBasicEvent.ResetUI,SelectNext); }
    void OnDisable() { GameEvents.UnregisterBasicEvent(GameBasicEvent.ResetUI,SelectNext); }
    void Start() { if(index<0) SelectNext(); }
    public void SelectNext()
    {
        int next=random.Next(index<0?paths.Length:Mathf.Max(1,paths.Length-1));
        if(index>=0 && paths.Length>1 && next>=index) next++;
        index=next;
        if(!sprites.TryGetValue(CurrentPath,out var sprite)) {
            var texture=Resources.Load<Texture2D>(CurrentPath);
            if(!texture) throw new InvalidOperationException("Missing background: "+CurrentPath);
            sprite=Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),Vector2.one*.5f,100,0,SpriteMeshType.FullRect);
            sprites.Add(CurrentPath,sprite);
        }
        backdrop.sprite=sprite;Refresh();
    }
    static void Panel(Transform parent,string name,Vector2 position,Vector2 size,Color color,float radius)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(RoundedPanel));
        var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);
        rect.sizeDelta=size;rect.position=new Vector3(position.x,position.y,.2f);
        var image=go.GetComponent<RoundedPanel>();image.radius=radius;image.color=color;image.raycastTarget=false;
    }
    public void Refresh()
    {
        var cam=Camera.main;if(!cam || !backdrop || !backdrop.sprite) return;
        float size=Mathf.Max(cam.orthographicSize*2/backdrop.sprite.bounds.size.y,
            cam.orthographicSize*2*cam.aspect/backdrop.sprite.bounds.size.x);
        backdrop.transform.position=new Vector3(cam.transform.position.x,cam.transform.position.y,1);
        backdrop.transform.localScale=Vector3.one*size;
    }
    void LateUpdate() { Refresh(); }
    void OnDestroy() { foreach(var sprite in sprites.Values) if(sprite) Destroy(sprite); }
}
