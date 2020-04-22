using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/RemainTable", false, 500)]
    public static void CreateRemainTableAssetFile()
    {
        RemainTable asset = CustomAssetUtility.CreateAsset<RemainTable>();
        asset.SheetName = "../Excel/Equip.xlsx";
        asset.WorksheetName = "RemainTable";
        EditorUtility.SetDirty(asset);        
    }
    
}