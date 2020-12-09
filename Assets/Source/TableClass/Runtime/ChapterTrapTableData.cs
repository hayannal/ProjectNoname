using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class ChapterTrapTableData
{
  [SerializeField]
  string _trapAddress;
  public string trapAddress { get { return _trapAddress; } set { _trapAddress = value; } }
  
  [SerializeField]
  int _chapter;
  public int chapter { get { return _chapter; } set { _chapter = value; } }
  
  [SerializeField]
  int _last;
  public int last { get { return _last; } set { _last = value; } }
  
  [SerializeField]
  float _firstWaiting;
  public float firstWaiting { get { return _firstWaiting; } set { _firstWaiting = value; } }
  
  [SerializeField]
  float _minPeriod;
  public float minPeriod { get { return _minPeriod; } set { _minPeriod = value; } }
  
  [SerializeField]
  float _maxPeriod;
  public float maxPeriod { get { return _maxPeriod; } set { _maxPeriod = value; } }
  
  [SerializeField]
  float _trapNoSpawnRange;
  public float trapNoSpawnRange { get { return _trapNoSpawnRange; } set { _trapNoSpawnRange = value; } }
  
}