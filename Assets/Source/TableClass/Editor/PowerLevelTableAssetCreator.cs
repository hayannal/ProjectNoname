using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/PowerLevelTable", false, 500)]
    public static void CreatePowerLevelTableAssetFile()
    {
        PowerLevelTable asset = CustomAssetUtility.CreateAsset<PowerLevelTable>();
        asset.SheetName = "../Excel/Actor.xlsx";
        asset.WorksheetName = "PowerLevelTable";
        EditorUtility.SetDirty(asset);        
    }
    
}