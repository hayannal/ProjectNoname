using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/WingPowerTable", false, 500)]
    public static void CreateWingPowerTableAssetFile()
    {
        WingPowerTable asset = CustomAssetUtility.CreateAsset<WingPowerTable>();
        asset.SheetName = "../Excel/Actor.xlsx";
        asset.WorksheetName = "WingPowerTable";
        EditorUtility.SetDirty(asset);        
    }
    
}