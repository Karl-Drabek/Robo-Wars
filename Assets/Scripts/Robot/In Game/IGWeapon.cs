using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]

public class IGWeapon : IGComponent
{
    public Weapon Weapon;

    public void SetUp(Weapon weapon){
        Weapon = weapon;
        SpriteRenderer SR = GetComponent<SpriteRenderer>();
        if(Weapon is not null) SR.sprite = Weapon.Sprite;
    }
}
