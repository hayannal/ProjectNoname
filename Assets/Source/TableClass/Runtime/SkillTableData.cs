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
  bool _passiveSkill;
  public bool passiveSkill { get { return _passiveSkill; } set { _passiveSkill = value; } }
  
  [SerializeField]
  string _icon;
  public string icon { get { return _icon; } set { _icon = value; } }
  
  [SerializeField]
  float _cooltime;
  public float cooltime { get { return _cooltime; } set { _cooltime = value; } }
  
  [SerializeField]
  string[] _passiveAffectorValueId = new string[0];
  public string[] passiveAffectorValueId { get { return _passiveAffectorValueId; } set { _passiveAffectorValueId = value; } }
  
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
  bool _usePassiveAffectorValueIdOverriding;
  public bool usePassiveAffectorValueIdOverriding { get { return _usePassiveAffectorValueIdOverriding; } set { _usePassiveAffectorValueIdOverriding = value; } }
  
  [SerializeField]
  bool _useNameIdOverriding;
  public bool useNameIdOverriding { get { return _useNameIdOverriding; } set { _useNameIdOverriding = value; } }
  
  [SerializeField]
  bool _useDescriptionIdOverriding;
  public bool useDescriptionIdOverriding { get { return _useDescriptionIdOverriding; } set { _useDescriptionIdOverriding = value; } }
  
}