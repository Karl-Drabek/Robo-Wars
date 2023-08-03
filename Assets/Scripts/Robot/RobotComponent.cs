using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ComponentType{
    Sensor, Misc, CPU, Movement, Weapon
}

public class RobotComponent
{
    public Dictionary<string, Func<List<dynamic>, GameContext, dynamic>> Funcs;
    public List<FuncType> FuncTypes;
    public Sprite Sprite;
    public string Name;
    
    public RobotComponent(string name, List<FuncType> funcTypes, Dictionary<string, Func<List<dynamic>, GameContext, dynamic>> funcs, Sprite sprite){
        (Name, Funcs, FuncTypes, Sprite) = (name, funcs, funcTypes, sprite);
    }

    public ComponentType ToCT() => this switch{
        Sensor => ComponentType.Sensor,
        CPU => ComponentType.CPU,
        Movement => ComponentType.Movement,
        Weapon => ComponentType.Weapon,
        _ => ComponentType.Misc};
}
