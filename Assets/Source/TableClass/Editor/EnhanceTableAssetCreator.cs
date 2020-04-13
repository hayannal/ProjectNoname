using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/EnhanceTable", false, 500)]
    public static void CreateEnhanceTableAssetFile()
    {
        EnhanceTable asset = CustomAssetUtility.CreateAsset<EnhanceTable>();
        asset.SheetName = "../Excel/Equip.xlsx";
        asset.WorksheetName = "EnhanceTable";
        EditorUtility.SetDirty(asset);        
    }
    
}