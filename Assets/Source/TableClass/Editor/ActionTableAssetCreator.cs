using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ActionTable", false, 500)]
    public static void CreateActionTableAssetFile()
    {
        ActionTable asset = CustomAssetUtility.CreateAsset<ActionTable>();
        asset.SheetName = "../Excel/Action.xlsx";
        asset.WorksheetName = "ActionTable";
        EditorUtility.SetDirty(asset);        
    }
    
}