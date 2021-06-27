using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/BossBattleTable", false, 500)]
    public static void CreateBossBattleTableAssetFile()
    {
        BossBattleTable asset = CustomAssetUtility.CreateAsset<BossBattleTable>();
        asset.SheetName = "../Excel/BossBattle.xlsx";
        asset.WorksheetName = "BossBattleTable";
        EditorUtility.SetDirty(asset);        
    }
    
}