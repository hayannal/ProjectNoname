using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/MapSetTable", false, 500)]
    public static void CreateMapSetTableAssetFile()
    {
        MapSetTable asset = CustomAssetUtility.CreateAsset<MapSetTable>();
        asset.SheetName = "../Excel/Stage.xlsx";
        asset.WorksheetName = "MapSetTable";
        EditorUtility.SetDirty(asset);        
    }
    
}