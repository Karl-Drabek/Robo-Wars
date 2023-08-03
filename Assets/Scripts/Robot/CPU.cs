using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CPU : RobotComponent
{
    public CPU(string name, List<FuncType> funcTypes, Dictionary<string, Func<List<dynamic>, GameContext, dynamic>> funcs, Sprite sprite) : base(name, funcTypes, funcs, sprite){}
}
