using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ResearchTable", false, 500)]
    public static void CreateResearchTableAssetFile()
    {
        ResearchTable asset = CustomAssetUtility.CreateAsset<ResearchTable>();
        asset.SheetName = "../Excel/Research.xlsx";
        asset.WorksheetName = "ResearchTable";
        EditorUtility.SetDirty(asset);        
    }
    
}