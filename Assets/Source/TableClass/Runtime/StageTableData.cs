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
  string _environmentSetting;
  public string environmentSetting { get { return _environmentSetting; } set { _environmentSetting = value; } }
  
  [SerializeField]
  string _overridingMap;
  public string overridingMap { get { return _overridingMap; } set { _overridingMap = value; } }
  
  [SerializeField]
  int _grouping;
  public int grouping { get { return _grouping; } set { _grouping = value; } }
  
  [SerializeField]
  bool _swap;
  public bool swap { get { return _swap; } set { _swap = value; } }
  
  [SerializeField]
  string _firstFixedMap;
  public string firstFixedMap { get { return _firstFixedMap; } set { _firstFixedMap = value; } }
  
  [SerializeField]
  string[] _addRandomMap = new string[0];
  public string[] addRandomMap { get { return _addRandomMap; } set { _addRandomMap = value; } }
  
  [SerializeField]
  string _defaultNormalDropId;
  public string defaultNormalDropId { get { return _defaultNormalDropId; } set { _defaultNormalDropId = value; } }
  
  [SerializeField]
  string _defaultBossDropId;
  public string defaultBossDropId { get { return _defaultBossDropId; } set { _defaultBossDropId = value; } }
  
  [SerializeField]
  float _bossHpRatioPer1Line;
  public float bossHpRatioPer1Line { get { return _bossHpRatioPer1Line; } set { _bossHpRatioPer1Line = value; } }
  
  [SerializeField]
  float _spDecreasePeriod;
  public float spDecreasePeriod { get { return _spDecreasePeriod; } set { _spDecreasePeriod = value; } }
  
}