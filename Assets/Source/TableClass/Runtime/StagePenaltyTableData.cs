using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class StagePenaltyTableData
{
  [SerializeField]
  string _stagePenaltyId;
  public string stagePenaltyId { get { return _stagePenaltyId; } set { _stagePenaltyId = value; } }
  
  [SerializeField]
  string[] _affectorValueId = new string[0];
  public string[] affectorValueId { get { return _affectorValueId; } set { _affectorValueId = value; } }
  
  [SerializeField]
  string _penaltyName;
  public string penaltyName { get { return _penaltyName; } set { _penaltyName = value; } }
  
  [SerializeField]
  string[] _nameParameter = new string[0];
  public string[] nameParameter { get { return _nameParameter; } set { _nameParameter = value; } }
  
  [SerializeField]
  string _penaltyMindText;
  public string penaltyMindText { get { return _penaltyMindText; } set { _penaltyMindText = value; } }
  
  [SerializeField]
  string[] _mindParameter = new string[0];
  public string[] mindParameter { get { return _mindParameter; } set { _mindParameter = value; } }
  
}