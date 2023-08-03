using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    public static List<FuncType> FuncTypes;
    public static Dictionary<string, Func<List<dynamic>, GameContext, dynamic>> Funcs;

    void Start()
    {
        SpriteRegistrar SR = GetComponent<SpriteRegistrar>();
        SR.RegisterComponents();
        RegisterFunctions();
    } 

    private static void RegisterFunctions(){
        FuncTypes = new();
        Funcs = new();

        RegisterFunc("Print", Type.Void, new List<Type>(), 
            delegate(List<dynamic> paramVals, GameContext context){

                string output = String.Empty;
                foreach(var value in paramVals){
                    output += paramVals.ToString();
                }
                context.Terminal.text += output + "\n";
                return null;

            });
    }

    public static void RegisterFunc(string name, Type type, List<Type> paramTypes, Func<List<dynamic>, GameContext, dynamic> func){
        FuncTypes.Add(new FuncType(name, type, paramTypes));
        Funcs.Add(name, func);
    }

    public static void MergeDicts(Dictionary<string, Func<List<dynamic>, GameContext, dynamic>> original, Dictionary<string, Func<List<dynamic>, GameContext, dynamic>> addition){
        foreach(var p in addition){
            original.Add(p.Key, p.Value);
        }
    }
}
