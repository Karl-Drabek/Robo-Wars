using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]

public class IGMovement : IGComponent
{
    public Movement Movement;

    public void SetUp(Movement movement){
        Movement = movement;
        SpriteRenderer SR = GetComponent<SpriteRenderer>();
        if(Movement is not null) SR.sprite = Movement.Sprite;
    }
}
