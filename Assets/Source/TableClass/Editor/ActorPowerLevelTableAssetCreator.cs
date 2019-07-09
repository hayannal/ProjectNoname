using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ActorPowerLevelTable", false, 500)]
    public static void CreateActorPowerLevelTableAssetFile()
    {
        ActorPowerLevelTable asset = CustomAssetUtility.CreateAsset<ActorPowerLevelTable>();
        asset.SheetName = "../Excel/Actor.xlsx";
        asset.WorksheetName = "ActorPowerLevelTable";
        EditorUtility.SetDirty(asset);        
    }
    
}