using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/StageTable", false, 500)]
    public static void CreateStageTableAssetFile()
    {
        StageTable asset = CustomAssetUtility.CreateAsset<StageTable>();
        asset.SheetName = "../Excel/Stage.xlsx";
        asset.WorksheetName = "StageTable";
        EditorUtility.SetDirty(asset);        
    }
    
}