using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class MapSetTableData
{
  [SerializeField]
  string _mapSetId;
  public string mapSetId { get { return _mapSetId; } set { _mapSetId = value; } }
  
  [SerializeField]
  string[] _normalMonsterMapEarly = new string[0];
  public string[] normalMonsterMapEarly { get { return _normalMonsterMapEarly; } set { _normalMonsterMapEarly = value; } }
  
  [SerializeField]
  string[] _angelMap = new string[0];
  public string[] angelMap { get { return _angelMap; } set { _angelMap = value; } }
  
  [SerializeField]
  string[] _normalMonsterMapLate = new string[0];
  public string[] normalMonsterMapLate { get { return _normalMonsterMapLate; } set { _normalMonsterMapLate = value; } }
  
  [SerializeField]
  string[] _rightBeforeBossMap = new string[0];
  public string[] rightBeforeBossMap { get { return _rightBeforeBossMap; } set { _rightBeforeBossMap = value; } }
  
  [SerializeField]
  string[] _bossMap = new string[0];
  public string[] bossMap { get { return _bossMap; } set { _bossMap = value; } }
  
}