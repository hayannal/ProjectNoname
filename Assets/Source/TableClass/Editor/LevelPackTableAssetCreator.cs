using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/LevelPackTable", false, 500)]
    public static void CreateLevelPackTableAssetFile()
    {
        LevelPackTable asset = CustomAssetUtility.CreateAsset<LevelPackTable>();
        asset.SheetName = "../Excel/LevelPack.xlsx";
        asset.WorksheetName = "LevelPackTable";
        EditorUtility.SetDirty(asset);        
    }
    
}