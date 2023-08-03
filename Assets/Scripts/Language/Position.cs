using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Position
{
    public int Index;
    private int _line, _column;
    private string _fileName, _fileText;
    private List<int> _lineLengths;

    public Position(int index, int line, int column, string fileName){
        this.Index = index;
        this._line = line;
        this._column = column;
        this._fileName = fileName;
        this._lineLengths = new List<int>();
    }

    public void Advance(char currentChar){
        this.Index += 1;
        this._column += 1;
        if(currentChar == '\n'){
            _lineLengths.Add(_column);
            this._line += 1;
            this._column = 0;
        }   
    }

    public void Retreate(){ //Only used in Lexer.Retreate which is not used
        this.Index -= 1;
        if(this._column > 0){
            this._column -= 1;
        }
        else{
            this._line -= 1;
            this._column = _lineLengths[_line];
        }
    }

    public Position Copy(){
        return new Position(Index, _line, _column, _fileName);
    }

    public string CoordsToString(){
        return $"[line: {_line + 1}, Column: {_column + 1}]";   
    }
}
