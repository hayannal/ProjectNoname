using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/WingLookTable", false, 500)]
    public static void CreateWingLookTableAssetFile()
    {
        WingLookTable asset = CustomAssetUtility.CreateAsset<WingLookTable>();
        asset.SheetName = "../Excel/Actor.xlsx";
        asset.WorksheetName = "WingLookTable";
        EditorUtility.SetDirty(asset);        
    }
    
}