using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class SkillTableData
{
  [SerializeField]
  string _id;
  public string id { get { return _id; } set { _id = value; } }
  
  [SerializeField]
  string _actorId;
  public string actorId { get { return _actorId; } set { _actorId = value; } }
  
  [SerializeField]
  int _skillType;
  public int skillType { get { return _skillType; } set { _skillType = value; } }
  
  [SerializeField]
  string _icon;
  public string icon { get { return _icon; } set { _icon = value; } }
  
  [SerializeField]
  float _cooltime;
  public float cooltime { get { return _cooltime; } set { _cooltime = value; } }
  
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
  bool _useCooltimeOverriding;
  public bool useCooltimeOverriding { get { return _useCooltimeOverriding; } set { _useCooltimeOverriding = value; } }
  
  [SerializeField]
  bool _useMecanimNameOverriding;
  public bool useMecanimNameOverriding { get { return _useMecanimNameOverriding; } set { _useMecanimNameOverriding = value; } }
  
  [SerializeField]
  bool _useTableAffectorValueIdOverriding;
  public bool useTableAffectorValueIdOverriding { get { return _useTableAffectorValueIdOverriding; } set { _useTableAffectorValueIdOverriding = value; } }
  
  [SerializeField]
  bool _useNameIdOverriding;
  public bool useNameIdOverriding { get { return _useNameIdOverriding; } set { _useNameIdOverriding = value; } }
  
  [SerializeField]
  bool _useDescriptionIdOverriding;
  public bool useDescriptionIdOverriding { get { return _useDescriptionIdOverriding; } set { _useDescriptionIdOverriding = value; } }
  
}