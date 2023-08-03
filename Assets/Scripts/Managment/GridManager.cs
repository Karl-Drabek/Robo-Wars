using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GridManager : MonoBehaviour
{
    [SerializeField] private Tile _tilePrefab;
    [SerializeField] private int height, width;
    [SerializeField] private TMP_Text _terminal;
    [SerializeField] GameObject playButton;
    [SerializeField] GameObject pauseButton;
    
    private Tile [ , ] _tiles;

    private List<Robot> _robots;

    private bool isOffset, updated, counting;

    public bool IsChoosingTile;
    public RobotAssembler Assembler;

    void Start(){
        _tiles = new Tile [width, height];
    }

    public void GenerateGrid(){
        for(int x = 0; x < width; x++){
            for(int y = 0; y < height; y++){
                var tile = Instantiate(_tilePrefab, new Vector3(x - width / 2 + 0.5f, y - height / 2 + 0.5f), Quaternion.identity, this.gameObject.transform);
                _tiles[x, y] = tile;
                isOffset = (x + y) % 2 == 0;
                tile.SetColor(isOffset, this);
                tile.name = $"Tile ({x}, {y})";
            }
        }
    }

    public void DestroyTiles(){
        foreach(var tile in _tiles){
            Destroy(tile.gameObject);
        }
        _tiles = new Tile [8, 8];
    }

    public void SetChoosingTrue(){
        IsChoosingTile = true;
    }

    public void Play(){
        _robots = new();
        foreach(var tile in _tiles){ //fetch All Robots
            if(tile.Robot is not null){
                _robots.Add(tile.Robot);
            }
        }
        int n = _robots.Count;
        while (n > 0) {  
            int r = Random.Range(0, n); // Randomize Order
            n--;  
            Robot robot = _robots[r];  
            _robots[r] = _robots[n];  
            _robots[n] = robot;  
        }
        foreach(var r in _robots){ //execute code
            RunTimeResult rte = r.OnTurn(_terminal);
            if(rte.Error is not null) _terminal.text += rte.Error.ToString() + "\n";
        }
        counting = true;
    }

    void Update(){
        if(updated){
            playButton.SetActive(true);
            pauseButton.SetActive(false);
        }
        updated = counting;
    }

    public void ClearTerminal(){
        _terminal.text = string.Empty;
    }
}
