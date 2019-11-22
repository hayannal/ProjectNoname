using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/StagePenaltyTable", false, 500)]
    public static void CreateStagePenaltyTableAssetFile()
    {
        StagePenaltyTable asset = CustomAssetUtility.CreateAsset<StagePenaltyTable>();
        asset.SheetName = "../Excel/Stage.xlsx";
        asset.WorksheetName = "StagePenaltyTable";
        EditorUtility.SetDirty(asset);        
    }
    
}