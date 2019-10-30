using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/StageExpTable", false, 500)]
    public static void CreateStageExpTableAssetFile()
    {
        StageExpTable asset = CustomAssetUtility.CreateAsset<StageExpTable>();
        asset.SheetName = "../Excel/LevelPack.xlsx";
        asset.WorksheetName = "StageExpTable";
        EditorUtility.SetDirty(asset);        
    }
    
}