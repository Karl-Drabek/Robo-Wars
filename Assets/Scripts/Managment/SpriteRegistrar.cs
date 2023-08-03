using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SpriteID{
    Movement = 0,
    Weapon = 1,
    Sensor = 2,
    CPU = 3
}

public class SpriteRegistrar : MonoBehaviour
{

    public List<Sprite> Sprites;
    public static List<CPU> CPUs;
    public static List<Weapon> Weapons;
    public static List<Movement> Movements;
    public static List<Sensor> Sensors;
    public static SpriteRegistrar Registrar;

    public void RegisterComponents(){
        Registrar = this;

        CPUs = new();
        Weapons = new();
        Movements = new();
        Sensors = new();

        RegisterSensors();
        RegisterCPUs();
        RegisterWeapons();
        RegisterMovements();
    }

    public static void RegisterSensors(){
        Sensors.Add(new Sensor("Laser Pointer",
            new List<FuncType>(){
                new FuncType("Scan", Type.Int, new List<Type>(){Type.Int})
                },
            new Dictionary<string, Func<List<dynamic>, GameContext, dynamic>>(){
                {"Scan", LaserPointerScan}
                },
            Registrar.Sprites[(int)SpriteID.Sensor]));
    }

    public static void RegisterCPUs(){
        CPUs.Add(new CPU("2", new List<FuncType>(), new Dictionary<string, Func<List<dynamic>, GameContext, dynamic>>(), Registrar.Sprites[(int)SpriteID.CPU]));
    }

    public static void RegisterWeapons(){
        Weapons.Add(new Weapon("3", new List<FuncType>(), new Dictionary<string, Func<List<dynamic>, GameContext, dynamic>>(), Registrar.Sprites[(int)SpriteID.Weapon]));
    }

    public static void RegisterMovements(){
        Movements.Add(new Movement("4", new List<FuncType>(), new Dictionary<string, Func<List<dynamic>, GameContext, dynamic>>(), Registrar.Sprites[(int)SpriteID.Movement]));
    }

    public static dynamic LaserPointerScan(List<dynamic> gameParameters, GameContext context){ // bool

        return null;
    }
}
