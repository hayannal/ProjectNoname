using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class LevelPackTableData
{
  [SerializeField]
  string _levelPackId;
  public string levelPackId { get { return _levelPackId; } set { _levelPackId = value; } }
  
  [SerializeField]
  string _icon;
  public string icon { get { return _icon; } set { _icon = value; } }
  
  [SerializeField]
  string[] _affectorValueId = new string[0];
  public string[] affectorValueId { get { return _affectorValueId; } set { _affectorValueId = value; } }
  
  [SerializeField]
  string _nameId;
  public string nameId { get { return _nameId; } set { _nameId = value; } }
  
  [SerializeField]
  string _descriptionId;
  public string descriptionId { get { return _descriptionId; } set { _descriptionId = value; } }
  
  [SerializeField]
  int _defaultMax;
  public int defaultMax { get { return _defaultMax; } set { _defaultMax = value; } }
  
  [SerializeField]
  bool _useAffectorValueIdOverriding;
  public bool useAffectorValueIdOverriding { get { return _useAffectorValueIdOverriding; } set { _useAffectorValueIdOverriding = value; } }
  
}