using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    public Stack<GameObject> BackStack;
    private GameObject tempObj;
    public GameObject HomeObject;
    public GameObject BackButton;
    public GameObject HomeButton; 

    void Start(){
        BackStack = new Stack<GameObject>();
    }

    public void Home(){
        tempObj = BackStack.Peek();
        tempObj.SetActive(false);
        BackStack.Clear();
        HomeObject.SetActive(true);
        BackButton.SetActive(false);
        HomeButton.SetActive(false);
    }

    public void Back(){
        tempObj = BackStack.Pop();
        tempObj.SetActive(false);
        if(BackStack.Count != 0){
            tempObj = BackStack.Peek();
            tempObj.SetActive(true);
        }else{
            HomeObject.SetActive(true);
            BackButton.SetActive(false);
            HomeButton.SetActive(false);
        }
    }   

    public void Add(GameObject obj){
        if(BackStack.Count != 0){
            tempObj = BackStack.Peek();
            tempObj.SetActive(false);
        }else{
            HomeObject.SetActive(false);
        }
        BackStack.Push(obj);
        obj.SetActive(true);
        BackButton.SetActive(true);
        HomeButton.SetActive(true);
    }
}