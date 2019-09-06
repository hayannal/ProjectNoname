using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/InApkStringTable", false, 500)]
    public static void CreateInApkStringTableAssetFile()
    {
        InApkStringTable asset = CustomAssetUtility.CreateAsset<InApkStringTable>();
        asset.SheetName = "../Excel/String.xlsx";
        asset.WorksheetName = "InApkStringTable";
        EditorUtility.SetDirty(asset);        
    }
    
}