using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LexerResult
{
    public List<Token> Tokens;
    public Error Error;
    public LexerResult(List<Token> tokens, Error error){
        this.Tokens = tokens;
        this.Error = error;
    }
}

public class Lexer
{

    private static string _text;
    private static int _length;
    private static Position _position;
    private static char _currentChar;
    private static bool _isEndOfFile;

    private static void Advance(){

        _position.Advance(_currentChar);

        if (_position.Index < _length){
            _currentChar = _text[_position.Index];
        }
        else{
            _isEndOfFile = true;
        }
    }

    private static void Retreate(){ // Not being used
        if (_position.Index > 1){
            _currentChar = _text[_position.Index - 1];
            _position.Retreate();
        }

    }

    private static Token MakeIdentifier(){

        Position startPosition = _position.Copy();
        var stringBuilder = new StringBuilder(_currentChar.ToString());

        Advance();

        while ((Char.IsLetter(_currentChar) || _currentChar == '_' ) && !_isEndOfFile){
            stringBuilder.Append(_currentChar.ToString());
            Advance();
        }

        string identifier = stringBuilder.ToString();
        switch (identifier){
            case "true":
                return new Token(TokenType.Bool, "True", startPosition, _position.Copy());
            case "false":
                return new Token(TokenType.Bool, "False", startPosition, _position.Copy());
            case "and":
                return new Token(TokenType.And, startPosition, _position.Copy());
            case "or":
                return new Token(TokenType.Or, startPosition, _position.Copy());
            case "not":
                return new Token(TokenType.Not, startPosition, _position.Copy());
            case "if":
                return new Token(TokenType.If, startPosition, _position.Copy());
            case "elif":
                return new Token(TokenType.Elif, startPosition, _position.Copy());
            case "try":
                return new Token(TokenType.Try, startPosition, _position.Copy());
            case "eltry":
                return new Token(TokenType.Eltry, startPosition, _position.Copy());
            case "else":
                return new Token(TokenType.Else, startPosition, _position.Copy());
            case "repeat":
                return new Token(TokenType.Repeat, startPosition, _position.Copy());
            case "loop":
                return new Token(TokenType.Loop, startPosition, _position.Copy());
            case "while":
                return new Token(TokenType.While, startPosition, _position.Copy());
            case "return":
                return new Token(TokenType.Return, startPosition, _position.Copy());
            case "continue":
                return new Token(TokenType.Continue, startPosition, _position.Copy());
            case "break":
                return new Token(TokenType.Break, startPosition, _position.Copy());
            case "int":
                return new Token(TokenType.IntType, startPosition, _position.Copy());
            case "bool":
                return new Token(TokenType.BoolType, startPosition, _position.Copy());
            case "string":
                return new Token(TokenType.StringType, startPosition, _position.Copy());
            case "double":
                return new Token(TokenType.DoubleType, startPosition, _position.Copy());
            case "func":
                return new Token(TokenType.Func, startPosition, _position.Copy());
            
        }
        return new Token(TokenType.Identifier, identifier, startPosition, _position.Copy());

    }

    private static Token MakeNumber(){

        Position startPosition = _position.Copy();
        bool hasPoint = false;
        var stringBuilder = new StringBuilder(_currentChar.ToString());

        Advance();

        while ((Char.IsNumber(_currentChar) || _currentChar == '.') && !_isEndOfFile){
            if (_currentChar == '.'){
                if (hasPoint){
                    break;
                }
                hasPoint = true;
                stringBuilder.Append(".");
            }
            else{
                stringBuilder.Append(_currentChar.ToString());
            }
            Advance();
        }
        string number = stringBuilder.ToString();
        if (hasPoint){
            return new Token(TokenType.Double, number, startPosition, _position.Copy());
        }
        else{
            return new Token(TokenType.Int, number, startPosition, _position.Copy());
        }
        
    }

    private static Token MakeString(){

        var stringBuilder = new StringBuilder();
        Position startPosition = _position.Copy();

        Advance();

        while (_currentChar != '"' && !_isEndOfFile){
            if (_currentChar == '\\'){
                Advance();
                if(_isEndOfFile){
                    break;
                }
                }
            stringBuilder.Append(_currentChar);
            Advance();
        }
        if(!_isEndOfFile){
            Advance();
        }
        return new Token(TokenType.String, stringBuilder.ToString(), startPosition, _position.Copy());
    }

    public static LexerResult MakeTokens(string inputText, string fileName){

        _text = inputText;
        _length = inputText.Length;
        _position = new Position(-1, 0, -1, fileName);
        _isEndOfFile = false;

        Advance();
        var tokens = new List<Token>();
        Position positionStart = _position.Copy();

        while (!_isEndOfFile){
            if (Char.IsWhiteSpace(_currentChar)){
                Advance();
            }
            else if (_currentChar == '#'){
                Advance();
                while (_currentChar != '#' && !_isEndOfFile){
                    Advance();
                }
                Advance();
            }
            else if (Char.IsLetter(_currentChar) || _currentChar == '_'){
                tokens.Add(MakeIdentifier());
            }
            else if (Char.IsNumber(_currentChar)){
                tokens.Add(MakeNumber());
            }
            else if (_currentChar == '"'){
                tokens.Add(MakeString());
            }
            else{
                switch (_currentChar){
                    case '+':
                        positionStart = _position.Copy();
                        Advance();
                        if (_currentChar == '+' && !_isEndOfFile){
                            Advance();
                            tokens.Add(new Token(TokenType.PP, positionStart, _position.Copy()));
                        }
                        else{
                            tokens.Add(new Token(TokenType.Plus, positionStart, _position.Copy()));
                        }
                        break;
                    case '-':
                        positionStart = _position.Copy();
                        Advance();
                        if (_currentChar == '-' && !_isEndOfFile){
                            Advance();
                            tokens.Add(new Token(TokenType.MM, positionStart, _position.Copy()));
                        }
                        else{
                            tokens.Add(new Token(TokenType.Minus, positionStart, _position.Copy()));
                        }
                        break;
                    case '*':
                        positionStart = _position.Copy();
                        tokens.Add(new Token(TokenType.Multiply, positionStart, _position.Copy()));
                        Advance();
                        break;
                    case '/':
                        positionStart = _position.Copy();
                        Advance();
                        tokens.Add(new Token(TokenType.Divide, positionStart, _position.Copy()));
                        break;
                    case '^':
                        positionStart = _position.Copy();
                        Advance();
                        tokens.Add(new Token(TokenType.Power, positionStart, _position.Copy()));
                        break;
                    case '=':
                        positionStart = _position.Copy();
                        Advance();
                        if(_isEndOfFile){
                            tokens.Add(new Token(TokenType.EQ, positionStart, _position.Copy()));
                            break;
                        }
                        switch (_currentChar){
                            case '+':
                                Advance();
                                tokens.Add(new Token(TokenType.EPlus, positionStart, _position.Copy()));
                                break;
                            case '-':
                                Advance();
                                tokens.Add(new Token(TokenType.EMinus, positionStart, _position.Copy()));
                                break;
                            case '*':
                                Advance();
                                tokens.Add(new Token(TokenType.EMult, positionStart, _position.Copy()));
                                break;
                            case '/':
                                Advance();
                                tokens.Add(new Token(TokenType.EDiv, positionStart, _position.Copy()));
                                break;
                            case '^':
                                Advance();
                                tokens.Add(new Token(TokenType.EPow, positionStart, _position.Copy()));
                                break;
                            case '=':
                                Advance();
                                tokens.Add(new Token(TokenType.EE, positionStart, _position.Copy()));
                                break;
                            default:
                                tokens.Add(new Token(TokenType.EQ, positionStart, _position.Copy()));
                                break;
                        }
                        break;
                    case '<':
                        positionStart = _position.Copy();
                        Advance();
                        if (_currentChar == '=' && !_isEndOfFile){
                            Advance();
                            tokens.Add(new Token(TokenType.LTE, positionStart, _position.Copy()));
                        }
                        else{
                            tokens.Add(new Token(TokenType.LT, positionStart, _position.Copy()));
                        }
                        break;
                    case '>':
                        positionStart = _position.Copy();
                        Advance();
                        if (_currentChar == '=' && !_isEndOfFile){
                            Advance();
                            tokens.Add(new Token(TokenType.GTE, positionStart, _position.Copy()));
                        }
                        else{
                            tokens.Add(new Token(TokenType.GT, positionStart, _position.Copy()));
                        }
                        break;
                    case '(':
                        positionStart = _position.Copy();
                        Advance();
                        tokens.Add(new Token(TokenType.LParen, positionStart, _position.Copy()));
                        break;
                    case ')':
                        positionStart = _position.Copy();
                        Advance();
                        tokens.Add(new Token(TokenType.RParen, positionStart, _position.Copy()));
                        break;
                    case '{':
                        positionStart = _position.Copy();
                        Advance();
                        tokens.Add(new Token(TokenType.LBrace, positionStart, _position.Copy()));
                        break;
                    case '}':
                        positionStart = _position.Copy();
                        Advance();
                        tokens.Add(new Token(TokenType.RBrace, positionStart, _position.Copy()));
                        break;
                    case '[':
                        positionStart = _position.Copy();
                        Advance();
                        tokens.Add(new Token(TokenType.LBrack, positionStart, _position.Copy()));
                        break;
                    case ']':
                        positionStart = _position.Copy();
                        Advance();
                        tokens.Add(new Token(TokenType.RBrack, positionStart, _position.Copy()));
                        break;
                    case ',':
                        positionStart = _position.Copy();
                        Advance();
                        tokens.Add(new Token(TokenType.Comma, positionStart, _position.Copy()));
                        break;
                    case ';':
                        positionStart = _position.Copy();
                        Advance();
                        tokens.Add(new Token(TokenType.SC, positionStart, _position.Copy()));
                        break;
                    case ':':
                        positionStart = _position.Copy();
                        Advance();
                        tokens.Add(new Token(TokenType.Collon, positionStart, _position.Copy()));
                        break;
                    case '.':
                        positionStart = _position.Copy();
                        Advance();
                        tokens.Add(new Token(TokenType.Point, positionStart, _position.Copy()));
                        break;
                    case '!':
                        positionStart = _position.Copy();
                        Advance();
                        if(_isEndOfFile){
                            tokens.Add(new Token(TokenType.Not, positionStart, _position.Copy()));
                            break;
                        }
                        switch(_currentChar){
                            case '=':
                                Advance();
                                tokens.Add(new Token(TokenType.NE, positionStart, _position.Copy()));
                                break;
                            default:
                                tokens.Add(new Token(TokenType.Not, positionStart, _position.Copy()));
                                break;
                        }
                        break;
                    case '|':
                        positionStart = _position.Copy();
                        Advance();
                        tokens.Add(new Token(TokenType.Or, positionStart, _position.Copy()));
                        break;
                    case '&':
                        positionStart = _position.Copy();
                        Advance();
                        tokens.Add(new Token(TokenType.And, positionStart, _position.Copy()));
                        break;
                    default:
                        positionStart = _position.Copy();
                        char character = _currentChar;
                        Advance();
                        return new LexerResult(null , new Error(positionStart, _position.Copy(), "Illegal Character", $"\"{character}\""));


                }
            }
        }
        positionStart = _position.Copy();
        Advance();
        tokens.Add(new Token(TokenType.EOF, positionStart, _position.Copy()));
        return new LexerResult(tokens, null);
    }
}
