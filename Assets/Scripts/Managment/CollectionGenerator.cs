using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class CollectionGenerator : MonoBehaviour
{
    [SerializeField] private ComponentType _ct;
    [SerializeField] private ItemDrag IDPrefab;
    private List<RobotComponent> _collection;
    private ItemDisplay _tempID;

    void Start(){
        _collection = _ct switch{
            ComponentType.Sensor => SpriteRegistrar.Sensors.Cast<RobotComponent>().ToList(),
            ComponentType.CPU => SpriteRegistrar.CPUs.Cast<RobotComponent>().ToList(),
            ComponentType.Movement => SpriteRegistrar.Movements.Cast<RobotComponent>().ToList(),
            ComponentType.Weapon => SpriteRegistrar.Weapons.Cast<RobotComponent>().ToList(),
            _ => null};

        foreach(var rc in _collection){
            _tempID = Instantiate(IDPrefab, this.transform);
            _tempID.SetUp(rc);
        }
    }
}
