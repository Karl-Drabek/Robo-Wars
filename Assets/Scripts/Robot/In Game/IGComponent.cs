using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]

public class IGComponent : MonoBehaviour
{
    public SpriteRenderer SR;

    public void SetSprite(Sprite sprite){
        SR = GetComponent<SpriteRenderer>();
        SR.sprite = sprite;
    }
}
