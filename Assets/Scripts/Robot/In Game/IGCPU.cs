using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]

public class IGCPU : IGComponent
{
    public CPU CPU;

    public void SetUp(CPU cpu){
        CPU = cpu;
        SpriteRenderer SR = GetComponent<SpriteRenderer>();
        if(CPU is not null) SR.sprite = CPU.Sprite;
    }
}
