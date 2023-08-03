using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class RobotAssembler : MonoBehaviour
{
    [SerializeField] private Robot prefab;
    [SerializeField] private ItemLock Sensor, CPU, LWeapon, RWeapon, Movement;
    [SerializeField] private TMP_Dropdown _dropdown;
    [SerializeField] private ScriptEditorManager _editor;
    private Robot robot;

    public Robot AssembleRobot(){
        var robot = Instantiate(prefab);
        return robot.SetUp((Sensor)Sensor.RC, (CPU)CPU.RC, (Weapon)LWeapon.RC,
        (Weapon)RWeapon.RC, (Movement)Movement.RC, _dropdown.value == 0, _editor.ParseResult);
    }

    public List<FuncType> GetFuncTypes(){
        List<FuncType> funcTypes = new();
        funcTypes.AddRange(Sensor.RC.FuncTypes);
        funcTypes.AddRange(CPU.RC.FuncTypes);
        funcTypes.AddRange(LWeapon.RC.FuncTypes);
        funcTypes.AddRange(RWeapon.RC.FuncTypes);
        funcTypes.AddRange(Movement.RC.FuncTypes);
        return funcTypes;
    }

    public Dictionary<string, Func<List<dynamic>, GameContext, dynamic>> GetFuncs(){
        Dictionary<string, Func<List<dynamic>, GameContext, dynamic>> funcs = new();
        Main.MergeDicts(funcs, Sensor.RC.Funcs);
        Main.MergeDicts(funcs, CPU.RC.Funcs);
        Main.MergeDicts(funcs, LWeapon.RC.Funcs);
        Main.MergeDicts(funcs, RWeapon.RC.Funcs);
        Main.MergeDicts(funcs, Movement.RC.Funcs);
        return funcs;
    }
}
