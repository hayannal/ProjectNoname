using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/BossExpTable", false, 500)]
    public static void CreateBossExpTableAssetFile()
    {
        BossExpTable asset = CustomAssetUtility.CreateAsset<BossExpTable>();
        asset.SheetName = "../Excel/BossBattle.xlsx";
        asset.WorksheetName = "BossExpTable";
        EditorUtility.SetDirty(asset);        
    }
    
}