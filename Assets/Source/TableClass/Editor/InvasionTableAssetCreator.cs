using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/InvasionTable", false, 500)]
    public static void CreateInvasionTableAssetFile()
    {
        InvasionTable asset = CustomAssetUtility.CreateAsset<InvasionTable>();
        asset.SheetName = "../Excel/Invasion.xlsx";
        asset.WorksheetName = "InvasionTable";
        EditorUtility.SetDirty(asset);        
    }
    
}