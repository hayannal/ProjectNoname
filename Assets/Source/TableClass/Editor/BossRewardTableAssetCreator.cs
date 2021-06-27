using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/BossRewardTable", false, 500)]
    public static void CreateBossRewardTableAssetFile()
    {
        BossRewardTable asset = CustomAssetUtility.CreateAsset<BossRewardTable>();
        asset.SheetName = "../Excel/BossBattle.xlsx";
        asset.WorksheetName = "BossRewardTable";
        EditorUtility.SetDirty(asset);        
    }
    
}