using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Error
{
    internal Position posStart, posEnd;
    internal string error_name, details;

    public Error(Position posStart, Position posEnd, string error_name = "Error", string details = "Something went wrong"){
        this.posStart = posStart;
        this.posEnd = posEnd;
        this.error_name = error_name;
        this.details = details;
    }
        
    public override string ToString(){
        return $"{error_name}: {details}; ({posStart.CoordsToString()} to {posEnd.CoordsToString()}).";
    }
}
public class RunTimeError : Error
{
    public RunTimeError(Position posStart, Position posEnd, string error_name, string details): base(posStart, posEnd, error_name, details) {
        this.posStart = posStart;
        this.posEnd = posEnd;
        this.error_name = error_name;
        this.details = details;
    }

    public override string ToString(){
        return $"{error_name} : {details}";
    }
}
