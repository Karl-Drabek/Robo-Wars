using System.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//TODO variables, classes and details. Make if stuff one node;
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

    public abstract RunTimeResult Visit(ref Context context);
}

public class IntNode : Node
{
    public int Value;
    
    public IntNode(int value, Position posStart, Position posEnd) : base(posStart, posEnd, new Type("Int")){
        Value = value;
    }
    
    public override string ToString(){
        return Value.ToString();
    }

    public override RunTimeResult Visit(ref Context context) => (new RunTimeResult()).SetValue(Value);
}

public class DoubleNode : Node
{
    public double Value;
    
    public DoubleNode(double value, Position posStart, Position posEnd) : base(posStart, posEnd, new Type("Double")){
        Value = value;
    }
    
    public override string ToString(){
        return Value.ToString();
    }

    public override RunTimeResult Visit(ref Context context) => (new RunTimeResult()).SetValue(Value);
}

public class StringNode : Node
{
    public string Value;

    public StringNode(string value, Position posStart, Position posEnd) : base(posStart, posEnd, new Type("String")){
        Value = value;
    }

    public override string ToString(){
        return $"\"{Value}\"";
    }
    
    public override RunTimeResult Visit(ref Context context) => (new RunTimeResult()).SetValue(Value);
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
    
    public override RunTimeResult Visit(ref Context context){
        RunTimeResult result = new();
        if(context.SymbolTable.Contains(Value)){
            dynamic value = context.SymbolTable.Get(Value);
            return result.SetValue(value);
        }
        return result.SetError(new RunTimeError(PosStart, PosEnd, "Identifier Error", $"The identifier {Value} does not exist in the current context"));
    }
}

public class BoolNode : Node
{
    public bool Value;

    public BoolNode(bool value, Position posStart, Position posEnd) : base(posStart, posEnd, new Type("Bool")){
        Value = value;
    }
    public override string ToString(){
        return Value.ToString();
    }
    
    public override RunTimeResult Visit(ref Context context) => (new RunTimeResult()).SetValue(Value);
}

public class StatementsNode : Node
{
    public List<Node> Nodes;

    public StatementsNode(List<Node> nodes, Position posStart, Position posEnd) : base(posStart, posEnd, new Type("Void")){
        Nodes = nodes;
    }

    public override string ToString(){
        return $"[{CommaList<Node>(Nodes)}]";
    }
    
    public override RunTimeResult Visit(ref Context context){
        RunTimeResult result = new();
        foreach(Node node in Nodes){
            result.Register(node.Visit(ref context));
            if(result.ShouldReturn()) return result;
        }
        return null;
    }
}

public class JumpNode : Node
{
    public TokenType Value;
    public Node Expresion;

    public JumpNode(TokenType value, Position posStart, Position posEnd, Node expresion = default) : base(posStart, posEnd, new Type("Void")){
        Value = value;
        Expresion = expresion;
    }

    public override string ToString(){
        if (Expresion is null) return Value.ToString();
        return $"{Value.ToString()} {Expresion.ToString()}";
    }
    
    public override RunTimeResult Visit(ref Context context){
        switch(Value){
            case TokenType.Continue:
                return (new RunTimeResult()).SetContinue(true);
            case TokenType.Break:
                return (new RunTimeResult()).SetBreak(true);
            case TokenType.Return:
                RunTimeResult result = new();
                if(Expresion is null) return result.SetReturn(true).SetValue(null);
                dynamic expr = result.Register(Expresion.Visit(ref context));
                if(result.ShouldReturn()) return result;
                return (result).SetReturn(true).SetValue(expr);
            default:
                return null;
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

    public override RunTimeResult Visit(ref Context context){
        RunTimeResult result = new();

        dynamic left = result.Register(Left.Visit(ref context));
        if(result.ShouldReturn()) return result;
        dynamic right = result.Register(Right.Visit(ref context));
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
                            "Cannot add operands of types {typeof(left)} and {typeof(right)}", "Details"));
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
                            "Cannot subtract operands of types {typeof(left)} and {typeof(right)}", "Details"));
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
                            "Cannot multiply operands of types {typeof(left)} and {typeof(right)}", "Details"));
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
                            "Cannot divide operands of types {typeof(left)} and {typeof(right)}", "Details"));
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
                        "Cannot exponentiate operands of types {typeof(left)} and {typeof(right)}", "Details"));
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
                        "Cannot determine equality for operands of types {typeof(left)} and {typeof(right)}", "Details"));
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
                        "Cannot determine inequality for operands of types {typeof(left)} and {typeof(right)}", "Details"));
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
                        "Cannot determine greater than for operands of types {typeof(left)} and {typeof(right)}", "Details"));
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
                        "Cannot determine less than for operands of types {typeof(left)} and {typeof(right)}", "Details"));
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
                        "Cannot determine less than or equals for operands of types {typeof(left)} and {typeof(right)}", "Details"));
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
                        "Cannot determine greater than or equals for operands of types {typeof(left)} and {typeof(right)}", "Details"));
                }
            case TokenType.And:
                switch (left, right){
                    case (bool, bool):
                        return result.SetValue(left && right);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        "Cannot apply and for operands of types {typeof(left)} and {typeof(right)}", "Details"));
                }
            case TokenType.Or:
                switch (left, right){
                    case (bool, bool):
                        return result.SetValue(left || right);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        "Cannot apply or for operands of types {typeof(left)} and {typeof(right)}", "Details"));
                }
            default:
                return null;
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
    
    public override RunTimeResult Visit(ref Context context){
        RunTimeResult result = new();
        dynamic node = result.Register(Node.Visit(ref context));
        if(result.ShouldReturn()) return result;
        switch(Op){
            case TokenType.Minus:
                switch(node){
                    case int:
                    case double:
                        return result.SetValue(-node);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        "Cannot apply minus to type {typeof(node)}", "Details"));
                }
            case TokenType.Not:
                switch(node){
                    case bool:
                        return result.SetValue(!node);
                    default:
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        "Cannot apply not to type {typeof(node)}", "Details"));
                }
            default:
                return null;
        }
    }
}

public class LoopNode : Node
{
    public Node[] Expresions;
    public Node Statements;

    public LoopNode(Node[] expresions, Node statements, Position posStart, Position posEnd) : base(posStart, posEnd, new Type("Void")){
        Expresions = expresions;
        Statements = statements;
    }

    public override string ToString(){
        return $"Loop({Expresions[0]}, {Expresions[1]}, {Expresions[2]}){{{Statements}}}";
    }
    
    public override RunTimeResult Visit(ref Context context){
        RunTimeResult result = new();
        dynamic expr;

        result.Register(Expresions[1].Visit(ref context));
        if(result.ShouldReturn()) return result;
        expr = result.Register(Expresions[2].Visit(ref context));
        if(result.ShouldReturn()) return result;
        if(expr is not bool){
            return result.SetError(new RunTimeError(PosStart, PosEnd,
            "Repeat expression must be of type Bool", "Details"));
        }
        while(expr){
            result.Register(Statements.Visit(ref context));
            if(result.HasBreak) break; //continue is uneccessary 
            if(result.ShouldReturn()) return result;
            result.Register(Expresions[3].Visit(ref context));
            if(result.ShouldReturn()) return result;
            expr = result.Register(Expresions[2].Visit(ref context));
            if(result.ShouldReturn()) return result;
        }
        return null;
    }
}

public class RepeatNode : Node
{
    public Node Expr, Statements;

    public RepeatNode(Node expr, Node statements, Position posStart, Position posEnd) : base(posStart, posEnd, new Type("Void")){
        Expr = expr;
        Statements = statements;
    }

    public override string ToString(){
        return $"Repeat({Expr}){{{Statements}}})";
    }
    
    public override RunTimeResult Visit(ref Context context){
        RunTimeResult result = new();

        dynamic expr = result.Register(Expr.Visit(ref context));
        if(result.ShouldReturn()) return result;
        if(expr is not int){
            return result.SetError(new RunTimeError(PosStart, PosEnd,
            "Repeat expression must be of type Int", "Details"));
        }
        for(int i = 0; i < expr; i++){
            result.Register(Statements.Visit(ref context));
            if(result.HasBreak) break; //continue is uneccessary 
            if(result.ShouldReturn()) return result;
        }
        return null;
    }
}

public class WhileNode : Node
{
    public Node Expr, Statements;

    public WhileNode(Node expr, Node statements, Position posStart, Position posEnd) : base(posStart, posEnd, new Type("Void")){
        Expr = expr;
        Statements = statements;
    }

    public override string ToString(){
        return $"While({Expr}){{{Statements}}})";
    }
    
    public override RunTimeResult Visit(ref Context context){
        return null;
    }
}

/*RunTimeResult result = new();

    dynamic value = result.Register(Nodes[0].Visit(ref context));
    if(result.ShouldReturn()) return result;

    for(int i = 0; i < Nodes.Count - 1; i++){       
        if(value is not ObjectValue) return result.SetError(new RunTimeError(PosStart, PosEnd, "Run Time Error", "Point call can only be performed on type object"));
        if(Nodes[i + 1] is not IdentifierNode) return result.SetError(new RunTimeError(PosStart, PosEnd, "Run Time Error", "Point call must call to an identifier"));
        IdentifierNode tempNode = Nodes[i + 1] as IdentifierNode;
        if(!(value.Dictionary.ContainsKey(tempNode.Value))) return result.SetError(new RunTimeError(PosStart, PosEnd, "Run Time Error", $"{value.ToString()} does not contain a defenition for {tempNode.ToString()}"));
        value = value.Dictionary[tempNode.Value];
    }
    return result.SetValue(value);*/

public class PointCallNode : Node
{
    public Node Left, Right;

    public PointCallNode(Node left, Node right, Position posStart, Position posEnd, Type type) : base(posStart, posEnd, type){
        Left = left;
        Right = right;
    }

    public override string ToString(){
        return $"{Left}.{Right}";
    }
    
    public override RunTimeResult Visit(ref Context context){
        return null;
    }
}

public class ClassNode : Node
{
    public Node Statements;
    public List<Type> Types;
    public List<string> Identifiers, InheritedIdentifiers;

    public ClassNode(List<Type> types, List<string> identifiers, List<string> inheritedIdentifiers, Node statements, Position posStart, Position posEnd) : base(posStart, posEnd, new Type("Void")){
        Types = types;
        Identifiers = identifiers;
        InheritedIdentifiers = inheritedIdentifiers;
        Statements = statements;
    }

    public override string ToString(){
        if(InheritedIdentifiers is not null){
            return $"{Type}[{CommaList<Type, string>(Types, Identifiers)}] From {CommaList<string>(InheritedIdentifiers)} {{{Statements}}}";
        }
        return $"{Type}[{CommaList<Type, string>(Types, Identifiers)}] {{{Statements}}}";
    }  
    
    public override RunTimeResult Visit(ref Context context){
        return (new RunTimeResult()).SetValue(new Class(Type.BaseType, Types, Identifiers, InheritedIdentifiers, Statements));
    }
}

public class FuncNode : Node
{
    public List<string> Identifiers;
    public Node Statements;

    public FuncNode(List<string> identifiers, Node statements, Position posStart, Position posEnd, Type type) : base(posStart, posEnd, type){
        Identifiers = identifiers;
        Statements = statements;
    }

    public override string ToString(){
        return $"Func ({CommaList<string>(Identifiers)}){{{Statements}}}";
    }
    
    public override RunTimeResult Visit(ref Context context){
        return (new RunTimeResult()).SetValue(new Function(Identifiers, Statements, context.SymbolTable));
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

    public override RunTimeResult Visit(ref Context context){
        return null;
    }
}

/*    public override RunTimeResult Visit(ref Context context){
    RunTimeResult result = new();
    dynamic expr = null;
    string Identifier = String.Empty;
    Dictionary<string, dynamic> dictionary = new();
    if(CallNode is IdentifierNode){
        var idNode = CallNode as IdentifierNode;
        Identifier = idNode.Value;
        while(!dictionary.ContainsKey(Identifier)){
            if(context.SymbolTable.Parent is not null){
                dictionary = context.SymbolTable.Symbols;
                continue; 
            }else break;
        }
    }else if(CallNode is PointCallNode){
        var pcNode = CallNode as PointCallNode;
        dynamic value = result.Register(pcNode.Nodes[0].Visit(ref context));
        if(result.ShouldReturn()) return result;
        IdentifierNode tempNode = null;
        for(int i = 0; i < pcNode.Nodes.Count - 1; i++){       
            if(value is not ObjectValue) return result.SetError(new RunTimeError(PosStart, PosEnd, "Run Time Error", "Point call can only be performed on type object"));
            if(pcNode.Nodes[i + 1] is not IdentifierNode) return result.SetError(new RunTimeError(PosStart, PosEnd, "Run Time Error", "Point call must call to an identifier"));
            tempNode = pcNode.Nodes[i + 1] as IdentifierNode;
            if(!(value.Dictionary.ContainsKey(tempNode.Value))) return result.SetError(new RunTimeError(PosStart, PosEnd, "Run Time Error", $"{value.ToString()} does not contain a defenition for {tempNode.ToString()}"));
            if(i != pcNode.Nodes.Count - 2) value = value.Dictionary[tempNode.Value];
        }
        Identifier = tempNode.Value; 
        dictionary = value.Dictionary;
    }
    if(dictionary.ContainsKey(Identifier)){
        if(Expresion is not null){
            expr = result.Register(Expresion.Visit(ref context));
        }
        if(result.ShouldReturn()) return result;
        switch(AssignmentOp){
            case(TokenType.EQ):
                return result.SetValue(dictionary[Identifier] = expr);
            case(TokenType.EPlus):
                if(dictionary[Identifier] is int || dictionary[Identifier] is double && expr is int || expr is double){
                    return result.SetValue(dictionary[Identifier] += expr);
                }
                return result.SetError(new RunTimeError(PosStart, PosEnd, "Run Time Error", $"Cannot perform Equals Plus between types of {dictionary[Identifier].GetType()} and {expr.GetType()}"));
            case(TokenType.EMinus):
                if(dictionary[Identifier] is int || dictionary[Identifier] is double && expr is int || expr is double){
                return result.SetValue(dictionary[Identifier] -= expr);
                }
                return result.SetError(new RunTimeError(PosStart, PosEnd, "Run Time Error", $"Cannot perform Equals Minus between types of {dictionary[Identifier].GetType()} and {expr.GetType()}"));
            case(TokenType.EMult):
                if(dictionary[Identifier] is int || dictionary[Identifier] is double && expr is int || expr is double){
                return result.SetValue(dictionary[Identifier] *= expr);
                }
                return result.SetError(new RunTimeError(PosStart, PosEnd, "Run Time Error", $"Cannot perform Equals Multiply between types of {dictionary[Identifier].GetType()} and {expr.GetType()}"));
            case(TokenType.EDiv):
                if(dictionary[Identifier] is int || dictionary[Identifier] is double && expr is int || expr is double){
                return result.SetValue(dictionary[Identifier] /= expr);
                }
                return result.SetError(new RunTimeError(PosStart, PosEnd, "Run Time Error", $"Cannot perform Equals divide between types of {dictionary[Identifier].GetType()} and {expr.GetType()}"));
            case(TokenType.EPow):
                if(dictionary[Identifier] is int || dictionary[Identifier] is double && expr is int || expr is double){
                return result.SetValue(dictionary[Identifier] = Math.Pow(dictionary[Identifier], expr));
                }
                return result.SetError(new RunTimeError(PosStart, PosEnd, "Run Time Error", $"Cannot perform Equals Power between types of {dictionary[Identifier].GetType()} and {expr.GetType()}"));
            case(TokenType.PP):
                if(dictionary[Identifier] is int) return result.SetValue(++dictionary[Identifier]);
                else return result.SetError(new RunTimeError(PosStart, PosEnd, "Run Time Error", "Plus Plus assignment must be made to type int"));
            case(TokenType.MM):
                if(dictionary[Identifier] is int) return result.SetValue(--dictionary[Identifier]);
                else return result.SetError(new RunTimeError(PosStart, PosEnd, "Run Time Error", "Minus Minus assignment must be made to type int"));
            default:
                return null;
        }
    }else if(AssignmentOp == TokenType.EQ){
        expr = result.Register(Expresion.Visit(ref context));
        if(result.ShouldReturn()) return result;
        dictionary.Add(Identifier, expr);
        return result.SetValue(expr);
    }else{
        return result.SetError(new RunTimeError(PosStart, PosEnd, "Run Time Error", $"Identifier {Identifier} does not exists in the current context"));
    }
}*/

public class SetVarNode : Node
{
    public Node CallNode, Expresion;
    public TokenType AssignmentOp;

    public SetVarNode (Node callNode, TokenType assignmentOp, Position posStart, Position posEnd, Type type, Node expresion = default) : base(posStart, posEnd, type){
        CallNode = callNode;
        AssignmentOp = assignmentOp;
        Expresion = expresion;
    }

    public override string ToString(){
        if(Expresion is not null){
            return $"{CallNode} {AssignmentOp} {Expresion}";
        }
        return $"{CallNode} {AssignmentOp}";
    }
    public override RunTimeResult Visit(ref Context context){
        return null;
    }
}
/*
public override RunTimeResult Visit(ref Context context){
    RunTimeResult result = new();
    dynamic value;
    dynamic callValue = Node.Visit(ref context);

    if(callValue is Class){
        foreach(var n in callValue.Expresions){
            callValue = result.Register(n.Visit(ref context));
            if(result.ShouldReturn()) return result;
            
        }
    }else if(callValue is Function){
        var childContext = new Context(new SymbolTable(callValue.SymbolTable), new TraceBack(callValue.Name, context.TraceBack, PosStart));
        for(int i = 0; i < callValue.Identifiers.Length; i++){
            if(childContext.SymbolTable.Symbols.ContainsKey(callValue.Identifiers[i])) return result.SetError(new RunTimeError(PosStart, PosEnd, "Run Time Error", "Identifier {Identifier} already exists in the current context"));
            value = result.Register(AccessNodes[i].Visit(ref context));
            if(result.ShouldReturn()) return result;
            childContext.SymbolTable.Symbols.Add(callValue.Identifiers[i], value);
        }
        value = result.Register(callValue.Statements.Visit(childContext));
        if(result.Error is not null) return result;
        return result.Clear().SetValue(value);
    }
    return result.SetError(new RunTimeError(PosStart, PosEnd, "Invalid Type Error", "Use of () must be on type function or class"));
}
*/
public class FuncCallNode : Node
{
    public Node Node;
    public List<Node> AccessNodes;

    public FuncCallNode(Node node, List<Node> accessNodes, Position posStart, Position posEnd, Type type) : base(posStart, posEnd, type){
        Node = node;
        AccessNodes = accessNodes;
    }

    public override string ToString(){
        return $"{Node}({CommaList<Node>(AccessNodes)})";
    }
    
    public override RunTimeResult Visit(ref Context context){
        return null;
    }
}

public class ObjCallNode : Node
{
    public Node Node;
    public List<Node> AccessNodes;

    public ObjCallNode(Node node, List<Node> accessNodes, Position posStart, Position posEnd, Type type) : base(posStart, posEnd, type){
        Node = node;
        AccessNodes = accessNodes;
    }

    public override string ToString(){
        return $"{Node}[{CommaList<Node>(AccessNodes)}]";
    }
    
    public override RunTimeResult Visit(ref Context context){
        return null;
    }
}

public class GenCallNode : Node
{
    public Node Node;
    public List<string> AccessIds;

    public GenCallNode(Node node, List<string> accessIds, Position posStart, Position posEnd, Type type) : base(posStart, posEnd, type){
        Node = node;
        AccessIds = accessIds;
    }

    public override string ToString(){
        return $"{Node}[{CommaList<string>(AccessIds)}]";
    }
    
    public override RunTimeResult Visit(ref Context context){
        return null;
    }
}

public class ConditionNode : Node
{
    public Node Expr, ElseStatements;
    public List<Node> ElExprList;

    public ConditionNode(Node expr, List<Node> elExprList, Node elseStatements, Position posStart, Position posEnd) : base(posStart, posEnd, new Type("Void")){
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
    
    public override RunTimeResult Visit(ref Context context){
        RunTimeResult result = new();
        dynamic expr;

        if(Expr is IfNode){
            expr = result.Register(((IfNode)Expr).Expr.Visit(ref context));
            if(result.ShouldReturn()) return result;
            if(expr is not bool){
                return result.SetError(new RunTimeError(PosStart, PosEnd,
                "If expression must be of type Bool", "Details"));
            }
            if(expr){
                return result.Register(((IfNode)Expr).Statements.Visit(ref context));
            }
        }else{
            result.Register(((TryNode)Expr).Statements.Visit(ref context));
            if(result.Error is null){
                if(result.ShouldReturn()) return result;
                return null;
            }
        }
        if(ElExprList is not null){
            foreach(Node node in ElExprList){
                if(Expr is IfNode){
                    expr = result.Register(((IfNode)node).Expr.Visit(ref context));
                    if(result.ShouldReturn()) return result;
                    if(expr is not bool){
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        "If expression must be of type Bool", "Details"));
                    }
                    if(expr){
                        return result.Register(((IfNode)node).Statements.Visit(ref context));
                    }
                return null;
                }else{ 
                    result.Register(((TryNode)node).Statements.Visit(ref context));
                    if(result.Error is null){
                        if(result.ShouldReturn()) return result;
                        return null;
                    }
                }
            }
        }
        if(ElseStatements is not null){
            result.Register(ElseStatements.Visit(ref context));
            if(result.ShouldReturn()) return result;
        }
        return null;
    }
}

public class IfNode : Node
{
    public Node Expr, Statements;

    public IfNode(Node expr, Node statements, Position posStart, Position posEnd) : base(posStart, posEnd, new Type("Void")){
        Expr = expr;
        Statements = statements;
    }

    public override string ToString(){
        return $"If({Expr}){{{Statements}}}";
    }
    
    public override RunTimeResult Visit(ref Context context){
        return null;
    }
}

public class TryNode : Node
{
    public Node Statements;

    public TryNode(Node statements, Position posStart, Position posEnd) : base(posStart, posEnd, new Type("Void")){
        Statements = statements;
    }

    public override string ToString(){
        return $"Try{{{Statements}}}";
    }
    
    public override RunTimeResult Visit(ref Context context){
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
    
    public override RunTimeResult Visit(ref Context context){
        RunTimeResult result = new();
        dynamic expr;

        if(Expr is IfExprNode){
            expr = result.Register(((IfExprNode)Expr).ConditionExpr.Visit(ref context));
            if(result.ShouldReturn()) return result;
            if(expr is not bool){
                return result.SetError(new RunTimeError(PosStart, PosEnd,
                "If expression must be of type Bool", "Details"));
            }
            if(expr){
                expr = result.Register(((IfExprNode)Expr).ResultExpr.Visit(ref context));
                if(result.ShouldReturn()) return result;
                return result.SetValue(expr);
            }
        }else{
            expr = result.Register(((TryExprNode)Expr).Expr.Visit(ref context));
            if(result.Error is null){
                if(result.ShouldReturn()) return result;
                return result.SetValue(expr);
            }
        }
        if(ElExprList is not null){
            foreach(Node node in ElExprList){
                if(Expr is IfExprNode){
                    expr = result.Register(((IfExprNode)node).ConditionExpr.Visit(ref context));
                    if(result.ShouldReturn()) return result;
                    if(expr is not bool){
                        return result.SetError(new RunTimeError(PosStart, PosEnd,
                        "If expression must be of type Bool", "Details"));
                    }
                    if(expr){
                        expr = result.Register(((IfExprNode)node).ResultExpr.Visit(ref context));
                        if(result.ShouldReturn()) return result;
                        return result.SetValue(expr);
                    }
            }else{ 
                    expr = result.Register(((TryExprNode)ElseExpr).Expr.Visit(ref context));
                    if(result.Error is null){
                        if(result.ShouldReturn()) return result;
                        return result.SetValue(expr);
                    }
                }
            }
        }
        expr = result.Register(ElseExpr.Visit(ref context));
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
    
    public override RunTimeResult Visit(ref Context context){
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
    
    public override RunTimeResult Visit(ref Context context){
        return null;
    }
}