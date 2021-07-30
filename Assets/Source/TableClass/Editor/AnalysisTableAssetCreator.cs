using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/AnalysisTable", false, 500)]
    public static void CreateAnalysisTableAssetFile()
    {
        AnalysisTable asset = CustomAssetUtility.CreateAsset<AnalysisTable>();
        asset.SheetName = "../Excel/Research.xlsx";
        asset.WorksheetName = "AnalysisTable";
        EditorUtility.SetDirty(asset);        
    }
    
}