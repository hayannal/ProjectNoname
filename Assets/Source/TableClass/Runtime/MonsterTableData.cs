using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class MonsterTableData
{
  [SerializeField]
  string _monsterId;
  public string monsterId { get { return _monsterId; } set { _monsterId = value; } }
  
  [SerializeField]
  float _multiHp;
  public float multiHp { get { return _multiHp; } set { _multiHp = value; } }
  
  [SerializeField]
  float _multiAtk;
  public float multiAtk { get { return _multiAtk; } set { _multiAtk = value; } }
  
  [SerializeField]
  float _multiDef;
  public float multiDef { get { return _multiDef; } set { _multiDef = value; } }
  
  [SerializeField]
  float _attackDelay;
  public float attackDelay { get { return _attackDelay; } set { _attackDelay = value; } }
  
  [SerializeField]
  float _moveSpeed;
  public float moveSpeed { get { return _moveSpeed; } set { _moveSpeed = value; } }
  
  [SerializeField]
  bool _boss;
  public bool boss { get { return _boss; } set { _boss = value; } }
  
  [SerializeField]
  bool _defaultDropUse;
  public bool defaultDropUse { get { return _defaultDropUse; } set { _defaultDropUse = value; } }
  
  [SerializeField]
  string _addDropId;
  public string addDropId { get { return _addDropId; } set { _addDropId = value; } }
  
  [SerializeField]
  float _initialDropSp;
  public float initialDropSp { get { return _initialDropSp; } set { _initialDropSp = value; } }
  
}