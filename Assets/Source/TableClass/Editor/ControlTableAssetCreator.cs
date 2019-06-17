using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ControlTable", false, 500)]
    public static void CreateControlTableAssetFile()
    {
        ControlTable asset = CustomAssetUtility.CreateAsset<ControlTable>();
        asset.SheetName = "../Excel/Action.xlsx";
        asset.WorksheetName = "ControlTable";
        EditorUtility.SetDirty(asset);        
    }
    
}