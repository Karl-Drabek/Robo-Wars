using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate ParseResult ParseFunction(ref TypeContext typeContext);

public class Type : IEquatable<Type>
{
    public static readonly Type IntType = new Type("Int");
    public static readonly Type DoubleType = new Type("Double");
    public static readonly Type StringType = new Type("String");
    public static readonly Type BoolType = new Type("Bool");

    public string BaseType;
    public List<Type> GenericTypes;

    public Type(string baseType, List<Type> genericTypes = default){
        BaseType = baseType;
        GenericTypes = genericTypes;
    }

    public bool Equals(Type type){
        if(this.BaseType != type.BaseType) return false;
        if(GenericTypes is null) return true;
        if(this.GenericTypes.Count != type.GenericTypes.Count) return false;
        for(int i = 0; i < GenericTypes.Count; i++){
            if(this.GenericTypes[i] != type.GenericTypes[i]) return false;
        }
        return true;
    }

    public override string ToString() => (GenericTypes is null)? BaseType : $"{BaseType}<{Node.CommaList(GenericTypes)}>";
}

public class TypeContext
{
    public Dictionary<string, Type> Variables;
    public Dictionary<string, int> Types;
    public TypeContext Parent;

    public TypeContext(TypeContext parent){
        Variables = new Dictionary<string, Type>();
        Types = new Dictionary<string, int>();
        Parent = parent;
    }
    
    public TypeContext(TypeContext parent, Dictionary<string, Type> variables){
        Variables = variables;
        Types = new Dictionary<string, int>();
        Parent = parent;
    }
    
    public TypeContext(Dictionary<string, int> types){
        Types = types;
        Variables = new Dictionary<string, Type>();
    }

    public bool IsClass(string identifier){
        Dictionary<string, int> tempTypes = Types;
        while(!tempTypes.ContainsKey(identifier)){
            if(Parent is not null){
                tempTypes = Parent.Types;
                continue;
            }
            return false;
        }
        return true;
    }

    public int ClassGenerics(string identifier){
        Dictionary<string, int> tempTypes = Types;
        while(!tempTypes.ContainsKey(identifier)){
            tempTypes = Parent.Types;
        }
        return tempTypes[identifier];
    }

    public Type GetVar(string identifier){
        Dictionary<string, Type> tempVars = Variables;
        while(!tempVars.ContainsKey(identifier)){
            if(Parent is not null){
                tempVars = Parent.Variables;
                continue;
            }
            return null;
        }
        return tempVars[identifier];
    }
}

public class ParseResult
{
    public Error Error;
    public Node Node;
    //Advance() is always included before SetNode() so it's an option to include it in the function
    public ParseResult SetNode(Node node){
        Node = node;
        return this;
    }
    //Advance() is also always included before SetError() so it could be included here too
    public ParseResult SetError(Error error){
        Error = error;
        return this;
    }

    public Node Register(ParseResult result){
        if(result.Error is not null) Error = result.Error;
        return result.Node;
    }
}

public class Parser
{
    private static List<Token> _tokens;
    private static int _index;
    private static Token _currentToken;
    public static int AdvanceCount;

    private static void Advance(){
        _index++;
        AdvanceCount++;
        if (_currentToken.Type != TokenType.EOF){
            _currentToken = _tokens[_index];
        }
        Debug.Log(_currentToken.Type);
    }

    private static void Retreate(int amount){
        _currentToken = _tokens[_index -= amount];
        Debug.Log(_currentToken.Type);
    }

    private static Node TryRegister(ref TypeContext typeContext, ParseFunction function){
        var result = new ParseResult();

        int localAdvanceCount = AdvanceCount;
        Node node = result.Register(function(ref typeContext));
        if(result.Error is not null){
            Retreate(AdvanceCount - localAdvanceCount);
            return null;
        }
        return node;
    }

    public static ParseResult Parse(List<Token> tokens){
        _tokens = tokens;
        _index = -1;
        AdvanceCount = 0;
        _currentToken = tokens[0];
        Advance();
        return Statements(new TypeContext(new Dictionary<string, int>(){
            {"int", 0},
            {"double", 0},
            {"string", 0},
            {"bool", 0},
            {"func", 1}
            }));
    }

    private static ParseResult Statements(TypeContext typeContext){
        var result = new ParseResult();
        Position posStart = _currentToken.PosStart.Copy();
        var nodes = new List<Node>();
        Node node;

        while(_currentToken.Type != TokenType.EOF){
            node = result.Register(Statement(ref typeContext));
            if(result.Error is not null) return result;
            nodes.Add(node);
        }
        return result.SetNode(new StatementsNode(nodes, posStart, _currentToken.PosEnd.Copy()));
    }

    private static ParseResult Statement(ref TypeContext typeContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node node;
        switch (_currentToken.Type){
            case TokenType.Repeat:
                node = result.Register(RepeatStatement(ref typeContext));
                if(result.Error is not null) return result;
                break;
            case TokenType.Loop:
                node = result.Register(LoopStatement(ref typeContext));
                if(result.Error is not null) return result;
                break;
            case TokenType.While:
                node = result.Register(WhileStatement(ref typeContext));
                if(result.Error is not null) return result;
                break;
            case TokenType.Identifier:
                node = result.Register(DefVar(ref typeContext));
                if(result.Error is not null) return result;
                break;
            case TokenType.Class:
                node = result.Register(ClassStatement(ref typeContext));
                if(result.Error is not null) return result;
                break;
            case TokenType.Return:
                Advance();
                Node expresion = TryRegister(ref typeContext, Expr);
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
                node = TryRegister(ref typeContext, ConditionStatement);
                if(node is null) goto default;
                break;
            default:
                node = result.Register(Expr(ref typeContext));
                if(result.Error is not null) return result;
                break;
        }
        if(_currentToken.Type != TokenType.SC){
            return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Expected Character", "\";\""));
        }
        Advance();
        return result.SetNode(node);
    }

    private static ParseResult RepeatStatement(ref TypeContext typeContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node expr = null;
        Node statements = null;

        Advance();
        expr = result.Register(Expr(ref typeContext));
        if(result.Error is not null) return result;
        BSB(ref statements, ref result, new TypeContext(typeContext));
        if(result.Error is not null) return result;
        return result.SetNode(new RepeatNode(expr, statements, posStart, _currentToken.PosEnd.Copy()));
    }

    private static ParseResult LoopStatement(ref TypeContext typeContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        var expresions = new Node[3];
        Node statements = null;

        Advance();
        if(_currentToken.Type != TokenType.LParen){
            return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Expected Character", "\"(\""));
        }
        Advance();
        for(int i = 0; i < 3; i++){
            expresions[i] = result.Register(Expr(ref typeContext));
            if(result.Error is not null) return result;
            if(i < 2){
                if(_currentToken.Type != TokenType.Comma){
                    Debug.Log(expresions[i].ToString());
                    return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Expected Character", "\",\""));
                }
                Advance();
            }
        }
        if(_currentToken.Type != TokenType.RParen){
            return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Expected Character", "\")\""));
        }
        Advance();
        BSB(ref statements, ref result, new TypeContext(typeContext));
        if(result.Error is not null) return result;
        return result.SetNode(new LoopNode(expresions, statements, posStart, _currentToken.PosEnd.Copy()));
    }

    private static ParseResult WhileStatement(ref TypeContext typeContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node expr = null;
        Node statements = null;

        Advance();
        expr = result.Register(Expr(ref typeContext));
        if(result.Error is not null) return result;
        BSB(ref statements, ref result, new TypeContext(typeContext));
        if(result.Error is not null) return result;
        return result.SetNode(new WhileNode(expr, statements, posStart, _currentToken.PosEnd.Copy()));
    }

    private static ParseResult DefVar(ref TypeContext typeContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        string identifier;

        (Error error, Type type) type = ParseType();
        if(type.error is not null) return result.SetError(type.error);
        if(!typeContext.IsClass(type.type.BaseType)) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax Error", $"The class {type.type.BaseType} does not exist in the current context"));
        int count = typeContext.ClassGenerics(type.type.BaseType);
        if(count == 0){
            if(type.type.GenericTypes is not null) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax Error", $"Cannot use generics on non-generic type {type.type.BaseType}"));
        }else if(type.type.BaseType == "func"){
            if(type.type.GenericTypes is null) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax Error", $"The type func must have at least one generic parameter"));
        }else if(type.type.GenericTypes is null || type.type.GenericTypes.Count != count) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax Error", $"Generic type {type.type.BaseType} must have {count} generic parameters"));
        if(_currentToken.Type != TokenType.Identifier) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Expected Identifier"));
        identifier = _currentToken.Value;
        if(typeContext.GetVar(identifier)is not null) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax Error", $"The identifier {identifier} already exists in the current context"));
        Advance();
        typeContext.Variables.Add(identifier, type.type);
        return result.SetNode(new DefVarNode(type.type, identifier, posStart, _currentToken.PosEnd.Copy()));
    }
    
    private static ParseResult ClassStatement(ref TypeContext typeContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node statements = null;
        string type;
        List<string> genIds;
        (Error error, List<Type> types, List<string> identifiers) listResult1;
        (Error error, List<string> identifiers) listResult2;

        Advance();
        if(_currentToken.Type != TokenType.Identifier) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", "Expected Identifier"));
        type = _currentToken.Value;
        Advance();
        if(_currentToken.Type == TokenType.LT){
            genIds = new List<string>();
            do{
                Advance();
                if(_currentToken.Type != TokenType.Identifier) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", "Expected Identifier"));
                genIds.Add(_currentToken.Value);
                Advance();
            }while(_currentToken.Type == TokenType.Comma);
            if(_currentToken.Type != TokenType.GT) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax error", "Expected \'>\'"));
            Advance();
        }
        if(_currentToken.Type != TokenType.LBrack){
            return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", "Expected \"[\""));
        }
        Advance();
        listResult1 = TypeIdList();
        if(listResult1.error is not null){
            return result.SetError(listResult1.error);
        }
        if(_currentToken.Type != TokenType.RBrack){
            return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", "Expected \"]\""));
        }
        Advance();
        if(_currentToken.Type == TokenType.From){
            Advance();
            listResult2 = IdList();
            if(listResult2.error is not null){
                return result.SetError(listResult2.error);
            }
        }else{
            listResult2.identifiers = null;
        }
        BSB(ref statements, ref result, new TypeContext(typeContext));
        if(result.Error is not null) return result;
        return result.SetNode(new ClassNode(listResult1.types, listResult1.identifiers, listResult2.identifiers, statements, posStart, _currentToken.PosEnd.Copy()));
    }

    private static ParseResult Expr(ref TypeContext typeContext){
        return BinOp(CompExprA, new TokenType[] {TokenType.And, TokenType.Or}, ref typeContext);
    }

    private static ParseResult CompExprA(ref TypeContext typeContext){
        return UnOp(CompExprB, CompExprA, TokenType.Not, ref typeContext);
    }

    private static ParseResult CompExprB(ref TypeContext typeContext){
        return BinOp(ArithExprA, new TokenType[] {TokenType.EE, TokenType.NE, TokenType.GT, TokenType.LT, TokenType.GTE, TokenType.LTE}, ref typeContext);
    }

    private static ParseResult ArithExprA(ref TypeContext typeContext){
        return BinOp(ArithExprB, new TokenType[] {TokenType.Plus, TokenType.Minus}, ref typeContext);
    }

    private static ParseResult ArithExprB(ref TypeContext typeContext){
        return BinOp(PowExpr, new TokenType[] {TokenType.Multiply, TokenType.Divide}, ref typeContext);
    }

    private static ParseResult PowExpr(ref TypeContext typeContext){
        return BinOp(NegExpr, new TokenType[] {TokenType.Power}, ref typeContext);
    }

    private static ParseResult NegExpr(ref TypeContext typeContext){
        return UnOp(SetVar, NegExpr, TokenType.Minus, ref typeContext);
    }

    private static ParseResult SetVar(ref TypeContext typeContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node callNode, expresion;
        (Error error, Type type) res;
        TokenType op;

        callNode = result.Register(PointCall(ref typeContext));
        if(result.Error is not null) return result;
        op = _currentToken.Type;
        switch(op){
            case TokenType.EQ:
                if(callNode is not PointCallNode && callNode is not IdentifierNode) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Assignment must be made to a variable"));
                Advance();
                expresion = result.Register(Expr(ref typeContext));
                if(result.Error is not null) return result;
                if(!callNode.Type.Equals(expresion.Type)) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax Error", $"Variable assignment must be made to the same type ({callNode.Type} and {expresion.Type})"));
                return result.SetNode(new SetVarNode(callNode, op, posStart, _currentToken.PosEnd.Copy(), expresion.Type, expresion));
            case TokenType.EPlus:
            case TokenType.EMinus:
            case TokenType.EMult:
            case TokenType.EDiv:
            case TokenType.EPow:
                if(callNode is not PointCallNode && callNode is not IdentifierNode) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Assignment must be made to a variable"));
                Advance();
                expresion = result.Register(Expr(ref typeContext));
                if(result.Error is not null) return result;
                res = BinOpType(callNode.Type, op, expresion.Type);
                if(res.error is not null) return result.SetError(res.error);
                if(callNode.Type != res.type) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Variable assignment must be made to the same type"));
                return result.SetNode(new SetVarNode(callNode, op, posStart, _currentToken.PosEnd.Copy(), expresion.Type, expresion));
            case TokenType.PP:
            case TokenType.MM:
                if(callNode is not PointCallNode && callNode is not IdentifierNode) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Assignment must be made to a variable"));
                res = BinOpType(callNode.Type, (op == TokenType.PP)? TokenType.Plus : TokenType.Minus, new Type("Int"));
                if(res.error is not null) return result.SetError(res.error);
                Advance(); //past MM or PP
                if(callNode.Type != res.type) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Variable assignment must be made to the same type"));
                return result.SetNode(new SetVarNode(callNode, op, posStart, _currentToken.PosEnd.Copy(), res.type));
            default:
                return result.SetNode(callNode);
        }
    }

    private static ParseResult PointCall(ref TypeContext typeContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node left, right;

        left = result.Register(FuncObjGenCall(ref typeContext));
        if(result.Error is not null) return result;
        while(_currentToken.Type == TokenType.Point){
            Advance();
            right = result.Register(FuncObjGenCall(ref typeContext));
            if(result.Error is not null) return result;
            left = new PointCallNode(left, right, posStart, _currentToken.PosEnd.Copy(), right.Type);
        }
        return result.SetNode(left);
    }

    private static ParseResult FuncObjGenCall(ref TypeContext typeContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node node;
        List<Node> accessNodes;
        List<string> accessIds;
        
        node = result.Register(Atom(ref typeContext));
        if(result.Error is not null) return result;
        start:
        if(_currentToken.Type == TokenType.LParen){
            if(node.Type.BaseType != "Func") return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Type Error", "Function call must be of type func"));
            Advance();
            (Error error, List<Node> nodes) listResult = MakeNodeList(ref typeContext);
            if(listResult.error is not null) return result.SetError(listResult.error);
            accessNodes = listResult.nodes;
            if(_currentToken.Type == TokenType.RParen){
            Advance();
            if(node.Type.GenericTypes is not null || node.Type.GenericTypes.Count != 1) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Type Error", "To call a function it must have exactly one generic return type"));
            node = new FuncCallNode(node, accessNodes, posStart, _currentToken.PosEnd.Copy(), node.Type.GenericTypes[0]);
            goto start;
            }
            return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Expected Right Parenthesis"));
        }else if(_currentToken.Type == TokenType.LBrack){
            if(node is not IdentifierNode) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Class call must be made to an Identifier"));
            string id = (node as IdentifierNode).Value;
            Advance();
            (Error error, List<Node> nodes) listResult = MakeNodeList(ref typeContext);
            if(listResult.error is not null) return result.SetError(listResult.error);
            accessNodes = listResult.nodes;
            if(_currentToken.Type == TokenType.RBrack){
            Advance();
            if(node.Type.GenericTypes is not null || node.Type.GenericTypes.Count != 1) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Type Error", "To call a class it must have exactly one generic return type"));
            node = new ObjCallNode(node, accessNodes, posStart, _currentToken.PosEnd.Copy(), node.Type.GenericTypes[0]);
            goto start;
            }
            return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Expected Right Bracket"));
        }else if(_currentToken.Type == TokenType.LT){
            if(node.Type.BaseType != "GenericFunction" || node.Type.BaseType != "GenericClass") return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Type Error", "Generic calls must be made to generic types"));
            
            Advance();
            (Error error, List<string> ids) listResult = IdList();
            if(listResult.error is not null) return result.SetError(listResult.error);
            accessIds = listResult.ids;
            if(_currentToken.Type == TokenType.GT){
            Advance();
            if(node.Type.GenericTypes is not null || node.Type.GenericTypes.Count != 1) return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Type Error", "To call a class it must have exactly one generic return type"));
            node = new GenCallNode(node, accessIds, posStart, _currentToken.PosEnd.Copy(), node.Type.GenericTypes[0]);
            goto start;
            }
            return result.SetError(new Error(posStart, _currentToken.PosEnd.Copy(), "Invalid Syntax Error", "Expected Right Bracket"));
        }
        return result.SetNode(node);
    }

    private static ParseResult Atom(ref TypeContext typeContext){
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
            case TokenType.Identifier:
                if(value == "func") return FuncExpr(ref typeContext);
                Type type = typeContext.GetVar(_currentToken.Value);
                if(type is null) return result.SetError(new Error(posStart, _currentToken.PosEnd, "Identfier Error", $"{_currentToken.Value} does not exist in the current context"));
                Advance();
                return result.SetNode(new IdentifierNode(value, posStart, _currentToken.PosEnd.Copy(), type));
            case TokenType.If:
            case TokenType.Try:
                return ConditionExpr(ref typeContext);
            case TokenType.LParen:
                Advance();
                node = result.Register(Expr(ref typeContext));
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

    private static ParseResult FuncExpr(ref TypeContext typeContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node statements = null;
        List<Type> genTypes, genIds;
        (Error error, Type type) parseType;
        bool isGen;

        Advance();
        parseType = ParseType();
        if(parseType.error is not null) return result.SetError(parseType.error);
        genTypes = new List<Type>();
        genTypes.Add(parseType.type);
        if(_currentToken.Type == TokenType.LT){
            genIds = new List<Type>();
            do{
                Advance();
                if(parseType.error is not null) return result.SetError(parseType.error);
                genIds.Add(parseType.type);
                Advance();
            }while(_currentToken.Type == TokenType.Comma);
            if(_currentToken.Type != TokenType.GT) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax error", "Expected \'>\'"));
            isGen = true;
        }else{
            isGen = false;
            genIds = null;
        }
        Advance();
        if(_currentToken.Type != TokenType.LParen) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", "Expected \"(\""));
        (Error error, List<Type> types, List<string> ids) listResult = TypeIdList();
        if(listResult.error is not null) return result.SetError(listResult.error);
        genTypes.AddRange(listResult.types);
        if(_currentToken.Type != TokenType.RParen) return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", "Expected \")\""));
        Advance();
        BSB(ref statements, ref result, new TypeContext(typeContext, ListsToDict(listResult.ids, listResult.types)));
        if(result.Error is not null) return result;
        return result.SetNode(new FuncNode(listResult.ids, statements, posStart, _currentToken.PosEnd.Copy(), isGen ? new Type("GenericFunction", new List<Type>(){new Type("Generic", genIds), new Type("Function", genTypes)}) : new Type("Function", listResult.types)));
    }

    public static (Error error, Type type) BinOpType(Type left, TokenType op, Type right){
        switch(op){
            case TokenType.Plus:
            case TokenType.EPlus:
                if(left == Type.IntType){
                    if(right == Type.IntType) return (null, Type.IntType);
                    else if(right == Type.DoubleType) return (null, Type.DoubleType);
                    else if(right == Type.StringType) return (null, Type.StringType);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
                }else if(left == Type.DoubleType){
                    if(right == Type.IntType || right == Type.DoubleType) return (null, Type.DoubleType);
                    else if(right == Type.StringType) return (null, Type.StringType);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
                }else if(left == Type.StringType){
                    if(right == Type.IntType || right == Type.DoubleType || right == Type.StringType) return (null, Type.StringType);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
                }else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
            case TokenType.Minus:
            case TokenType.EMinus:
            case TokenType.Multiply:
            case TokenType.EMult:
            case TokenType.Power:
            case TokenType.EPow:
                if(left == Type.IntType){
                    if(right == Type.IntType) return (null, Type.IntType);
                    else if(right == Type.DoubleType) return (null, Type.DoubleType);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
                }else if(left == Type.DoubleType){
                    if(right == Type.IntType || right == Type.DoubleType) return (null, Type.DoubleType);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
                }else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
            case TokenType.Divide:
            case TokenType.EDiv:
                if(left == Type.IntType){
                    if(right == Type.IntType || right == Type.DoubleType) return (null, Type.DoubleType);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
                }else if(left == Type.DoubleType){
                    if(right == Type.IntType || right == Type.DoubleType) return (null, Type.DoubleType);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
                }else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
            case TokenType.EE:
            case TokenType.NE:
                if(left == Type.IntType){
                    if(right == Type.IntType || right == Type.DoubleType) return (null, Type.BoolType);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
                }else if(left == Type.DoubleType){
                    if(right == Type.IntType || right == Type.DoubleType) return (null, Type.BoolType);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
                }else if(left == Type.StringType){
                    if(right == Type.StringType) return (null, Type.BoolType);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
                }else if(left == Type.BoolType){
                    if(right == Type.BoolType) return (null, Type.BoolType);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
                }else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
            case TokenType.GT:
            case TokenType.LT:
            case TokenType.GTE:
            case TokenType.LTE:
                if(left == Type.IntType){
                    if(right == Type.IntType || right == Type.DoubleType) return (null, Type.BoolType);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
                }else if(left == Type.DoubleType){
                    if(right == Type.IntType || right == Type.DoubleType) return (null, Type.BoolType);
                    else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
                }else return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
            default:
                return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {op} to types {left} and {right}"), null);
        }
    }

    private static ParseResult BinOp(ParseFunction child, TokenType[] tokenTypes, ref TypeContext typeContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node left, right;
        TokenType op = default;
        (Error error, Type type) res;

        left = result.Register(child(ref typeContext));
        if(result.Error is not null) return result;
        while(Array.Exists(tokenTypes, TT => TT == _currentToken.Type)){
            op = _currentToken.Type;
            Advance();
            right = result.Register(child(ref typeContext));
            if(result.Error is not null) return result;
            res = BinOpType(left.Type, op, right.Type);
            if(res.error is not null) return result.SetError(res.error);
            left = new BinOpNode(left, op, right, posStart, _currentToken.PosEnd.Copy(), res.type);
        }
        return result.SetNode(left);
    }

    private static ParseResult UnOp(ParseFunction child, ParseFunction current, TokenType tokenType, ref TypeContext typeContext){
        Position posStart = _currentToken.PosStart.Copy();
        var result = new ParseResult();
        Node node;
        Type type = null;

        if(_currentToken.Type == tokenType){
            Advance();
            node = result.Register(current(ref typeContext));
            if(result.Error is not null) return result;
            switch(tokenType){
                case TokenType.Minus:
                    if(node.Type == Type.IntType) type = Type.IntType;
                    else if(node.Type == Type.DoubleType) type = Type.DoubleType;
                    else return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {tokenType} to type {node.Type}"));
                    break;
                case TokenType.Not:
                    if(node.Type == Type.BoolType) type = Type.BoolType;
                    else return result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", $"Cannot apply operand {tokenType} to type {node.Type}"));
                    break;
            }
            node = new UnOpNode(tokenType, node, posStart, _currentToken.PosEnd.Copy(), type);
            return result.SetNode(node);
        }
        return child(ref typeContext);
    }

    private static ParseResult ConditionStatement(ref TypeContext typeContext){
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
            expr = result.Register(Expr(ref typeContext));
            if(result.Error is not null) return result;
            if(expr.Type != Type.BoolType) return result.SetError(new Error(tempPosStart, _currentToken.PosEnd.Copy(), "Type Error", "If Statement Expresion must be of type Bool"));
            BSB(ref statements, ref result, new TypeContext(typeContext));
            if(result.Error is not null) return result;
            expr = new IfNode(expr, statements, tempPosStart, _currentToken.PosEnd.Copy());
        } else {
            tempPosStart = _currentToken.PosStart.Copy();
            Advance();
            BSB(ref statements, ref result, new TypeContext(typeContext));
            if(result.Error is not null) return result;
            expr = new TryNode(statements, posStart, _currentToken.PosEnd.Copy());
        }
        if((isIf = _currentToken.Type == TokenType.Elif) || _currentToken.Type == TokenType.Eltry){
            elExpresionList = new List<Node>();
            do{
                if(isIf){
                    tempPosStart = _currentToken.PosStart.Copy();
                    Advance();
                    elExpr = result.Register(Expr(ref typeContext));
                    if(result.Error is not null) return result;
                    if(elExpr.Type != Type.BoolType) return result.SetError(new Error(tempPosStart, _currentToken.PosEnd.Copy(), "Type Error", "If Statement Expresion must be of type Bool"));
                    BSB(ref elStatements, ref result, new TypeContext(typeContext));
                    if(result.Error is not null) return result;
                    elExpr = new IfNode(elExpr, elStatements, tempPosStart, _currentToken.PosEnd.Copy());
                } else {
                    tempPosStart = _currentToken.PosStart.Copy();
                    Advance();
                    BSB(ref elStatements, ref result, new TypeContext(typeContext));
                    if(result.Error is not null) return result;
                    elExpr = new TryNode(elStatements, posStart, _currentToken.PosEnd.Copy());
                }
                elExpresionList.Add(elExpr);
            }while((isIf = _currentToken.Type == TokenType.Elif) || _currentToken.Type == TokenType.Eltry);
        }
        if(_currentToken.Type == TokenType.Else){
            Advance();
            BSB(ref elseStatements, ref result, new TypeContext(typeContext));
            if(result.Error is not null) return result;
        }
        return result.SetNode(new ConditionNode(expr, elExpresionList, elseStatements, posStart, _currentToken.PosStart.Copy()));
    }

    private static ParseResult ConditionExpr(ref TypeContext typeContext){
        Position posStart = _currentToken.PosStart.Copy();
        Position tempPosStart;
        var result = new ParseResult();
        Node conditionExpr = default, returnExpr = default, elConditionExpr = default, elReturnExpr = default, elseExpr = default;
        List<Node> elExpresionList = default;
        bool isIf;

        if(_currentToken.Type == TokenType.If){
            tempPosStart = _currentToken.PosStart.Copy();
            Advance(); 
            conditionExpr = result.Register(Expr(ref typeContext));
            if(result.Error is not null) return result;
            if(conditionExpr.Type != Type.BoolType) return result.SetError(new Error(tempPosStart, _currentToken.PosEnd.Copy(), "Type Error", "If expresion's conditional expresion must be of type Bool"));
            returnExpr = result.Register(Expr(ref typeContext));
            if(result.Error is not null) return result;
            returnExpr = new IfExprNode(conditionExpr, returnExpr, tempPosStart, _currentToken.PosEnd.Copy());
        } else {
            tempPosStart = _currentToken.PosStart.Copy();
            Advance();
            returnExpr = result.Register(Expr(ref typeContext));
            if(result.Error is not null) return result;
            returnExpr = new TryExprNode(returnExpr, posStart, _currentToken.PosEnd.Copy());
        }
        if((isIf = _currentToken.Type == TokenType.Elif) || _currentToken.Type == TokenType.Eltry){
            elExpresionList = new List<Node>();
            do{
                if(isIf){
                    tempPosStart = _currentToken.PosStart.Copy();
                    Advance(); 
                    elConditionExpr = result.Register(Expr(ref typeContext));
                    if(result.Error is not null) return result;
                    if(elConditionExpr.Type != Type.BoolType) return result.SetError(new Error(tempPosStart, _currentToken.PosEnd.Copy(), "Type Error", "If expresion's conditional expresion must be of type Bool"));
                    elReturnExpr = result.Register(Expr(ref typeContext));
                    if(result.Error is not null) return result;
                    elReturnExpr = new IfExprNode(elConditionExpr, elReturnExpr, tempPosStart, _currentToken.PosEnd.Copy());
                    if(elReturnExpr.Type != returnExpr.Type) return result.SetError(new Error(tempPosStart, _currentToken.PosEnd.Copy(), "Type Error", "If Expresion's results must all be of the same type"));
                } else {
                    tempPosStart = _currentToken.PosStart.Copy();
                    Advance();
                    returnExpr = result.Register(Expr(ref typeContext));
                    if(result.Error is not null) return result;
                    returnExpr = new TryExprNode(returnExpr, posStart, _currentToken.PosEnd.Copy());
                }
                elExpresionList.Add(elReturnExpr);
            }while((isIf = _currentToken.Type == TokenType.Elif) || _currentToken.Type == TokenType.Eltry);
        }
        if(_currentToken.Type == TokenType.Else){
            posStart = _currentToken.PosStart.Copy();
            Advance();
            elseExpr = result.Register(Expr(ref typeContext));
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

    private static void BSB(ref Node statements, ref ParseResult result, TypeContext typeContext){
        Position posStart = _currentToken.PosStart.Copy();
        Node statement;
        var localStatments = new List<Node>();

        if(_currentToken.Type != TokenType.LBrace){
            result.SetError(new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax", "Expected \"{\""));
            return;
        }
        Advance();
        while(_currentToken.Type != TokenType.RBrace){
            statement = result.Register(Statement(ref typeContext));
            if(result.Error is not null) return;
            localStatments.Add(statement);
        }
        Advance();
        statements = new StatementsNode(localStatments, posStart, _currentToken.PosEnd.Copy());
    }

    private static (Error error, List<Node> nodes) MakeNodeList(ref TypeContext typeContext){
        Node tempNode;
        var result = new ParseResult();
        var nodes = new List<Node>();
        
        tempNode = TryRegister(ref typeContext, Expr);
        if(tempNode is not null){
            nodes.Add(tempNode);
            while(_currentToken.Type == TokenType.Comma){
                Advance();
                tempNode = result.Register(Expr(ref typeContext));
                if (result.Error is not null){
                    return (result.Error, null);
                }
                nodes.Add(tempNode);
            }
        }
        return (null, nodes);
    }

    private static (Error error, List<string> identifiers) IdList(){
        var identifiers = new List<string>();
        
        if(_currentToken.Type == TokenType.Identifier){
            identifiers.Add(_currentToken.Value);
            Advance();
            while(_currentToken.Type == TokenType.Comma){
                Advance();
                if(_currentToken.Type == TokenType.Identifier){
                    identifiers.Add(_currentToken.Value);
                    Advance();
                } else{
                    return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "EInvalid syntax error", "Expected identifier"), null);
                }
            }
        }
        return (null, identifiers);
    }

    private static (Error error, List<Type> types, List<string> identifiers) TypeIdList(){
        var identifiers = new List<string>();
        var types = new List<Type>();
        (Error error, Type type) res;
        
        if(_currentToken.Type == TokenType.Identifier){
            res = ParseType();
            if(res.error is not null) return (res.error, null, null);
            types.Add(res.type);
            if(_currentToken.Type != TokenType.Identifier) return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax error", "Expected identifier"), null, null);;
            identifiers.Add(_currentToken.Value);
            Advance();
            while(_currentToken.Type == TokenType.Comma){
                Advance();
                res = ParseType();
                if(res.error is not null) return (res.error, null, null);
                types.Add(res.type);
                if(_currentToken.Type != TokenType.Identifier) return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax error", "Expected identifier"), null, null);
                identifiers.Add(_currentToken.Value);
                Advance();
            }
        }
        return (null, types, identifiers);
    }

    private static (Error error, Type type) ParseType(){
        string id;
        List<Type> types = null;

        if(_currentToken.Type != TokenType.Identifier) return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax error", "Expected identifier"), null);
        id = _currentToken.Value;
        Advance();
        if(_currentToken.Type == TokenType.LT){
            Advance();
            types = new List<Type>();
            (Error error, Type type) type;
            Parse:
            type = ParseType();
            if(type.error is not null) return type;
            types.Add(type.type);
            if(_currentToken.Type == TokenType.Comma){
                Advance();
                goto Parse;
            }
            if(_currentToken.Type != TokenType.GT) return (new Error(_currentToken.PosStart.Copy(), _currentToken.PosEnd.Copy(), "Invalid syntax error", "Expected \'>\'"), null);
            Advance();
        }
        return (null, new Type(id, types));
    }

    private static Dictionary<T, U> ListsToDict<T, U>(List<T> listT, List<U> listU){
        if(listT.Count != listU.Count) return null;
        Dictionary<T, U> result = new();
        for(int i = 0; i < listT.Count; i++){
            result.Add(listT[i], listU[i]);
        }
        return result;
    }
}
