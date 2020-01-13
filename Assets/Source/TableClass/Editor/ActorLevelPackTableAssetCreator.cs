using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ActorLevelPackTable", false, 500)]
    public static void CreateActorLevelPackTableAssetFile()
    {
        ActorLevelPackTable asset = CustomAssetUtility.CreateAsset<ActorLevelPackTable>();
        asset.SheetName = "../Excel/LevelPack.xlsx";
        asset.WorksheetName = "ActorLevelPackTable";
        EditorUtility.SetDirty(asset);        
    }
    
}