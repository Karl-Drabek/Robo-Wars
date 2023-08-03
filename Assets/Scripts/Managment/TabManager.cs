using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TabManager : MonoBehaviour
{
    private GameObject currentObj;
    public GameObject HomeObj;  

    void Awake(){
        currentObj = HomeObj;
    }

    public void Activate(GameObject screen){
        currentObj.SetActive(false);
        currentObj = screen;
        screen.SetActive(true);

    }

    void OnEnable(){
        Activate(HomeObj);
    }
}
