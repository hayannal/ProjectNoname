using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class MapTableData
{
  [SerializeField]
  string _mapId;
  public string mapId { get { return _mapId; } set { _mapId = value; } }
  
  [SerializeField]
  string _plane;
  public string plane { get { return _plane; } set { _plane = value; } }
  
  [SerializeField]
  string _ground;
  public string ground { get { return _ground; } set { _ground = value; } }
  
  [SerializeField]
  string _wall;
  public string wall { get { return _wall; } set { _wall = value; } }
  
  [SerializeField]
  string _spawnFlag;
  public string spawnFlag { get { return _spawnFlag; } set { _spawnFlag = value; } }
  
  [SerializeField]
  string _portalFlag;
  public string portalFlag { get { return _portalFlag; } set { _portalFlag = value; } }
  
  [SerializeField]
  string _bossPreviewAddress;
  public string bossPreviewAddress { get { return _bossPreviewAddress; } set { _bossPreviewAddress = value; } }
  
  [SerializeField]
  float _mirrorOffset;
  public float mirrorOffset { get { return _mirrorOffset; } set { _mirrorOffset = value; } }
  
  [SerializeField]
  string _nameId;
  public string nameId { get { return _nameId; } set { _nameId = value; } }
  
  [SerializeField]
  string _descriptionId;
  public string descriptionId { get { return _descriptionId; } set { _descriptionId = value; } }
  
  [SerializeField]
  string[] _suggestedActorId = new string[0];
  public string[] suggestedActorId { get { return _suggestedActorId; } set { _suggestedActorId = value; } }
  
}