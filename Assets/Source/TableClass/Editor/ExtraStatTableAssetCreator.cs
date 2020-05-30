using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ExtraStatTable", false, 500)]
    public static void CreateExtraStatTableAssetFile()
    {
        ExtraStatTable asset = CustomAssetUtility.CreateAsset<ExtraStatTable>();
        asset.SheetName = "../Excel/Actor.xlsx";
        asset.WorksheetName = "ExtraStatTable";
        EditorUtility.SetDirty(asset);        
    }
    
}