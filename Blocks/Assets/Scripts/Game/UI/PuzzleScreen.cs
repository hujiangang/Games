using UnityEngine;
using UnityEngine.UI;

/// <summary>One layout contract for the world camera and screen-space controls.</summary>
public static class PuzzleScreen
{
    public const float Header=145,Footer=125;
    public static readonly Rect WorldContent=new(-2.45f,-5.05f,4.9f,9.45f);
    public struct Metrics
    {
        public int width,height;
        public Rect safe,content;
        public float scale;
    }
    // Screen edges only: never use the opening arrangement as a drop constraint.
    public static Rect DragBounds {
        get {
            var cam=Camera.main;if(!cam) return WorldContent;
            var m=Current;float margin=20*m.scale;
            float bottom=m.safe.yMin+Footer*m.scale;
            float top=m.safe.yMax-margin;
            float units=cam.orthographicSize*2/m.height;
            Vector2 center=cam.transform.position;
            return Rect.MinMaxRect(center.x+(m.safe.xMin+margin-m.width*.5f)*units,
                center.y+(bottom-m.height*.5f)*units,
                center.x+(m.safe.xMax-margin-m.width*.5f)*units,
                center.y+(top-m.height*.5f)*units);
        }
    }
#if UNITY_EDITOR
    public static Metrics? Preview;
#endif
    public static Metrics Current {
        get {
#if UNITY_EDITOR
            if(Preview.HasValue) return Preview.Value;
#endif
            return Calculate(Screen.width,Screen.height,Screen.safeArea);
        }
    }
    public static Metrics Calculate(int width,int height,Rect safe)
    {
        width=Mathf.Max(1,width);height=Mathf.Max(1,height);
        if(safe.width<=0 || safe.height<=0) safe=new Rect(0,0,width,height);
        safe=Rect.MinMaxRect(Mathf.Clamp(safe.xMin,0,width),Mathf.Clamp(safe.yMin,0,height),
            Mathf.Clamp(safe.xMax,0,width),Mathf.Clamp(safe.yMax,0,height));
        float scale=Mathf.Min(safe.width/1080f,safe.height/1800f);
        float column=Mathf.Min(safe.width,1080*scale);
        return new Metrics { width=width,height=height,safe=safe,scale=scale,
            content=new Rect(safe.center.x-column*.5f,safe.yMin+Footer*scale,column,
                safe.height-(Header+Footer)*scale) };
    }
    public static void FitCamera(Camera cam)
    {
        var m=Current;var world=WorldContent;
        float unitsPerPixel=Mathf.Max(world.width/m.content.width,world.height/m.content.height);
        cam.orthographic=true;cam.orthographicSize=unitsPerPixel*m.height*.5f;
        cam.aspect=m.width/(float)m.height;
        var center=world.center-(m.content.center-new Vector2(m.width,m.height)*.5f)*unitsPerPixel;
        cam.transform.position=new Vector3(center.x,center.y,cam.transform.position.z);
    }
    public static void FitCanvas(CanvasScaler scaler,RectTransform safeRoot=null)
    {
        var m=Current;
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ConstantPixelSize;scaler.scaleFactor=m.scale;
        // Apply immediately as validation capture and device resize can occur between scaler updates.
        scaler.GetComponent<Canvas>().scaleFactor=m.scale;
        if(!safeRoot) return;
        float width=Mathf.Min(m.safe.width,1080*m.scale);
        safeRoot.anchorMin=new Vector2((m.safe.center.x-width*.5f)/m.width,m.safe.yMin/m.height);
        safeRoot.anchorMax=new Vector2((m.safe.center.x+width*.5f)/m.width,m.safe.yMax/m.height);
        safeRoot.offsetMin=safeRoot.offsetMax=Vector2.zero;
    }
}
