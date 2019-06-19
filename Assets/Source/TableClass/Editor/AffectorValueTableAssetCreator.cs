using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/AffectorValueTable", false, 500)]
    public static void CreateAffectorValueTableAssetFile()
    {
        AffectorValueTable asset = CustomAssetUtility.CreateAsset<AffectorValueTable>();
        asset.SheetName = "../Excel/AffectorValue.xlsx";
        asset.WorksheetName = "AffectorValueTable";
        EditorUtility.SetDirty(asset);        
    }
    
}