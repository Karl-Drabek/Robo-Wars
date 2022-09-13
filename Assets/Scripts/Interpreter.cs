using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RunTimeResult
{   
    public dynamic Value;
    public Error Error;
    public bool HasReturn;
    public bool HasContinue;
    public bool HasBreak;

    public dynamic Register(RunTimeResult result){
        Error = result.Error;
        HasReturn = result.HasReturn;
        HasContinue = result.HasContinue;
        HasBreak = result.HasBreak;
        return result.Value;
    }

    public RunTimeResult SetValue(dynamic value){
        Value = value;
        return this;
    }

    public RunTimeResult SetError(Error error){
        Error = error;
        return this;
    }

    public RunTimeResult SetReturn(bool hasReturn){
        HasReturn = hasReturn;
        return this;
    }

    public RunTimeResult SetContinue(bool hasContinue){
        HasContinue = hasContinue;
        return this;
    }

    public RunTimeResult SetBreak(bool hasBreak){
        HasBreak = hasBreak;
        return this;
    }
    
    public RunTimeResult Clear(){
        Value = null;
        Error = null;
        HasReturn = HasContinue = HasBreak = false;
        return this;
    }

    public bool ShouldReturn(){
        return Error is not null || HasReturn || HasContinue || HasBreak;
    }
}

public class Value
{
    

}

public class Function
{
    public List<string> Identifiers;
    public Node Statements;
    public SymbolTable SymbolTable;
    public Position Position;

    public Function(List<string> identifiers, Node statements, SymbolTable symbolTable){
        Identifiers = identifiers;
        Statements = statements;
        SymbolTable = symbolTable;
    }

}

public class Class
{
    public string Name;
    public List<Type> Types;
    public List<string> Identifiers, InheritedIdentifiers;
    public Node Statements;

    public Class(string name, List<Type> types, List<string> identifiers, List<string> inheritedIdentifiers, Node statements){
        Name = name;
        Types = types;
        Identifiers = identifiers;
        InheritedIdentifiers = inheritedIdentifiers;
        Statements = statements;
    }

}

public class ObjectValue
{
    public Dictionary<string, dynamic> Dictionary;

    public ObjectValue(){
        Dictionary = new();
    }

}

public class Context
{
    public TraceBack TraceBack;
    public SymbolTable SymbolTable;

    public Context(SymbolTable symbolTable, TraceBack traceBack){
        SymbolTable = symbolTable;
        TraceBack = traceBack;
    }
}

public class TraceBack
{
    public TraceBack Parent;
    public string Name;
    public Position StartPosition;

    public TraceBack(string name, TraceBack parent = null, Position startPosition = null){
        Name = name;
        Parent = parent;
        StartPosition = startPosition;
    }
}

public class SymbolTable
{
    public Dictionary<string, dynamic> Symbols;
    public SymbolTable Parent;

    public SymbolTable(SymbolTable parent){
        Symbols = new Dictionary<string, dynamic>();
        Parent = parent;
    }

    public bool Set(string key, dynamic value){
        Dictionary<string, dynamic> tempSymbols = Symbols;
        while(!tempSymbols.ContainsKey(key)){
            if(Parent is not null){
                tempSymbols = Parent.Symbols;
                continue; 
            }else return false;
        }
        tempSymbols[key] = value;
        return true;
    }

    public bool Contains(string key){
        Dictionary<string, dynamic> tempSymbols = Symbols;
        while(!tempSymbols.ContainsKey(key)){
            if(Parent is not null){
                tempSymbols = Parent.Symbols;
                continue; 
            }else return false;
        }
        return true;
    }

    public dynamic Get(string key){
        Dictionary<string, dynamic> tempSymbols = Symbols;
        while(!tempSymbols.ContainsKey(key) && Parent is not null){
            tempSymbols = Parent.Symbols;
            continue; 
        }
        return tempSymbols[key];
    }
}
