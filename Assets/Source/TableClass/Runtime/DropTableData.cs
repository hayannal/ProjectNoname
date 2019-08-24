using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class DropTableData
{
  [SerializeField]
  string _dropId;
  public string dropId { get { return _dropId; } set { _dropId = value; } }
  
  [SerializeField]
  int[] _dropEnum = new int[0];
  public int[] dropEnum { get { return _dropEnum; } set { _dropEnum = value; } }
  
  [SerializeField]
  string[] _subValue = new string[0];
  public string[] subValue { get { return _subValue; } set { _subValue = value; } }
  
  [SerializeField]
  float[] _probability = new float[0];
  public float[] probability { get { return _probability; } set { _probability = value; } }
  
  [SerializeField]
  float[] _minValue = new float[0];
  public float[] minValue { get { return _minValue; } set { _minValue = value; } }
  
  [SerializeField]
  float[] _maxValue = new float[0];
  public float[] maxValue { get { return _maxValue; } set { _maxValue = value; } }
  
}