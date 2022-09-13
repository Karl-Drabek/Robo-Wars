using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUITextInput : MonoBehaviour
{
    private string _text = System.String.Empty;
    private Vector2 _scrollPosition;
    private float _width = Screen.width;
    private float _height = Screen.height;
    private float _padding = 50f;

    private void OnGUI() {
        GUILayout.BeginArea(new Rect(_padding/2, _padding/2, _width - _padding, _height - _padding));
        GUILayout.Label("Main");
        _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
        _text = GUILayout.TextArea(_text); 
        GUILayout.EndScrollView();
        if(GUILayout.Button("Make Tokens")){
            PrintTokens(_text);
        }
        if(GUILayout.Button("Parse")){
            PrintParseNode(_text);
        }
        GUILayout.EndArea();
    }

    private void PrintTokens(string text){
        LexerResult lexerResult = Lexer.MakeTokens(text, "test");
        if (lexerResult.Error is not null){
            print(lexerResult.Error.ToString());
        }
        else{
        StringBuilder output = new StringBuilder();
        int length = lexerResult.Tokens.Count;
        for(int i = 0; i < length; i++){
            output.Append(lexerResult.Tokens[i].ToString());
            if(i != length - 1){
            output.Append(", ");
            }
        }
        print($"({output})");
        }
    }

    private void PrintParseNode(string text){
        LexerResult lexerResult = Lexer.MakeTokens(text, "test");
        if (lexerResult.Error is not null){
            print(lexerResult.Error.ToString());
        }
        ParseResult parseResult = Parser.Parse(lexerResult.Tokens);
        if (parseResult.Error is not null){
            print(parseResult.Error.ToString());
            
        }
        else{
            print(parseResult.Node);
        }
    }
}
