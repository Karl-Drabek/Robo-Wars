using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]

public class ItemDrag : ItemDisplay, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform trans;
    public ItemDrag clone;
    public bool isLocked;
    public bool isClone;

    private CanvasGroup canvasGroup;
    private Canvas canvas;

    void Awake(){
        trans = this.GetComponent<RectTransform>();
        canvasGroup = this.GetComponent<CanvasGroup>();
        canvas = GameObject.Find("/Canvas").GetComponent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData data){
        clone = Instantiate(this, this.transform.position, Quaternion.identity, this.transform);
        clone.isLocked = false;
        clone.isClone = true;
        clone.RC = this.RC;
        clone.canvasGroup.alpha = .75f;
        clone.canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData data){
        if(clone is not null){
            clone.trans.anchoredPosition += data.delta / canvas.scaleFactor;
        }
    }
    
    public void OnEndDrag(PointerEventData data){
        if(!clone.isLocked) Destroy(clone.gameObject);
        if(this.isClone) Destroy(this.gameObject);
        clone.canvasGroup.alpha = 1f;
        clone.canvasGroup.blocksRaycasts = true; 
        clone = null;
    }
}
