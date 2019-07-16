using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/AffectorValueLevelTable", false, 500)]
    public static void CreateAffectorValueLevelTableAssetFile()
    {
        AffectorValueLevelTable asset = CustomAssetUtility.CreateAsset<AffectorValueLevelTable>();
        asset.SheetName = "../Excel/AffectorValue.xlsx";
        asset.WorksheetName = "AffectorValueLevelTable";
        EditorUtility.SetDirty(asset);        
    }
    
}