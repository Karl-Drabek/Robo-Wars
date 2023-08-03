using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
//TODO  Error cheching or empty elements of list call node,  SetType for unary and binary op nodes, fix context for statement stuff
//finsih setting up print

public struct GameContext{

    public Robot Robot;
    public TMP_Text Terminal;
    public Dictionary<string, Func<List<dynamic>, GameContext, dynamic>> Funcs;
    public GridManager Manager;

    public GameContext(Robot robot, TMP_Text terminal, Dictionary<string, Func<List<dynamic>, GameContext, dynamic>> funcs, GridManager manager){
        (Robot, Terminal, Funcs, Manager) = (robot, terminal, funcs, manager);
    }
}

public abstract class Node
{
    public Position PosStart, PosEnd;
    public Type Type;

    public Node(Position posStart, Position posEnd, Type type){
        PosStart = posStart;
        PosEnd = posEnd;
        Type = type;
    }

    public static string CommaList<T> (List<T> list){
        StringBuilder output = new StringBuilder();
        int length = list.Count;

        for(int i = 0; i < length; i++){
            output.Append(list[i].ToString());
            if(i != length - 1){
            output.Append(", ");
            }
        }
        return output.ToString();
    }

    public static string CommaList<T, U>(List<T> list1, List<U> list2){
        StringBuilder output = new StringBuilder();
        int length = list1.Count;

        for(int i = 0; i < length; i++){
            output.Append($"{list1[i]} {list2[i]}");
            if(i != length - 1){
            output.Append(", ");
            }
        }
        return output.ToString();
    }

    public abstract RunTimeResult Visit(ref RTContext context, ref GameContext gameContext);
}

public class IntNode : Node
{
    public int Value;
    
    public IntNode(int value, Position posStart, Position posEnd) : base(posStart, posEnd, Type.Int){
        Value = value;
    }
    
    public override string ToString(){
        return Value.ToString();
    }

    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext) => (new RunTimeResult()).SetValue(Value).SetType(Type);
}

public class DoubleNode : Node
{
    public double Value;
    
    public DoubleNode(double value, Position posStart, Position posEnd) : base(posStart, posEnd, Type.Double){
        Value = value;
    }
    
    public override string ToString(){
        return Value.ToString();
    }

    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext) => (new RunTimeResult()).SetValue(Value).SetType(Type);
}

public class StringNode : Node
{
    public string Value;

    public StringNode(string value, Position posStart, Position posEnd) : base(posStart, posEnd, Type.String){
        Value = value;
    }

    public override string ToString(){
        return $"\"{Value}\"";
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext) => (new RunTimeResult()).SetValue(Value).SetType(Type);
}

public class IdentifierNode : Node
{
    public string Value;

    public IdentifierNode(string value, Position posStart, Position posEnd, Type type) : base(posStart, posEnd, type){
        Value = value;
    }
    
    public override string ToString(){
        return Value;
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        return new RunTimeResult().SetValue(context.SymbolTable.GetVar(Value)).SetType(Type);
    }
}

public class BoolNode : Node
{
    public bool Value;

    public BoolNode(bool value, Position posStart, Position posEnd) : base(posStart, posEnd, Type.Bool){
        Value = value;
    }
    public override string ToString(){
        return Value.ToString();
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext) => (new RunTimeResult()).SetValue(Value).SetType(Type);
}

public class StatementsNode : Node
{
    public List<Node> Nodes;

    public StatementsNode(List<Node> nodes, Position posStart, Position posEnd) : base(posStart, posEnd, Type.Void){
        Nodes = nodes;
    }

    public override string ToString(){
        return $"[{CommaList<Node>(Nodes)}]";
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        RunTimeResult result = new();
        foreach(Node node in Nodes){
            result.Register(node.Visit(ref context, ref gameContext));
            if(result.ShouldReturn()) return result;
        }
        return result;
    }
}

public class JumpNode : Node
{
    public TokenType Value;
    public Node Expresion;

    public JumpNode(TokenType value, Position posStart, Position posEnd, Node expresion = default) : base(posStart, posEnd, Type.Void){
        Value = value;
        Expresion = expresion;
    }

    public override string ToString(){
        if (Expresion is null) return Value.ToString();
        return $"{Value.ToString()} {Expresion.ToString()}";
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        switch(Value){
            case TokenType.Continue:
                return new RunTimeResult().SetContinue(true);
            case TokenType.Break:
                return new RunTimeResult().SetBreak(true);
            case TokenType.Return:
                RunTimeResult result = new();
                if(Expresion is null) return result.SetReturn(true).SetValue(null);
                dynamic expr = result.Register(Expresion.Visit(ref context, ref gameContext));
                if(result.ShouldReturn()) return result;
                return (result).SetReturn(true).SetValue(expr).SetType(Expresion.Type);
            default:
                return new RunTimeResult().SetError(new Error(PosStart, PosEnd, "Invalid Jump", "The Jump Statement is Invalid"));
        }
    }
}

public class BinOpNode : Node
{
    public Node Left, Right;
    public TokenType Op;
    
    public BinOpNode(Node left, TokenType op, Node right, Position posStart, Position posEnd, Type type) : base(posStart, posEnd, type){
        Left = left;
        Op = op;
        Right = right;
    }

    public override string ToString(){
        return $"({Left.ToString()} {Op.ToString()} {Right.ToString()})";
    }

    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        RunTimeResult result = new();

        dynamic left = result.Register(Left.Visit(ref context, ref gameContext));
        if(result.ShouldReturn()) return result;
        dynamic right = result.Register(Right.Visit(ref context, ref gameContext));
        if(result.ShouldReturn()) return result;
        switch(Op){
            case TokenType.Plus:
                switch (left, right){
                    case (int, int):
                    case (int, double):
                    case (int, string):
                    case (double, int):
                    case (double, double):
                    case (double, string):
                    case (string, int):
                    case (string, double):
                    case (string, string):
                        return result.SetValue(left + right);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                            $"Cannot add operands of types {left.GetType()} and {right.GetType()}", "Details"));
                }
            case TokenType.Minus:
                switch (left, right){
                    case (int, int):
                    case (int, double):
                    case (double, int):
                    case (double, double):
                        return result.SetValue(left - right);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                            $"Cannot subtract operands of types {left.GetType()} and {right.GetType()}", "Details"));
                }
            case TokenType.Multiply:
                switch (left, right){
                    case (int, int):
                    case (int, double):
                    case (double, int):
                    case (double, double):
                        return result.SetValue(left * right);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                            $"Cannot multiply operands of types {left.GetType()} and {right.GetType()}", "Details"));
                }
            case TokenType.Divide:
                switch (left, right){
                    case (int, int):
                    case (int, double):
                    case (double, int):
                    case (double, double):
                        if(right == 0) return result.SetError(
                            new RunTimeError(PosStart, PosEnd,"Cannot Devide by Zero", "Details"));
                        return result.SetValue(left / right);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                            $"Cannot divide operands of types {left.GetType()} and {right.GetType()}", "Details"));
                }
            case TokenType.Power:
                switch (left, right){
                    case (int, int):
                    case (int, double):
                    case (double, int):
                    case (double, double):
                        return result.SetValue(Math.Pow(left, right));
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        $"Cannot exponentiate operands of types {left.GetType()} and {right.GetType()}", "Details"));
                }
            case TokenType.EE:
                switch (left, right){
                    case (int, int):
                    case (int, double):
                    case (double, int):
                    case (double, double):
                    case (string, string):
                    case (bool, bool):
                        return result.SetValue(left == right);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        $"Cannot determine equality for operands of types {left.GetType()} and {right.GetType()}", "Details"));
                }
            case TokenType.NE:
                switch (left, right){
                    case (int, int):
                    case (int, double):
                    case (double, int):
                    case (double, double):
                    case (string, string):
                    case (bool, bool):
                        return result.SetValue(left != right);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        $"Cannot determine inequality for operands of types {left.GetType()} and {right.GetType()}", "Details"));
                }
            case TokenType.LT:
                switch (left, right){
                    case (int, int):
                    case (int, double):
                    case (double, int):
                    case (double, double):
                        return result.SetValue(left < right);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        $"Cannot determine greater than for operands of types {left.GetType()} and {right.GetType()}", "Details"));
                }
            case TokenType.GT:
                switch (left, right){
                    case (int, int):
                    case (int, double):
                    case (double, int):
                    case (double, double):
                        return result.SetValue(left > right);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        $"Cannot determine less than for operands of types {left.GetType()} and {right.GetType()}", "Details"));
                }
            case TokenType.LTE:
                switch (left, right){
                    case (int, int):
                    case (int, double):
                    case (double, int):
                    case (double, double):
                        return result.SetValue(left <= right);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        $"Cannot determine less than or equals for operands of types {left.GetType()} and {right.GetType()}", "Details"));
                }
            case TokenType.GTE:
                switch (left, right){
                    case (int, int):
                    case (int, double):
                    case (double, int):
                    case (double, double):
                        return result.SetValue(left >= right);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        $"Cannot determine greater than or equals for operands of types {left.GetType()} and {right.GetType()}", "Details"));
                }
            case TokenType.And:
                switch (left, right){
                    case (bool, bool):
                        return result.SetValue(left && right);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        $"Cannot apply and for operands of types {left.GetType()} and {right.GetType()}", "Details"));
                }
            case TokenType.Or:
                switch (left, right){
                    case (bool, bool):
                        return result.SetValue(left || right);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        $"Cannot apply or for operands of types {left.GetType()} and {right.GetType()}", "Details"));
                }
            default:
                return result.SetError(new Error(PosStart, PosEnd, "Invalid Op", "The Binary Operator is Invalid"));
        } 
    }
}

public class UnOpNode : Node
{
    public Node Node;
    public TokenType Op;

    public UnOpNode(TokenType op, Node node, Position posStart, Position posEnd, Type type) : base(posStart, posEnd, type){
        Node = node;
        Op = op;
    }

    public override string ToString(){
        return $"({Op.ToString()} {Node.ToString()})";
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        RunTimeResult result = new();
        dynamic node = result.Register(Node.Visit(ref context, ref gameContext));
        if(result.ShouldReturn()) return result;
        switch(Op){
            case TokenType.Minus:
                switch(node){
                    case int:
                    case double:
                        return result.SetValue(-node);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        $"Cannot apply minus to type {node.GetType()}", "Details"));
                }
            case TokenType.Not:
                switch(node){
                    case bool:
                        return result.SetValue(!node);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        $"Cannot apply not to type {node.GetType()}", "Details"));
                }
            default:
                return result.SetError(new Error(PosStart, PosEnd, "Invalid Op", "The Unary Operator is Invalid"));
        }
    }
}

public class LoopNode : Node
{
    public Node[] Expresions;
    public Node Statements;

    public LoopNode(Node[] expresions, Node statements, Position posStart, Position posEnd) : base(posStart, posEnd, Type.Void){
        Expresions = expresions;
        Statements = statements;
    }

    public override string ToString(){
        return $"Loop({Expresions[0]}, {Expresions[1]}, {Expresions[2]}){{{Statements}}}";
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        RunTimeResult result = new();
        dynamic expr;

        result.Register(Expresions[0].Visit(ref context, ref gameContext));
        if(result.ShouldReturn()) return result;
        expr = result.Register(Expresions[1].Visit(ref context, ref gameContext));
        if(result.ShouldReturn()) return result;
        while(expr){
            result.Register(Statements.Visit(ref context, ref gameContext));
            if(result.ShouldReturnLoop()) return result;
            if(result.HasBreak) return result.SetBreak(false);
            if(result.HasContinue){
                result.SetContinue(false);
                continue;
            }
            result.Register(Expresions[2].Visit(ref context, ref gameContext));
            if(result.ShouldReturn()) return result;
            expr = result.Register(Expresions[1].Visit(ref context, ref gameContext));
            if(result.ShouldReturn()) return result;
        }
        return result;
    }
}

public class RepeatNode : Node
{
    public Node Expr, Statements;

    public RepeatNode(Node expr, Node statements, Position posStart, Position posEnd) : base(posStart, posEnd, Type.Void){
        Expr = expr;
        Statements = statements;
    }

    public override string ToString(){
        return $"Repeat({Expr}){{{Statements}}})";
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        RunTimeResult result = new();

        dynamic expr = result.Register(Expr.Visit(ref context, ref gameContext));
        if(result.ShouldReturn()) return result;
        if(expr is not int){
            return result.SetError(new RunTimeError(PosStart, PosEnd,
            "Repeat expression must be of type Int", "Details"));
        }
        for(int i = 0; i < expr; i++){
            result.Register(Statements.Visit(ref context, ref gameContext));
            if(result.ShouldReturnLoop()) return result;
            if(result.HasBreak) return result.SetBreak(false);
            if(result.HasContinue){
                result.SetContinue(false);
                continue;
            }
        }
        return result;
    }
}

public class WhileNode : Node
{
    public Node Expr, Statements;

    public WhileNode(Node expr, Node statements, Position posStart, Position posEnd) : base(posStart, posEnd, Type.Void){
        Expr = expr;
        Statements = statements;
    }

    public override string ToString(){
        return $"While({Expr}){{{Statements}}})";
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        RunTimeResult result = new();

        
        dynamic expr = result.Register(Expr.Visit(ref context, ref gameContext));
        if(result.ShouldReturn()) return result;
        if(expr is not bool){
            return result.SetError(new RunTimeError(PosStart, PosEnd,
            "Repeat expression must be of type Bool", "Details"));
        }
        while(expr){
            result.Register(Statements.Visit(ref context, ref gameContext));
            if(result.ShouldReturnLoop()) return result;
            if(result.HasBreak) return result.SetBreak(false);
            if(result.HasContinue){
                result.SetContinue(false);
                continue;
            }
        }
        return result;
    }
}

public class DefFuncNode : Node
{
    public Type ReturnType;
    public string Identifier;
    public List<Type> ParamTypes;
    public List<string> ParamIds;
    public Node Statements;

    public DefFuncNode(Type returnType, string identifier, List<Type> paramTypes, List<string> paramIds, Node statements, Position posStart, Position posEnd) : base(posStart, posEnd, Type.Void){
        ReturnType = returnType;
        Identifier = identifier;
        ParamTypes = paramTypes;
        ParamIds = paramIds;
        Statements = statements;
    }

    public override string ToString(){
        return $"Func {ReturnType} {Identifier}({CommaList<Type, string>(ParamTypes, ParamIds)}){{{Statements}}}";
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        context.SymbolTable.DefFunc(Identifier, new Function(Statements, ParamIds, context.SymbolTable));
        return new RunTimeResult();
    }
}

public class DefVarNode : Node
{
    public string Identifier;

    public DefVarNode(Type type, string identifier, Position posStart, Position posEnd) : base(posStart, posEnd, type){
        Identifier = identifier;
    }

    public override string ToString(){
        return $"{Type} {Identifier}";
    }

    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        context.SymbolTable.DefVar(Identifier);
        return new RunTimeResult();
    }
}

public class SetVarNode : Node
{
    public string ID;
    public Node Expresion;
    public TokenType AssignmentOp;

    public SetVarNode (string id, TokenType assignmentOp, Position posStart, Position posEnd, Type type, Node expresion = default) : base(posStart, posEnd, type){
        ID = id;
        AssignmentOp = assignmentOp;
        Expresion = expresion;
    }

    public override string ToString(){
        if(Expresion is not null){
            return ID + $" {AssignmentOp} {Expresion}";
        }
        return ID + $" {AssignmentOp}";
    }
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        RunTimeResult result = new();
        dynamic expr = result.Register(Expresion.Visit(ref context, ref gameContext));
        if(result.ShouldReturn()) return result;
        result.Value = expr;
        context.SymbolTable.SetVar(ID, result.Value);
        return result;
    }
}

public class FuncCallNode : Node
{
    public string Name;
    public List<Node> Exprs;

    public FuncCallNode(string name, List<Node> exprs, Position posStart, Position posEnd, Type type) : base(posStart, posEnd, type){
        Name = name;
        Exprs = exprs;
    }

    public override string ToString(){
        return Name + $"({CommaList<Node>(Exprs)})";
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        RunTimeResult result = new();
        List<dynamic> values = new();
        foreach(Node n in Exprs){
            dynamic value = result.Register(n.Visit(ref context, ref gameContext));
            if(result.ShouldReturn()) return result;
            values.Add(value);
        }
        
        if(gameContext.Funcs.ContainsKey(Name)){
            dynamic retVal = gameContext.Funcs[Name](values, gameContext);
            return result.SetValue(retVal);
        }else{
            return context.SymbolTable.GetFunc(Name).Call(values, new TraceBack(Name, context.TraceBack, PosStart), ref gameContext);
        }
    }
}

public class ListCallNode : Node
{
    public string Name;
    public Node Expr;

    public ListCallNode(string name, Node expr, Position posStart, Position posEnd, Type type) : base(posStart, posEnd, type){
        Name = name;
        Expr = expr;
    }

    public override string ToString(){
        return Name + $"[{Expr}]";
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        RunTimeResult result = new();
        dynamic value = result.Register(Expr.Visit(ref context, ref gameContext));
        if(result.ShouldReturn()) return result;
        dynamic list = context.SymbolTable.GetVar(Name);
        return result.SetValue(list[value]);
    }
}

public class ListExprNode : Node
{
    public List<Node> Nodes;

    public ListExprNode(List<Node> nodes, Position posStart, Position posEnd, Type type) : base(posStart, posEnd, type){
        Nodes = nodes;
    }

    public override string ToString(){
        return $"{{{CommaList(Nodes)}}}";
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        RunTimeResult result = new();
        List<dynamic> values = new();
        foreach(Node n in Nodes){
            dynamic value = result.Register(n.Visit(ref context, ref gameContext));
            if(result.ShouldReturn()) return result;
            values.Add(value);
        }
        return result.SetValue(values).SetType(new Type(Type.DT, true)).SetValue(values);
    }
}

public class ConditionNode : Node
{
    public Node Expr, ElseStatements;
    public List<Node> ElExprList;

    public ConditionNode(Node expr, List<Node> elExprList, Node elseStatements, Position posStart, Position posEnd) : base(posStart, posEnd, Type.Void){
        Expr = expr;
        ElExprList = elExprList;
        ElseStatements = elseStatements;
    }

    public override string ToString(){
        string output = Expr.ToString();
        if(ElExprList is not null){
            foreach(var n in ElExprList){
                output += " El" + n.ToString(); // try and if are uppercase here which is technically wrong
            }
        }
        if(ElseStatements is not null){
            output += $" Else{{{ElseStatements}}}";
        }
        return output;
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        RunTimeResult result = new();
        dynamic expr;

        if(Expr is IfNode){
            expr = result.Register(((IfNode)Expr).Expr.Visit(ref context, ref gameContext));
            if(result.ShouldReturn()) return result;
            if(expr is not bool){
                return result.SetError(new RunTimeError(PosStart, PosEnd,
                "If expression must be of type Bool", "Details"));
            }
            if(expr){
                return result.Register(((IfNode)Expr).Statements.Visit(ref context, ref gameContext));
            }
        }else{
            result.Register(((TryNode)Expr).Statements.Visit(ref context, ref gameContext));
            if(result.Error is null){
                if(result.ShouldReturn()) return result;
                return null;
            }
        }
        if(ElExprList is not null){
            foreach(Node node in ElExprList){
                if(Expr is IfNode){
                    expr = result.Register(((IfNode)node).Expr.Visit(ref context, ref gameContext));
                    if(result.ShouldReturn()) return result;
                    if(expr is not bool){
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        "If expression must be of type Bool", "Details"));
                    }
                    if(expr){
                        return result.Register(((IfNode)node).Statements.Visit(ref context, ref gameContext));
                    }
                return null;
                }else{ 
                    result.Register(((TryNode)node).Statements.Visit(ref context, ref gameContext));
                    if(result.Error is null){
                        if(result.ShouldReturn()) return result;
                        return null;
                    }
                }
            }
        }
        if(ElseStatements is not null){
            result.Register(ElseStatements.Visit(ref context, ref gameContext));
            if(result.ShouldReturn()) return result;
        }
        return result;
    }
}

public class IfNode : Node
{
    public Node Expr, Statements;

    public IfNode(Node expr, Node statements, Position posStart, Position posEnd) : base(posStart, posEnd, Type.Void){
        Expr = expr;
        Statements = statements;
    }

    public override string ToString(){
        return $"If({Expr}){{{Statements}}}";
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        return null;
    }
}

public class TryNode : Node
{
    public Node Statements;

    public TryNode(Node statements, Position posStart, Position posEnd) : base(posStart, posEnd, Type.Void){
        Statements = statements;
    }

    public override string ToString(){
        return $"Try{{{Statements}}}";
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        return null;
    }
}

public class ConditionExprNode : Node
{
    public Node Expr, ElseExpr;
    public List<Node> ElExprList;

    public ConditionExprNode(Node expr, List<Node> elExprList, Node elseExpr, Position posStart, Position posEnd) : base(posStart, posEnd, expr.Type){
        Expr = expr;
        ElExprList = elExprList;
        ElseExpr = elseExpr;
    }

    public override string ToString(){
        string output = Expr.ToString();
        if(ElExprList is not null){
            foreach(var n in ElExprList){
                output += " El" + n.ToString(); // try and if are uppercase here which is technically wrong
            }
        }
        if(ElseExpr is not null){
            output += $" Else {ElseExpr}";
        }
        return output;
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        RunTimeResult result = new();
        dynamic expr;

        if(Expr is IfExprNode){
            expr = result.Register(((IfExprNode)Expr).ConditionExpr.Visit(ref context, ref gameContext));
            if(result.ShouldReturn()) return result;
            if(expr is not bool){
                return result.SetError(new RunTimeError(PosStart, PosEnd,
                "If expression must be of type Bool", "Details"));
            }
            if(expr){
                expr = result.Register(((IfExprNode)Expr).ResultExpr.Visit(ref context, ref gameContext));
                if(result.ShouldReturn()) return result;
                return result.SetValue(expr);
            }
        }else{
            expr = result.Register(((TryExprNode)Expr).Expr.Visit(ref context, ref gameContext));
            if(result.Error is null){
                if(result.ShouldReturn()) return result;
                return result.SetValue(expr);
            }
        }
        if(ElExprList is not null){
            foreach(Node node in ElExprList){
                if(Expr is IfExprNode){
                    expr = result.Register(((IfExprNode)node).ConditionExpr.Visit(ref context, ref gameContext));
                    if(result.ShouldReturn()) return result;
                    if(expr is not bool){
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        "If expression must be of type Bool", "Details"));
                    }
                    if(expr){
                        expr = result.Register(((IfExprNode)node).ResultExpr.Visit(ref context, ref gameContext));
                        if(result.ShouldReturn()) return result;
                        return result.SetValue(expr);
                    }
            }else{ 
                    expr = result.Register(((TryExprNode)ElseExpr).Expr.Visit(ref context, ref gameContext));
                    if(result.Error is null){
                        if(result.ShouldReturn()) return result;
                        return result.SetValue(expr);
                    }
                }
            }
        }
        expr = result.Register(ElseExpr.Visit(ref context, ref gameContext));
        if(result.ShouldReturn()) return result;
        return result.SetValue(expr);
    }
}

public class IfExprNode : Node
{
    public Node ConditionExpr, ResultExpr;

    public IfExprNode(Node conditionExpr, Node resultExpr, Position posStart, Position posEnd) : base(posStart, posEnd, resultExpr.Type){
        ConditionExpr = conditionExpr;
        ResultExpr = resultExpr;
    }

    public override string ToString(){
        return $"If {ConditionExpr} {ResultExpr}";
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        return null;
    }
}

public class TryExprNode : Node
{
    public Node Expr;

    public TryExprNode(Node expr, Position posStart, Position posEnd) : base(posStart, posEnd, expr.Type){
        Expr = expr;
    }

    public override string ToString(){
        return $"Try {Expr} ";
    }
    
    public override RunTimeResult Visit(ref RTContext context, ref GameContext gameContext){
        return null;
    }
}