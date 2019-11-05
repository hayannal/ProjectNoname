using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class DamageRateTableData
{
  [SerializeField]
  string _id;
  public string id { get { return _id; } set { _id = value; } }
  
  [SerializeField]
  int _number;
  public int number { get { return _number; } set { _number = value; } }
  
  [SerializeField]
  float[] _rate = new float[0];
  public float[] rate { get { return _rate; } set { _rate = value; } }
  
}