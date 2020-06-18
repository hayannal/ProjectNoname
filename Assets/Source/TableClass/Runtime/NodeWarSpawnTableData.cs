using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class NodeWarSpawnTableData
{
  [SerializeField]
  string _monsterId;
  public string monsterId { get { return _monsterId; } set { _monsterId = value; } }
  
  [SerializeField]
  bool _trap;
  public bool trap { get { return _trap; } set { _trap = value; } }
  
  [SerializeField]
  int _fixedLevel;
  public int fixedLevel { get { return _fixedLevel; } set { _fixedLevel = value; } }
  
  [SerializeField]
  int _oneLevel;
  public int oneLevel { get { return _oneLevel; } set { _oneLevel = value; } }
  
  [SerializeField]
  int _minStep;
  public int minStep { get { return _minStep; } set { _minStep = value; } }
  
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
  float _spawnChance;
  public float spawnChance { get { return _spawnChance; } set { _spawnChance = value; } }
  
  [SerializeField]
  float _spawnPeriod;
  public float spawnPeriod { get { return _spawnPeriod; } set { _spawnPeriod = value; } }
  
  [SerializeField]
  float _lastSpawnPeriod;
  public float lastSpawnPeriod { get { return _lastSpawnPeriod; } set { _lastSpawnPeriod = value; } }
  
  [SerializeField]
  int _maxCount;
  public int maxCount { get { return _maxCount; } set { _maxCount = value; } }
  
  [SerializeField]
  int _lastMaxCount;
  public int lastMaxCount { get { return _lastMaxCount; } set { _lastMaxCount = value; } }
  
  [SerializeField]
  bool _totalMax;
  public bool totalMax { get { return _totalMax; } set { _totalMax = value; } }
  
}