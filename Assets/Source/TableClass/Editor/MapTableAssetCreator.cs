using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/MapTable", false, 500)]
    public static void CreateMapTableAssetFile()
    {
        MapTable asset = CustomAssetUtility.CreateAsset<MapTable>();
        asset.SheetName = "../Excel/Stage.xlsx";
        asset.WorksheetName = "MapTable";
        EditorUtility.SetDirty(asset);        
    }
    
}