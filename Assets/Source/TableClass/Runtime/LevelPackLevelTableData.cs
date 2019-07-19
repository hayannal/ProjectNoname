using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class LevelPackLevelTableData
{
  [SerializeField]
  string _levelPackId;
  public string levelPackId { get { return _levelPackId; } set { _levelPackId = value; } }
  
  [SerializeField]
  int _level;
  public int level { get { return _level; } set { _level = value; } }
  
  [SerializeField]
  string[] _affectorValueId = new string[0];
  public string[] affectorValueId { get { return _affectorValueId; } set { _affectorValueId = value; } }
  
  [SerializeField]
  string[] _parameter = new string[0];
  public string[] parameter { get { return _parameter; } set { _parameter = value; } }
  
}