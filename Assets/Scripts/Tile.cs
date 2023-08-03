using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class Tile : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Color _baseColorA, _offsetColorA, _baseColorB, _offsetColorB;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    private bool _isOffset;
    private GridManager _manager;
    public Robot Robot;

    public void SetColor(bool isOffset, GridManager manager){
        _manager = manager;
        _isOffset = isOffset;
        _spriteRenderer.color = isOffset ? _offsetColorA : _baseColorA;
    }

    public void OnPointerEnter(PointerEventData pointerEventData){
    
        _spriteRenderer.color = _isOffset ? _offsetColorB : _baseColorB;
    }

    public void OnPointerExit(PointerEventData pointerEventData)
    {
        _spriteRenderer.color = _isOffset ? _offsetColorA : _baseColorA;
    }

    public void OnPointerClick(PointerEventData pointerEventData)
    {
        if(_manager.IsChoosingTile){
            Robot = _manager.Assembler.AssembleRobot();
            Robot.transform.SetParent(this.transform, false);
            _manager.IsChoosingTile = false;
        }
    }
}
