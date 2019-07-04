using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class StageTableData
{
  [SerializeField]
  string _id;
  public string id { get { return _id; } set { _id = value; } }
  
  [SerializeField]
  int _chapter;
  public int chapter { get { return _chapter; } set { _chapter = value; } }
  
  [SerializeField]
  int _stage;
  public int stage { get { return _stage; } set { _stage = value; } }
  
  [SerializeField]
  float _standardHp;
  public float standardHp { get { return _standardHp; } set { _standardHp = value; } }
  
  [SerializeField]
  float _standardAtk;
  public float standardAtk { get { return _standardAtk; } set { _standardAtk = value; } }
  
  [SerializeField]
  float _standardDef;
  public float standardDef { get { return _standardDef; } set { _standardDef = value; } }
  
  [SerializeField]
  string _overridingMap;
  public string overridingMap { get { return _overridingMap; } set { _overridingMap = value; } }
  
  [SerializeField]
  int _grouping;
  public int grouping { get { return _grouping; } set { _grouping = value; } }
  
  [SerializeField]
  string _firstFixedMap;
  public string firstFixedMap { get { return _firstFixedMap; } set { _firstFixedMap = value; } }
  
  [SerializeField]
  string _addRandomMap;
  public string addRandomMap { get { return _addRandomMap; } set { _addRandomMap = value; } }
  
}