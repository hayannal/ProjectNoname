using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class BossExpTableData
{
  [SerializeField]
  int _xpLevel;
  public int xpLevel { get { return _xpLevel; } set { _xpLevel = value; } }
  
  [SerializeField]
  int _requiredExp;
  public int requiredExp { get { return _requiredExp; } set { _requiredExp = value; } }
  
  [SerializeField]
  int _requiredAccumulatedExp;
  public int requiredAccumulatedExp { get { return _requiredAccumulatedExp; } set { _requiredAccumulatedExp = value; } }
  
}