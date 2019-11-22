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
  string[] _normalMonsterMap = new string[0];
  public string[] normalMonsterMap { get { return _normalMonsterMap; } set { _normalMonsterMap = value; } }
  
  [SerializeField]
  string[] _angelMap = new string[0];
  public string[] angelMap { get { return _angelMap; } set { _angelMap = value; } }
  
  [SerializeField]
  string[] _bossMap = new string[0];
  public string[] bossMap { get { return _bossMap; } set { _bossMap = value; } }
  
}