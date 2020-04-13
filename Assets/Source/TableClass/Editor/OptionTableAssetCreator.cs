using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/OptionTable", false, 500)]
    public static void CreateOptionTableAssetFile()
    {
        OptionTable asset = CustomAssetUtility.CreateAsset<OptionTable>();
        asset.SheetName = "../Excel/Equip.xlsx";
        asset.WorksheetName = "OptionTable";
        EditorUtility.SetDirty(asset);        
    }
    
}