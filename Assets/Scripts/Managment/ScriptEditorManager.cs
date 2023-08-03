using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScriptEditorManager : MonoBehaviour
{
    [SerializeField] private TMP_Text _terminal;
    [SerializeField] private TMP_InputField _editor;
    [SerializeField] private RobotAssembler _assembler;
    public ParseResult ParseResult;
    private List<FuncType> _funcs;
    

    public void Compile(){
        LexerResult lexerResult = Lexer.MakeTokens(_editor.text, "test");
        if(lexerResult.Error is not null){
            _terminal.text += lexerResult.Error.ToString() + "\n";
            return;
        }

        _funcs.AddRange(Main.FuncTypes);
        _funcs.AddRange(_assembler.GetFuncTypes());

        ParseResult = Parser.Parse(lexerResult.Tokens, _funcs);
        if(ParseResult.Error is not null){
            _terminal.text += ParseResult.Error.ToString() + "\n";
            return;
        }
        _terminal.text += "Compilation Successfull" + "\n";
    }
  
    public void ClearTerminal(){
        _terminal.text = string.Empty;
    }
}
