using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class NodeWarTableData
{
  [SerializeField]
  int _level;
  public int level { get { return _level; } set { _level = value; } }
  
  [SerializeField]
  int _firstRewardDiamond;
  public int firstRewardDiamond { get { return _firstRewardDiamond; } set { _firstRewardDiamond = value; } }
  
  [SerializeField]
  int _firstRewardGold;
  public int firstRewardGold { get { return _firstRewardGold; } set { _firstRewardGold = value; } }
  
  [SerializeField]
  float _standardHp;
  public float standardHp { get { return _standardHp; } set { _standardHp = value; } }
  
  [SerializeField]
  float _standardAtk;
  public float standardAtk { get { return _standardAtk; } set { _standardAtk = value; } }
  
  [SerializeField]
  string[] _environmentSetting = new string[0];
  public string[] environmentSetting { get { return _environmentSetting; } set { _environmentSetting = value; } }
  
  [SerializeField]
  string[] _addPlane = new string[0];
  public string[] addPlane { get { return _addPlane; } set { _addPlane = value; } }
  
}