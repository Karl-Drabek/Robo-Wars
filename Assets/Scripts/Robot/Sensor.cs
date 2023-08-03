using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sensor : RobotComponent
{
    public Sensor(string name, List<FuncType> funcTypes, Dictionary<string, Func<List<dynamic>, GameContext, dynamic>> funcs, Sprite sprite) : base(name, funcTypes, funcs, sprite){}
}
