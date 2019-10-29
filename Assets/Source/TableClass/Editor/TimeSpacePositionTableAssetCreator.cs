using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/TimeSpacePositionTable", false, 500)]
    public static void CreateTimeSpacePositionTableAssetFile()
    {
        TimeSpacePositionTable asset = CustomAssetUtility.CreateAsset<TimeSpacePositionTable>();
        asset.SheetName = "../Excel/TimeSpace.xlsx";
        asset.WorksheetName = "TimeSpacePositionTable";
        EditorUtility.SetDirty(asset);        
    }
    
}