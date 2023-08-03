using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemLock : MonoBehaviour, IDropHandler
{
    public ItemDrag id;
    public ComponentType ct;
    public RobotComponent RC;

    public void OnDrop(PointerEventData data){
        if(data.pointerDrag != null){
            id = data.pointerDrag.GetComponent<ItemDrag>();
            if(ct == id.clone.RC.ToCT()){
                RC = id.RC;
                id.clone.isLocked = true;
                id.clone.trans.SetParent(this.transform); 
                id.clone.trans.anchorMin = id.clone.trans.anchorMax= new Vector2(0.5f, 0.5f);
                id.clone.trans.anchoredPosition = new Vector3(0, 0);
            }
        }
    }
}
