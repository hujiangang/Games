using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

/// <summary>One pointer owns each drag; cancel on focus loss and block UI passthrough.</summary>
public class InputManager : MonoBehaviour
{
    private DraggableComponent currentTarget;
    private Camera mainCamera;
    private int fingerId=-1;
    private Vector2 lastPosition;
    private readonly List<RaycastResult> uiHits=new();
    void Awake() { mainCamera=Camera.main; }
    void Update()
    {
        if(!mainCamera || GamePlay.isGlobalLocked || AdService.IsBusy || HintWindow.IsShowing) { Cancel();return; }
        if(Input.touchCount>0) {
            for(int i=0;i<Input.touchCount;i++) {
                Touch touch=Input.GetTouch(i);
                if(currentTarget==null && touch.phase==TouchPhase.Began && fingerId<0) Begin(touch.position,touch.fingerId);
                if(currentTarget==null || touch.fingerId!=fingerId) continue;
                lastPosition=touch.position;
                if(touch.phase==TouchPhase.Canceled) Cancel();
                else if(touch.phase==TouchPhase.Ended) End();
                else currentTarget.FollowMouse(World(touch.position));
            }
            return;
        }
        if(fingerId>=0) { Cancel();return; }
        lastPosition=Input.mousePosition;
        if(Input.GetMouseButtonDown(0)) Begin(lastPosition,-1);
        if(currentTarget && Input.GetMouseButton(0)) currentTarget.FollowMouse(World(lastPosition));
        if(Input.GetMouseButtonUp(0)) End();
    }
    Vector2 World(Vector2 p) => mainCamera.ScreenToWorldPoint(new Vector3(p.x,p.y,-mainCamera.transform.position.z));
    void Begin(Vector2 screen,int id)
    {
        if(currentTarget) return;
        if(EventSystem.current) {
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position=screen,pointerId=id },uiHits);
            if(uiHits.Count>0) return;
        }
        var point=World(screen);Physics2D.SyncTransforms();
        foreach(var col in Physics2D.OverlapPointAll(point,LayerMask.GetMask("PuzzlePiece"))) {
            var piece=col.GetComponent<DraggableComponent>();
            if(piece && piece.CanDrag && (!currentTarget || piece.sortingOrder>currentTarget.sortingOrder)) currentTarget=piece;
        }
        if(currentTarget) { fingerId=id;lastPosition=screen;currentTarget.StartDragging(point); }
    }
    void End() { if(currentTarget) currentTarget.StopDragging(World(lastPosition));currentTarget=null;fingerId=-1; }
    void Cancel() { if(currentTarget) currentTarget.CancelDragging();currentTarget=null;fingerId=-1; }
    void OnApplicationFocus(bool focused) { if(!focused) Cancel(); }
    void OnApplicationPause(bool paused) { if(paused) Cancel(); }
    void OnDisable() { Cancel(); }
    public void StartRepeatCoroutine() { }
    public void StopRepeatCoroutine() { }
}
