using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;

public class ItemDisplay : MonoBehaviour
{
    public RobotComponent RC;
    public Image Image;

    public void SetUp(RobotComponent rc){
        RC = rc;
        Image.sprite = RC.Sprite;
    }
}
