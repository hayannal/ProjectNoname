using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/LevelPackLevelTable", false, 500)]
    public static void CreateLevelPackLevelTableAssetFile()
    {
        LevelPackLevelTable asset = CustomAssetUtility.CreateAsset<LevelPackLevelTable>();
        asset.SheetName = "../Excel/LevelPack.xlsx";
        asset.WorksheetName = "LevelPackLevelTable";
        EditorUtility.SetDirty(asset);        
    }
    
}