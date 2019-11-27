using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class SkillLevelTableData
{
  [SerializeField]
  string _skillId;
  public string skillId { get { return _skillId; } set { _skillId = value; } }
  
  [SerializeField]
  int _level;
  public int level { get { return _level; } set { _level = value; } }
  
  [SerializeField]
  float _cooltime;
  public float cooltime { get { return _cooltime; } set { _cooltime = value; } }
  
  [SerializeField]
  string _mecanimName;
  public string mecanimName { get { return _mecanimName; } set { _mecanimName = value; } }
  
  [SerializeField]
  string[] _tableAffectorValueId = new string[0];
  public string[] tableAffectorValueId { get { return _tableAffectorValueId; } set { _tableAffectorValueId = value; } }
  
  [SerializeField]
  string _nameId;
  public string nameId { get { return _nameId; } set { _nameId = value; } }
  
  [SerializeField]
  string _descriptionId;
  public string descriptionId { get { return _descriptionId; } set { _descriptionId = value; } }
  
  [SerializeField]
  string[] _parameter = new string[0];
  public string[] parameter { get { return _parameter; } set { _parameter = value; } }
  
}