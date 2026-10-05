using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Click or drag the handle; the drawer overlays the board without moving the camera.</summary>
public class PuzzleDrawer : MonoBehaviour,IPointerDownHandler,IPointerClickHandler,IBeginDragHandler,IDragHandler,IEndDragHandler
{
    public const float Height=460,Peek=110,Travel=Height-Peek;
    public RectTransform panel;
    public CanvasGroup controls;
    public PuzzleIcon arrow;
    private bool open,dragging,moved;
    private float reveal;
    private int pointer;
    public bool IsOpen=>open;
    public bool IsSettled=>Mathf.Abs(reveal-(open?Travel:0))<.01f;
    bool CanInteract=>!AdService.IsBusy && !HintWindow.IsShowing &&
        !DraggableComponent.Active.Exists(p=>p && p.IsDragging);
    public void SetOpen(bool value,bool immediate=false)
    {
        open=value;dragging=false;
        if(immediate) reveal=open?Travel:0;
        Refresh();
    }
    public void Refresh()
    {
        if(!panel) return;
        panel.anchoredPosition=new Vector2(0,-Travel+reveal);
        controls.alpha=Mathf.InverseLerp(Travel*.25f,Travel,reveal);
        controls.interactable=controls.blocksRaycasts=open && IsSettled && CanInteract;
        arrow.Symbol=open?PuzzleIcon.Kind.Collapse:PuzzleIcon.Kind.Expand;
    }
    void Update()
    {
        if(dragging && !CanInteract) dragging=false;
        if(!dragging) reveal=Mathf.MoveTowards(reveal,open?Travel:0,Time.unscaledDeltaTime*1500);
        Refresh();
    }
    public void OnPointerDown(PointerEventData e) { if(!dragging) { pointer=e.pointerId;moved=false; } }
    public void OnPointerClick(PointerEventData e)
    { if(e.pointerId==pointer && !moved && CanInteract) { SetOpen(!open);GameEvents.InvokeBasicEvent(GameBasicEvent.UIClick); } }
    public void OnBeginDrag(PointerEventData e) { if(e.pointerId==pointer && CanInteract) { dragging=true;moved=true; } }
    public void OnDrag(PointerEventData e)
    {
        if(!dragging || e.pointerId!=pointer || !CanInteract) return;
        reveal=Mathf.Clamp(reveal+e.delta.y/GetComponentInParent<Canvas>().scaleFactor,0,Travel);Refresh();
    }
    public void OnEndDrag(PointerEventData e) { if(dragging && e.pointerId==pointer) SetOpen(reveal>Travel*.4f); }
    void OnDisable() { dragging=false; }
}
