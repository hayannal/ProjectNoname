using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class ActorTableData
{
  [SerializeField]
  string _actorId;
  public string actorId { get { return _actorId; } set { _actorId = value; } }
  
  [SerializeField]
  int _grade;
  public int grade { get { return _grade; } set { _grade = value; } }
  
  [SerializeField]
  int _mainWeaponType;
  public int mainWeaponType { get { return _mainWeaponType; } set { _mainWeaponType = value; } }
  
  [SerializeField]
  float _attackDelay;
  public float attackDelay { get { return _attackDelay; } set { _attackDelay = value; } }
  
  [SerializeField]
  float _moveSpeed;
  public float moveSpeed { get { return _moveSpeed; } set { _moveSpeed = value; } }
  
}