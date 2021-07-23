using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class InvasionTableData
{
  [SerializeField]
  int _dayWeek;
  public int dayWeek { get { return _dayWeek; } set { _dayWeek = value; } }
  
  [SerializeField]
  int _hard;
  public int hard { get { return _hard; } set { _hard = value; } }
  
  [SerializeField]
  int _chapter;
  public int chapter { get { return _chapter; } set { _chapter = value; } }
  
  [SerializeField]
  int _stage;
  public int stage { get { return _stage; } set { _stage = value; } }
  
  [SerializeField]
  int _limitPower;
  public int limitPower { get { return _limitPower; } set { _limitPower = value; } }
  
  [SerializeField]
  string[] _limitActorId = new string[0];
  public string[] limitActorId { get { return _limitActorId; } set { _limitActorId = value; } }
  
  [SerializeField]
  string _rewardTitle;
  public string rewardTitle { get { return _rewardTitle; } set { _rewardTitle = value; } }
  
  [SerializeField]
  string _rewardType;
  public string rewardType { get { return _rewardType; } set { _rewardType = value; } }
  
  [SerializeField]
  string _rewardValue;
  public string rewardValue { get { return _rewardValue; } set { _rewardValue = value; } }
  
  [SerializeField]
  string _rewardMore;
  public string rewardMore { get { return _rewardMore; } set { _rewardMore = value; } }
  
  [SerializeField]
  string _dropId;
  public string dropId { get { return _dropId; } set { _dropId = value; } }
  
  [SerializeField]
  float _killSp;
  public float killSp { get { return _killSp; } set { _killSp = value; } }
  
}