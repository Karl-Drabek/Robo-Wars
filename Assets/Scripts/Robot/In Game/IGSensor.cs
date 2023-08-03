using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]

public class IGSensor : IGComponent
{
    public Sensor Sensor;

    public void SetUp(Sensor sensor){
        Sensor = sensor;
        SpriteRenderer SR = GetComponent<SpriteRenderer>();
        if(Sensor is not null) SR.sprite = Sensor.Sprite;
    }
}
