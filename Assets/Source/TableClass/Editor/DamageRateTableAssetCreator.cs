using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/DamageRateTable", false, 500)]
    public static void CreateDamageRateTableAssetFile()
    {
        DamageRateTable asset = CustomAssetUtility.CreateAsset<DamageRateTable>();
        asset.SheetName = "../Excel/GlobalConstant.xlsx";
        asset.WorksheetName = "DamageRateTable";
        EditorUtility.SetDirty(asset);        
    }
    
}