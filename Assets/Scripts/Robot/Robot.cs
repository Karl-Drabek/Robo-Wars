using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Robot : MonoBehaviour
{
    public IGSensor Sensor;
    public IGCPU CPU;
    public IGWeapon LWeapon, RWeapon;
    public IGMovement Movement;
    public bool IsBlueTeam;
    public ParseResult AST;
    [SerializeField] private RobotAssembler _assembler;
    [SerializeField] private GridManager _manager;

    public Robot SetUp(Sensor sensor, CPU cpu, Weapon lweapon, Weapon rweapon, Movement movement, bool isBlueTeam, ParseResult ast){
        (IsBlueTeam, AST) = (isBlueTeam, ast);
        Sensor.SetUp(sensor);
        CPU.SetUp(cpu);
        LWeapon.SetUp(lweapon);
        RWeapon.SetUp(rweapon);
        Movement.SetUp(movement);
        return this;
    }

    public RunTimeResult OnTurn(TMP_Text terminal){
        RTContext tempRTC = new RTContext(new SymbolTable(null), new TraceBack("Main"));
        Dictionary<string, Func<List<dynamic>, GameContext, dynamic>> funcs = new();

        Main.MergeDicts(funcs, Main.Funcs);
        Main.MergeDicts(funcs, _assembler.GetFuncs());

        GameContext gameContext = new GameContext(this, terminal, funcs, _manager);
        if(AST is null) return new RunTimeResult();
        return AST.Node.Visit(ref tempRTC, ref gameContext);
    }
    
    public void Move(int value){
        print(value);
    }
}
