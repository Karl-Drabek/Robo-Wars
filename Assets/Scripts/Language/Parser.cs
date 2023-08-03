using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//TODO replace all Type with TokenType,
// Generalize idContext

//Used in try register to include a ref IDContext parameter for a function as a parameter
public delegate ParseResult ParseFunction(ref IDContext idContext);

public struct FuncType{
    public string Name;
    public Type Type;
    public List<Type> ParamTypes;

    public FuncType(string name, Type type, List<Type> paramTypes){
        (Name, Type, ParamTypes) = (name, type, paramTypes);
    }
}

//Contains a built in Type and can also be a list
public class Type : IEquatable<Type>
{
    //readonly types for equatable reference
    public static readonly Type Int = new Type(DataType.Int);
    public static readonly Type Double = new Type(DataType.Double);
    public static readonly Type Bool = new Type(DataType.Bool);
    public static readonly Type String = new Type(DataType.String);
    public static readonly Type Void = new Type(DataType.Void);

    public DataType DT;
    public bool IsList;

    public Type(DataType dt, bool isList = false){
        DT = dt;
        IsList = isList;
    }

    public override string ToString() => $"{DT}{(IsList? "[]" : string.Empty)}";

    public bool Equals(Type other) => (this.DT == other.DT) && (this.IsList == other.IsList);
    
    public override bool Equals(System.Object obj){
        if (obj == null) return false;
        Type typeObj = obj as Type;
        if (typeObj == null) return false;
        return Equals(typeObj);
   }

   public override int GetHashCode() => Tuple.Create(DT, IsList).GetHashCode();

   public static bool operator == (Type type1, Type type2)
   {
      if (((object)type1) == null || ((object)type2) == null)
         return System.Object.Equals(type1, type2);

      return type1.Equals(type2);
   }

   public static bool operator != (Type type1, Type type2)
   {
      if (((object)type1) == null || ((object)type2) == null)
         return !System.Object.Equals(type1, type2);

      return !type1.Equals(type2);
   }
}

//non-list derivitives of Type
public enum DataType{Int, Double, String, Bool, Void}

//Carries the variable information with a scope
public class IDContext
{
    public Dictionary<string, Type> Variables;
    public Dictionary<string, Type> Functions;
    public Dictionary<string, List<Type>> FuncParams;
    public IDContext Parent;

    public IDContext(IDContext parent = default){
        Variables = new Dictionary<string, Type>();
        Functions = new Dictionary<string, Type>();
        FuncParams = new Dictionary<string, List<Type>>();
        Parent = parent;
    }

    //Gets the type of a variable given a identifier
    public (bool, Type) GetVar(string identifier){ 
        IDContext tempContext = this;
        while(!tempContext.Variables.ContainsKey(identifier)){
            if(tempContext.Parent is not null){
                tempContext = tempContext.Parent;
                continue;
            }
            return (false, Type.Void);
        }
        return (true, tempContext.Variables[identifier]);
    }

    //Gets the return type of a function given a identifier
    public (bool, Type, List<Type>) GetFunc(string identifier){ 
        IDContext tempContext = this;
        while(!tempContext.Functions.ContainsKey(identifier)){
            if(tempContext.Parent is not null){
                tempContext = tempContext.Parent;
                Debug.Log(tempContext is null);
                continue;
            }
            return (false, Type.Void, null);
        }
        return (true, tempContext.Functions[identifier], tempContext.FuncParams[identifier]);
    }


    /*returns false if the variable does not exisit in the current idContext,
    otherwise it adds the variable to the dictionary in the given idContext.*/
    public bool DefVar(string identifier, Type type){
        if(Variables.ContainsKey(identifier)) return false;
        Variables.Add(identifier, type);
        return true;
    }

    
    
    /*returns false if the function does not exisit in the current idContext,
    otherwise it adds the functoin to the dictionary in the given idContext.*/
    public bool DefFunc(string identifier, Type type, List<Type> parameters){
        if(Functions.ContainsKey(identifier)) return false;
        Functions.Add(identifier, type);
        FuncParams.Add(identifier, parameters);
        return true;
    }

    //checks if the function exists in the current context
    public bool IsFunc(string identifier) => Functions.ContainsKey(identifier);
}

//caries information regarding the parsing of all functions
public class ParseResult
{
    public Error Error;
    public Node Node;

    public ParseResult SetNode(Node node){
        Node = node;
        return this;
    }

    public ParseResult SetError(Error error){
        Error = error;
        return this;
    }

    public Node Register(ParseResult result){
        if(result.Error is not null) Error = result.Error;
        return result.Node;
    }
}

//Takes a list of tokens and returns an abstract syntax tree
public class Parser
{
    private static List<Token> _tokens;
    private static int _index;
    private static Token _currentToken;
    public static int AdvanceCount;
    public static List<string> BuiltInFuncs;

    private static void Advance(){
        _index++;
        AdvanceCount++;
        if (_currentToken.Type != TokenType.EOF){
            _currentToken = _tokens[_index];
        }
        //Debug.Log(_currentToken.Type); //for play by play debugging
    }

    private static void Retreate(int amount){
        _currentToken = _tokens[_index -= amount];
        //Debug.Log($"Retreate to {_currentToken.Type}"); //for play by play debugging
    }

    /*Attempts to normally register the function within the idContext,
    but if an error is presented it returns null and retreates to the original location*/
    private static Node TryRegister(ref IDContext idContext, ParseFunction function){
        var result = new ParseResult();

        int localAdvanceCount = AdvanceCount;
        Node node = result.Register(function(ref idContext));
        if(result.Error is not null){
            Retreate(AdvanceCount - localAdvanceCount);
            return null;
        }
        return node;
    }

    //This Function starts the parsing process
    public static ParseResult Parse(List<Token> tokens, List<FuncType> funcs){
        _tokens = tokens;
        _index = -1;
        AdvanceCount = 0;
        _currentToken = tokens[0];
        Advance();

        IDContext idContext = new();
        foreach(var f in funcs){idContext.DefFunc(f.Name, f.Type, f.ParamTypes);}

        return Statements(idContext);
    }

    //Statements is an entire body of code
    private static ParseResult Statements(IDContext idContext){
        var result = new ParseResult();
        Position posStart = _currentToken.PosStart.Copy();
        var nodes = new List<Node>();
        Node node;

        while(_currentToken.Type != TokenType.EOF){
            node = result.Register(Statement(ref idContext));
            if(result.Error is not null) return result;
            nodes.Add(node);
        }
        return result.SetNode(new StatementsNode(nodes, posStart, _currentToken.PosEnd.Copy()));
    }

    //A statement is a single line of code which ends in a semi-colon
    private static ParseResult Statement(ref IDContext idContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node node;
        switch (_currentToken.Type){
            case TokenType.Repeat:
                node = result.Register(RepeatStatement(ref idContext));
                if(result.Error is not null) return result;
                break;
            case TokenType.Loop:
                node = result.Register(LoopStatement(ref idContext));
                if(result.Error is not null) return result;
                break;
            case TokenType.While:
                node = result.Register(WhileStatement(ref idContext));
                if(result.Error is not null) return result;
                break;
            case TokenType.Func:
                node = result.Register(DefFunc(ref idContext));
                if(result.Error is not null) return result;
                break;
            case TokenType.IntType:
            case TokenType.DoubleType:
            case TokenType.BoolType:
            case TokenType.StringType:
                node = result.Register(DefVar(ref idContext));
                if(result.Error is not null) return result;
                break;   
            //Return may be standalone or be follow by an expresion to return
            case TokenType.Return:
                Advance();
                Node expresion = TryRegister(ref idContext, Expr);
                if(expresion is not null){
                    node = new JumpNode(TokenType.Return, posStart, _currentToken.PosEnd.Copy(), expresion);
                } else{
                   node = new JumpNode(TokenType.Return, posStart, _currentToken.PosEnd.Copy());
                }
                break;
            case TokenType.Continue:
                Advance();
                node = new JumpNode(TokenType.Continue, posStart, _currentToken.PosEnd.Copy());
                break;
            case TokenType.Break:
                Advance();
                node = new JumpNode(TokenType.Break, posStart, _currentToken.PosEnd.Copy());
                break;
            case TokenType.If:
            case TokenType.Try:
                node = TryRegister(ref idContext, ConditionStatement);
                if(node is null) goto default;
                break;
            default:
                node = result.Register(Expr(ref idContext));
                if(result.Error is not null) return result;
                break;
        }
        if(_currentToken.Type != TokenType.SC){
            return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Expected Character", "\";\""));
        }
        Advance();
        return result.SetNode(node);
    }

    //The repeat statement takes an integer value, and a block of code to reapeat that number of times
    private static ParseResult RepeatStatement(ref IDContext idContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node expr = null;
        Node statements = null;

        Advance();
        expr = result.Register(Expr(ref idContext));
        if(result.Error is not null) return result;
        BSB(ref statements, ref result, new IDContext(idContext));
        if(result.Error is not null) return result;
        return result.SetNode(new RepeatNode(expr, statements, posStart, _currentToken.PosEnd.Copy()));
    }

    /*The loop statement offers the user anexpresion that is run at the start,
    another that must evaluate as bolean that decides whether to continue the loop,
    another that is run at the end of every loop, and a final block under the loop that is run every time*/
    private static ParseResult LoopStatement(ref IDContext idContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        var expresions = new Node[3];
        Node statements = null;

        Advance();
        if(_currentToken.Type != TokenType.LParen){
            return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Expected Character", "\"(\""));
        }
        Advance();

        //creating syntax for the first three expresions
        for(int i = 0; i < 3; i++){
            expresions[i] = result.Register(Expr(ref idContext));
            if(result.Error is not null) return result;
            if(i == 1 && expresions[i].Type != Type.Bool){
                return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Type Error", "Second Expresion of Loop Statement must be of type Bool"));
            }
            if(i < 2){
                if(_currentToken.Type != TokenType.Comma){
                    return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Expected Character", "\",\""));
                }
                Advance();
            }
        }
        if(_currentToken.Type != TokenType.RParen){
            return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Expected Character", "\")\""));
        }
        Advance();
        BSB(ref statements, ref result, new IDContext(idContext));
        if(result.Error is not null) return result;
        return result.SetNode(new LoopNode(expresions, statements, posStart, _currentToken.PosEnd.Copy()));
    }

    //runs the statements so long as the condition evaluates true
    private static ParseResult WhileStatement(ref IDContext idContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node expr = null;
        Node statements = null;

        Advance();
        expr = result.Register(Expr(ref idContext));
        if(result.Error is not null) return result;
        if(expr.Type != Type.Bool){
            return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Type Error", "Second Expresion of Loop Statement must be of type Bool"));
        }
        BSB(ref statements, ref result, new IDContext(idContext));
        if(result.Error is not null) return result;
        return result.SetNode(new WhileNode(expr, statements, posStart, _currentToken.PosEnd.Copy()));
    }

    //defines a function with a return type, set of parameters, and block of code to run
    private static ParseResult DefFunc(ref IDContext idContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        (Error Error, Type Type) typeRes;
        (Error Error, Type Type) tempTypeRes;
        string id;
        Node statements = null;
        List<Type> paramTypes = new List<Type>();
        List<string> paramIds = new List<string>();

        Advance();
        typeRes = ToType(_currentToken.Type);
        if(typeRes.Error is not null) return result.SetError(typeRes.Error);
        Advance();
        if(_currentToken.Type != TokenType.Identifier) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Expected Identifier"));
        id = _currentToken.Value;
        if(!idContext.IsFunc(id)) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Error", $"The function {id} already exists in the current idContext"));
        Advance();
        if(_currentToken.Type != TokenType.LParen) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Expected \"(\""));
        //get parameters
        Advance();
        if((tempTypeRes = ToType(_currentToken.Type)).Error is not null) goto End;
        paramTypes.Add(tempTypeRes.Type);
        Advance();
        if(_currentToken.Type != TokenType.Identifier) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Expected Identifier"));
        paramIds.Add(_currentToken.Value);
        Advance();
        while(_currentToken.Type == TokenType.Comma){
            Advance();
            if((tempTypeRes = ToType(_currentToken.Type)).Error is not null) return result.SetError(tempTypeRes.Error);
            Advance();
            if(_currentToken.Type != TokenType.Identifier) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Expected Identifier"));
            paramIds.Add(_currentToken.Value);
            Advance();
        }
        idContext.DefFunc(id, typeRes.Type, paramTypes);
        End:
        if(_currentToken.Type != TokenType.RParen) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Expected \")\""));
        Advance();
        BSB(ref statements, ref result, new IDContext(idContext));
        if(result.Error is not null) return result;
        return result.SetNode(new DefFuncNode(typeRes.Type, id, paramTypes, paramIds, statements, posStart, _currentToken.PosEnd.Copy()));
    }

    //defines a variable with a type
    private static ParseResult DefVar(ref IDContext idContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        string identifier;
        Type type;

        type = (ToType(_currentToken.Type)).Item2;
        if(_currentToken.Type != TokenType.Identifier) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Expected Identifier"));
        identifier = _currentToken.Value;
        Advance();
        if(!idContext.DefVar(identifier, type)) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Identifier Error", $"The variable {identifier} already exists in the current Context"));
        return result.SetNode(new DefVarNode(type, identifier, posStart, _currentToken.PosEnd.Copy()));
    }

    //Binary and/or expresion
    private static ParseResult Expr(ref IDContext idContext){
        return BinOp(CompExprA, new TokenType[] {TokenType.And, TokenType.Or}, ref idContext);
    }

    //Unary not expresion
    private static ParseResult CompExprA(ref IDContext idContext){
        return UnOp(CompExprB, CompExprA, TokenType.Not, ref idContext);
    }

    //Binary equals/not equals/greater than/less than/greater than or equal to/less than or equal to expresion
    private static ParseResult CompExprB(ref IDContext idContext){
        return BinOp(ArithExprA, new TokenType[] {TokenType.EE, TokenType.NE, TokenType.GT, TokenType.LT, TokenType.GTE, TokenType.LTE}, ref idContext);
    }

    //Binary plus/minus expresion
    private static ParseResult ArithExprA(ref IDContext idContext){
        return BinOp(ArithExprB, new TokenType[] {TokenType.Plus, TokenType.Minus}, ref idContext);
    }

    //Binary mulitply/devide expresion
    private static ParseResult ArithExprB(ref IDContext idContext){
        return BinOp(PowExpr, new TokenType[] {TokenType.Multiply, TokenType.Divide}, ref idContext);
    }

    //Binary power expresion
    private static ParseResult PowExpr(ref IDContext idContext){
        return BinOp(NegExpr, new TokenType[] {TokenType.Power}, ref idContext);
    }

    //Unary negitive expresion 
    private static ParseResult NegExpr(ref IDContext idContext){
        return UnOp(Atom, NegExpr, TokenType.Minus, ref idContext);
    }

    //Base level of the syntax tree
    private static ParseResult Atom(ref IDContext idContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node node;
        string value = _currentToken.Value;
        switch(_currentToken.Type){
            case TokenType.Int:
                Advance();
                return result.SetNode(new IntNode(int.Parse(value), posStart, _currentToken.PosEnd.Copy()));
            case TokenType.Double:
                Advance();
                return result.SetNode(new DoubleNode(double.Parse(value), posStart, _currentToken.PosEnd.Copy()));
            case TokenType.String:
                Advance();
                return result.SetNode(new StringNode(value, posStart, _currentToken.PosEnd.Copy()));
            case TokenType.Bool:
                Advance();
                return result.SetNode(new BoolNode(bool.Parse(value), posStart, _currentToken.PosEnd.Copy()));
            case TokenType.LBrace:
                node = result.Register(ListExpr(ref idContext));
                if(result.Error is not null) return result;
                return result.SetNode(node);
            //Identifier could mean the user is setting or referencing a variable or calling a list or function
            case TokenType.Identifier:
                node = TryRegister(ref idContext, FuncCall);
                node ??= TryRegister(ref idContext, ListCall);
                node ??= TryRegister(ref idContext, SetVar);
                if(node is null){
                    (bool Exists, Type Type) resType = idContext.GetVar(_currentToken.Value);
                    if(!resType.Exists) return result.SetError(new Error(posStart, _currentToken.PosEnd, "Identifier Error", $"The variable {_currentToken.Value} does not exist in the current idContext"));
                    node = new IdentifierNode(value, posStart, _currentToken.PosEnd.Copy(), resType.Type);
                    Advance();
                }
                return result.SetNode(node);
            case TokenType.If:
            case TokenType.Try:
                return ConditionExpr(ref idContext);
            //Parenthesis bring it up to the expresion level
            case TokenType.LParen:
                Advance();
                node = result.Register(Expr(ref idContext));
                if(result.Error is not null) return result;
                if(_currentToken.Type == TokenType.RParen){
                    Advance();
                    return result.SetNode(node);
                }
                Advance();
                return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax", "Expected \")\""));
            default:
                Advance();
                return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax", "Expected Value, Identifier, List, Object or \"(\""));
        }
    }

    private static ParseResult ListExpr(ref IDContext idContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node tempNode;
        List<Node> nodes = new();
        Type type;

        Advance();
        tempNode = result.Register(Expr(ref idContext));
        if(result.Error is not null) return result;
        type = tempNode.Type;
        nodes.Add(tempNode);
        while(_currentToken.Type == TokenType.Comma){
            Advance();
            tempNode = result.Register(Expr(ref idContext));
            if(result.Error is not null) return result;
            if(tempNode.Type != type) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Type Syntax", "All values of the list must be of the same type"));
            nodes.Add(tempNode);
        }
        return result.SetNode(new ListExprNode(nodes, posStart, _currentToken.PosEnd.Copy(), type));
    }

    //Calls a function by passing parameters
    private static ParseResult FuncCall(ref IDContext idContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        string id;
        Node expr;
        List<Node> exprs = new();
        (bool Exists, Type Type, List<Type> Parameters) typeRes;

        id = _currentToken.Value;
        typeRes = idContext.GetFunc(id);
        if(!typeRes.Exists) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Identifier Error", $"The function {id} does not exist in the current context"));
        Advance();
        if(_currentToken.Type != TokenType.LParen) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Expected \"(\""));
        Advance();
        if(_currentToken.Type == TokenType.RParen) goto End;
        int i = 0;
        do{
            if(id != "Print" && i >= typeRes.Parameters.Count) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid Argument Error", $"The Function{id} only takes {typeRes.Parameters.Count} arguments"));
            if(i != 0) Advance();
            expr = result.Register(Expr(ref idContext));
            if(result.Error is not null) return result;
            if(id != "Print" && typeRes.Parameters[i] != (expr.Type)) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Type Error", $"Parameter {i} of {id} must be of type {typeRes.Parameters[i]}"));
            exprs.Add(expr);
            i++;
        }while(_currentToken.Type == TokenType.Comma);
        if(i < typeRes.Parameters.Count) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid Argument Error", $"The Function {id} Must have at least {typeRes.Parameters.Count} arguments"));
        if(_currentToken.Type != TokenType.RParen) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Expected \")\""));
        End:
        Advance();
        return result.SetNode(new FuncCallNode(id, exprs, posStart, _currentToken.PosEnd, typeRes.Type));
    }

    //Accesses elements of a list with dynamic types
    private static ParseResult ListCall(ref IDContext idContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        string id;
        Node expr;

        id = _currentToken.Value; 
        (bool Exists, Type Type) typeRes = idContext.GetVar(id);
        if(!typeRes.Exists) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Identifier Error", $"The variable {id} does not exist in the current context"));
        if(!typeRes.Type.IsList) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Type Error", $"Cannot call a list value from a non-list type"));
        Advance();
        if(_currentToken.Type != TokenType.LBrack) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Expected \"[\""));
        Advance();
        expr = result.Register(Expr(ref idContext));
        if(result.Error is not null) return result;
        if(_currentToken.Type != TokenType.RBrack) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Expected \"]\""));
        Advance();
        return result.SetNode(new ListCallNode(id, expr, posStart, _currentToken.PosEnd, new Type(typeRes.Type.DT))); 
    }

    //Sets the value of a variable and returns the value of the variable
    private static ParseResult SetVar(ref IDContext idContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node expr, node;
        TokenType op;
        string id;
        (bool Exists, Type Type) typeRes;

        id = _currentToken.Value;
        typeRes = idContext.GetVar(id);
        if(!typeRes.Exists) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Identifier Error", $"The variable {id} does not exists in the current context"));
        Advance();
        if(_currentToken.Type == TokenType.PP | _currentToken.Type == TokenType.MM){
            op = _currentToken.Type;
            if(typeRes.Type != Type.Int || typeRes.Type != Type.Double) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Assignment Error", $"The operator {op} does not take a parameter of type {typeRes.Type}"));
            Advance();
            node = new SetVarNode(id, op, posStart, _currentToken.PosEnd.Copy(), typeRes.Type);
        }else if(_currentToken.Type == TokenType.EQ
            | _currentToken.Type == TokenType.EPlus
            | _currentToken.Type == TokenType.EMinus
            | _currentToken.Type == TokenType.EMult
            | _currentToken.Type == TokenType.EDiv
            | _currentToken.Type == TokenType.EPow){
            op = _currentToken.Type;
            Advance();
            expr = result.Register(Expr(ref idContext));
            if(result.Error is not null) return result;
            if(expr.Type != typeRes.Type) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Type Error", $"Cannot assign type {expr.Type} to variable of type {typeRes.Type}"));
            node = new SetVarNode(id, op, posStart, _currentToken.PosEnd.Copy(), typeRes.Type, expr);
        }else return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax", "Expected \"=\", \"=+\", \"=-\", \"=*\", \"=/\", \"=^\", \"++\" or \"--\" "));
        return result.SetNode(node);
    }

    public static (Error error, Type type) BinOpType(Type left, TokenType op, Type right){
        switch(op){
            case TokenType.Plus:
            case TokenType.EPlus:
                if(left == Type.Int){
                    if(right == Type.Int) return (null, Type.Int);
                    else if(right == Type.Double) return (null, Type.Double);
                    else if(right == Type.String) return (null, Type.String);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
                }else if(left == Type.Double){
                    if(right == Type.Int || right == Type.Double) return (null, Type.Double);
                    else if(right == Type.String) return (null, Type.String);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
                }else if(left == Type.String){
                    if(right == Type.Int || right == Type.Double || right == Type.String) return (null, Type.String);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
                }else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
            case TokenType.Minus:
            case TokenType.EMinus:
            case TokenType.Multiply:
            case TokenType.EMult:
            case TokenType.Power:
            case TokenType.EPow:
                if(left == Type.Int){
                    if(right == Type.Int) return (null, Type.Int);
                    else if(right == Type.Double) return (null, Type.Double);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
                }else if(left == Type.Double){
                    if(right == Type.Int || right == Type.Double) return (null, Type.Double);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
                }else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
            case TokenType.Divide:
            case TokenType.EDiv:
                if(left == Type.Int){
                    if(right == Type.Int || right == Type.Double) return (null, Type.Double);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
                }else if(left == Type.Double){
                    if(right == Type.Int || right == Type.Double) return (null, Type.Double);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
                }else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
            case TokenType.EE:
            case TokenType.NE:
                if(left == Type.Int){
                    if(right == Type.Int || right == Type.Double) return (null, Type.Bool);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
                }else if(left == Type.Double){
                    if(right == Type.Int || right == Type.Double) return (null, Type.Bool);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
                }else if(left == Type.String){
                    if(right == Type.String) return (null, Type.Bool);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
                }else if(left == Type.Bool){
                    if(right == Type.Bool) return (null, Type.Bool);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
                }else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
            case TokenType.GT:
            case TokenType.LT:
            case TokenType.GTE:
            case TokenType.LTE:
                if(left == Type.Int){
                    if(right == Type.Int || right == Type.Double) return (null, Type.Bool);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
                }else if(left == Type.Double){
                    if(right == Type.Int || right == Type.Double) return (null, Type.Bool);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
                }else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
            default:
                return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid synttax", $"Cannot apply operand {op} to types {left} and {right}"), Type.Void);
        }
    }

    private static ParseResult BinOp(ParseFunction child, TokenType[] tokenTypes, ref IDContext idContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node left, right;
        TokenType op = default;
        (Error error, Type type) res;

        left = result.Register(child(ref idContext));
        if(result.Error is not null) return result;
        while(Array.Exists(tokenTypes, TT => TT == _currentToken.Type)){
            op = _currentToken.Type;
            Advance();
            right = result.Register(child(ref idContext));
            if(result.Error is not null) return result;
            res = BinOpType(left.Type, op, right.Type);
            if(res.error is not null) return result.SetError(res.error);
            left = new BinOpNode(left, op, right, posStart, _currentToken.PosEnd.Copy(), res.type);
        }
        return result.SetNode(left);
    }

    private static ParseResult UnOp(ParseFunction child, ParseFunction current, TokenType tokenType, ref IDContext idContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node node;
        Type type = Type.Void;

        if(_currentToken.Type == tokenType){
            Advance();
            node = result.Register(current(ref idContext));
            if(result.Error is not null) return result;
            switch(tokenType){
                case TokenType.Minus:
                    if(node.Type == Type.Int) type = Type.Int;
                    else if(node.Type == Type.Double) type = Type.Double;
                    else return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {tokenType} to type {node.Type}"));
                    break;
                case TokenType.Not:
                    if(node.Type == Type.Bool) type = Type.Bool;
                    else return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {tokenType} to type {node.Type}"));
                    break;
            }
            node = new UnOpNode(tokenType, node, posStart, _currentToken.PosEnd.Copy(), type);
            return result.SetNode(node);
        }
        return child(ref idContext);
    }

    private static ParseResult ConditionStatement(ref IDContext idContext){
        Position posStart = _currentToken.PosStart.Copy();
        Position tempPosStart;
        var result = new ParseResult();
        Node expr, statements, elExpr, elStatements, elseStatements;
        expr = statements = elExpr = elStatements = elseStatements = default;
        List<Node> elExpresionList = default;
        bool isIf;

        if(_currentToken.Type == TokenType.If){
            tempPosStart = _currentToken.PosStart.Copy();
            Advance();
            expr = result.Register(Expr(ref idContext));
            if(result.Error is not null) return result;
            if(expr.Type != Type.Bool) return result.SetError(new Error(tempPosStart, _currentToken.PosEnd.Copy(), "Type Error", "If Statement Expresion must be of type Bool"));
            BSB(ref statements, ref result, new IDContext(idContext));
            if(result.Error is not null) return result;
            expr = new IfNode(expr, statements, tempPosStart, _currentToken.PosEnd.Copy());
        } else {
            tempPosStart = _currentToken.PosStart.Copy();
            Advance();
            BSB(ref statements, ref result, new IDContext(idContext));
            if(result.Error is not null) return result;
            expr = new TryNode(statements, posStart, _currentToken.PosEnd.Copy());
        }
        if((isIf = _currentToken.Type == TokenType.Elif) || _currentToken.Type == TokenType.Eltry){
            elExpresionList = new List<Node>();
            do{
                if(isIf){
                    tempPosStart = _currentToken.PosStart.Copy();
                    Advance();
                    elExpr = result.Register(Expr(ref idContext));
                    if(result.Error is not null) return result;
                    if(elExpr.Type != Type.Bool) return result.SetError(new Error(tempPosStart, _currentToken.PosEnd.Copy(), "Type Error", "If Statement Expresion must be of type Bool"));
                    BSB(ref elStatements, ref result, new IDContext(idContext));
                    if(result.Error is not null) return result;
                    elExpr = new IfNode(elExpr, elStatements, tempPosStart, _currentToken.PosEnd.Copy());
                } else {
                    tempPosStart = _currentToken.PosStart.Copy();
                    Advance();
                    BSB(ref elStatements, ref result, new IDContext(idContext));
                    if(result.Error is not null) return result;
                    elExpr = new TryNode(elStatements, posStart, _currentToken.PosEnd.Copy());
                }
                elExpresionList.Add(elExpr);
            }while((isIf = _currentToken.Type == TokenType.Elif) || _currentToken.Type == TokenType.Eltry);
        }
        if(_currentToken.Type == TokenType.Else){
            Advance();
            BSB(ref elseStatements, ref result, new IDContext(idContext));
            if(result.Error is not null) return result;
        }
        return result.SetNode(new ConditionNode(expr, elExpresionList, elseStatements, posStart, _currentToken.PosStart.Copy()));
    }

    private static ParseResult ConditionExpr(ref IDContext idContext){
        Position posStart = _currentToken.PosStart.Copy();
        Position tempPosStart;
        var result = new ParseResult();
        Node conditionExpr = default, returnExpr = default, elConditionExpr = default, elReturnExpr = default, elseExpr = default;
        List<Node> elExpresionList = default;
        bool isIf;

        if(_currentToken.Type == TokenType.If){
            tempPosStart = _currentToken.PosStart.Copy();
            Advance(); 
            conditionExpr = result.Register(Expr(ref idContext));
            if(result.Error is not null) return result;
            if(conditionExpr.Type != Type.Bool) return result.SetError(new Error(tempPosStart, _currentToken.PosEnd.Copy(), "Type Error", "If expresion's conditional expresion must be of type Bool"));
            returnExpr = result.Register(Expr(ref idContext));
            if(result.Error is not null) return result;
            returnExpr = new IfExprNode(conditionExpr, returnExpr, tempPosStart, _currentToken.PosEnd.Copy());
        } else {
            tempPosStart = _currentToken.PosStart.Copy();
            Advance();
            returnExpr = result.Register(Expr(ref idContext));
            if(result.Error is not null) return result;
            returnExpr = new TryExprNode(returnExpr, posStart, _currentToken.PosEnd.Copy());
        }
        if((isIf = _currentToken.Type == TokenType.Elif) || _currentToken.Type == TokenType.Eltry){
            elExpresionList = new List<Node>();
            do{
                if(isIf){
                    tempPosStart = _currentToken.PosStart.Copy();
                    Advance(); 
                    elConditionExpr = result.Register(Expr(ref idContext));
                    if(result.Error is not null) return result;
                    if(elConditionExpr.Type != Type.Bool) return result.SetError(new Error(tempPosStart, _currentToken.PosEnd.Copy(), "Type Error", "If expresion's conditional expresion must be of type Bool"));
                    elReturnExpr = result.Register(Expr(ref idContext));
                    if(result.Error is not null) return result;
                    elReturnExpr = new IfExprNode(elConditionExpr, elReturnExpr, tempPosStart, _currentToken.PosEnd.Copy());
                    if(elReturnExpr.Type != returnExpr.Type) return result.SetError(new Error(tempPosStart, _currentToken.PosEnd.Copy(), "Type Error", "If Expresion's results must all be of the same type"));
                } else {
                    tempPosStart = _currentToken.PosStart.Copy();
                    Advance();
                    returnExpr = result.Register(Expr(ref idContext));
                    if(result.Error is not null) return result;
                    returnExpr = new TryExprNode(returnExpr, posStart, _currentToken.PosEnd.Copy());
                }
                elExpresionList.Add(elReturnExpr);
            }while((isIf = _currentToken.Type == TokenType.Elif) || _currentToken.Type == TokenType.Eltry);
        }
        if(_currentToken.Type == TokenType.Else){
            posStart = _currentToken.PosStart.Copy();
            Advance();
            elseExpr = result.Register(Expr(ref idContext));
            if(result.Error is not null) return result;
            if(elseExpr.Type != returnExpr.Type){
                return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Type Error", "If Expresion's results must all be of the same type"));
            }
            return result.SetNode(new ConditionExprNode(returnExpr, elExpresionList, elseExpr, posStart, _currentToken.PosStart.Copy()));
        }
        else{
            return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", "Conditional expresions must have a catching else exspresion"));
        }
    }

    private static void BSB(ref Node statements, ref ParseResult result, IDContext idContext){
        Position posStart = _currentToken.PosStart.Copy();
        Node statement;
        var localStatments = new List<Node>();

        if(_currentToken.Type != TokenType.LBrace){
            result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", "Expected \"{\""));
            return;
        }
        Advance();
        while(_currentToken.Type != TokenType.RBrace){
            statement = result.Register(Statement(ref idContext));
            if(result.Error is not null) return;
            localStatments.Add(statement);
        }
        Advance();
        statements = new StatementsNode(localStatments, posStart, _currentToken.PosEnd.Copy());
    }

    private static Dictionary<T, U> ListsToDict<T, U>(List<T> listT, List<U> listU){
        if(listT.Count != listU.Count) return null;
        Dictionary<T, U> result = new();
        for(int i = 0; i < listT.Count; i++){
            result.Add(listT[i], listU[i]);
        }
        return result;
    }

    private static (Error, Type) ToType(TokenType tt){
        Position posStart = _currentToken.PosStart.Copy();
        DataType dt;

        switch (tt){
            case TokenType.IntType:
                dt = DataType.Int;
                break;
            case TokenType.DoubleType:
                dt = DataType.Double;
                break;
            case TokenType.BoolType:
                dt = DataType.Bool;
                break;
            case TokenType.StringType:
                dt = DataType.String;
                break;
            default:
                return (new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Type", "Expected 'bool', 'int', 'string', 'double' or list"), null);
        }
        Advance();
        if(_currentToken.Type != TokenType.LBrace){return (null, new Type(dt));}
        Advance();
        if(_currentToken.Type != TokenType.RBrace){return (new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid syntax", "Expected \"]\""), null);}
        return (null, new Type(dt, true));
    }
}
