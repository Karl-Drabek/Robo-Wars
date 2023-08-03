using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TokenType{
    Int, String, Double, Bool, Identifier,
    Func, IntType, StringType, DoubleType, BoolType,  
    Plus, Minus, Multiply, Divide, Power,
    EQ, EPlus, EMinus, EMult, EDiv, EPow, PP, MM,
    LParen, RParen, LBrace, RBrace, LBrack, RBrack,
    EE, NE, LT, GT, LTE, GTE,
    Comma, SC, Point, Collon,
    And, Or, Not,
    If, Elif, Try, Eltry, Else,
    Repeat, Loop, While,
    Return, Continue, Break,
    EOF
}

public class Token
{
    public string Value;
    public TokenType Type;
    public Position PosStart, PosEnd;

    public Token(TokenType type, Position posStart, Position posEnd){
        Type = type;
        PosStart = posStart;
        PosEnd = posEnd;
    }

    public Token(TokenType type, string value, Position posStart, Position posEnd){
        Type = type;
        Value = value;
        PosStart = posStart;
        PosEnd = posEnd;
    }

    public override string ToString(){
        if(Value is not null){
            return "[" + Type.ToString() + ", " + Value + "]";
        }
        return "[" + Type.ToString() + "]";
    }
}