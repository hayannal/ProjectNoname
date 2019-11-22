using UnityEngine;
using System.Collections;

///
/// !!! Machine generated code !!!
/// !!! DO NOT CHANGE Tabs to Spaces !!!
/// 
[System.Serializable]
public class ChapterTableData
{
  [SerializeField]
  int _chapter;
  public int chapter { get { return _chapter; } set { _chapter = value; } }
  
  [SerializeField]
  int _maxStage;
  public int maxStage { get { return _maxStage; } set { _maxStage = value; } }
  
  [SerializeField]
  int _suggestedPowerLevel;
  public int suggestedPowerLevel { get { return _suggestedPowerLevel; } set { _suggestedPowerLevel = value; } }
  
  [SerializeField]
  string _descriptionId;
  public string descriptionId { get { return _descriptionId; } set { _descriptionId = value; } }
  
  [SerializeField]
  string[] _suggestedActorId = new string[0];
  public string[] suggestedActorId { get { return _suggestedActorId; } set { _suggestedActorId = value; } }
  
  [SerializeField]
  int _chapterGoldReward;
  public int chapterGoldReward { get { return _chapterGoldReward; } set { _chapterGoldReward = value; } }
  
}