using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/AnalysisKeyTable", false, 500)]
    public static void CreateAnalysisKeyTableAssetFile()
    {
        AnalysisKeyTable asset = CustomAssetUtility.CreateAsset<AnalysisKeyTable>();
        asset.SheetName = "../Excel/Research.xlsx";
        asset.WorksheetName = "AnalysisKeyTable";
        EditorUtility.SetDirty(asset);        
    }
    
}