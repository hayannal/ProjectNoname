using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class ActorStateTableData
{
  [SerializeField]
  string _actorStateId;
  public string actorStateId { get { return _actorStateId; } set { _actorStateId = value; } }
  
  [SerializeField]
  string[] _continuousAffectorValueId = new string[0];
  public string[] continuousAffectorValueId { get { return _continuousAffectorValueId; } set { _continuousAffectorValueId = value; } }
  
}