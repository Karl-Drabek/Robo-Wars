using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RunTimeResult
{
    public Type Type;
    public dynamic Value;
    public Error Error;
    public bool HasReturn;
    public bool HasContinue;
    public bool HasBreak;

    public RunTimeResult(){
        Type = Value = Error = null;
        HasReturn = HasContinue = HasBreak = false;
    }

    public dynamic Register(RunTimeResult result){
        Error = result.Error;
        HasReturn = result.HasReturn;
        HasContinue = result.HasContinue;
        HasBreak = result.HasBreak;
        return result.Value;
    }

    public RunTimeResult SetType(Type type){
        Type = type;
        return this;
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

    public bool ShouldReturn() => Error is not null || HasReturn || HasContinue || HasBreak;

    public bool ShouldReturnLoop() => Error is not null || HasReturn;
}


public class RTContext
{
    public static readonly RTContext Empty = new RTContext(new SymbolTable(null), new TraceBack("Main"));
    public TraceBack TraceBack;
    public SymbolTable SymbolTable;

    public RTContext(SymbolTable symbolTable, TraceBack traceBack){
        SymbolTable = symbolTable;
        TraceBack = traceBack;
    }
}

public class TraceBack
{
    public TraceBack Parent;
    public string Name;
    public Position Position;

    public TraceBack(string name, TraceBack parent = null, Position position = null){
        Name = name;
        Parent = parent;
        Position = position;
    }
}

public class Function
{
    public Node Statements;
    public List<string> Parameters;
    public SymbolTable SymbolTable;

    public Function(Node statements, List<string> parameters, SymbolTable symbolTable){
        Statements = statements;
        Parameters = parameters;
        SymbolTable = symbolTable;
    }

    public RunTimeResult Call(List<dynamic> parameters, TraceBack traceBack, ref GameContext gameContext){
        RTContext rtContext = new RTContext(SymbolTable, traceBack);
        for(int i = 0; i < parameters.Count; i++){
            rtContext.SymbolTable.SetVar(Parameters[i], parameters[i]);
        }
        return new RunTimeResult().Register(Statements.Visit(ref rtContext, ref gameContext));
    }
}

public class SymbolTable
{
    public Dictionary<string, dynamic> Variables;
    public Dictionary<string, Function> Functions;
    public SymbolTable Parent;

    public SymbolTable(SymbolTable parent){
        Variables = new Dictionary<string, dynamic>();
        Parent = parent;
    }

    public void DefVar(string key){
        Variables.Add(key, null);
        return;
    }

    public void DefFunc(string key, Function func){
        Functions.Add(key, func);
        return;
    }

    public void SetVar(string key, dynamic value){
        SymbolTable tempST = this;
        while(!tempST.Variables.ContainsKey(key)){
            tempST = tempST.Parent;
        }
        tempST.Variables[key] = value;
        return;
    }

    public dynamic GetVar(string key){
        SymbolTable tempST = this;
        while(!tempST.Variables.ContainsKey(key)){
            tempST = tempST.Parent;
        }
        return tempST.Variables[key];
    }
    
    public Function GetFunc(string key){
        SymbolTable tempST = this;
        while(!tempST.Functions.ContainsKey(key)){
            tempST = tempST.Parent;
        }
        return tempST.Functions[key];
    }
}
