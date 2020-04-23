using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/NotStreakTable", false, 500)]
    public static void CreateNotStreakTableAssetFile()
    {
        NotStreakTable asset = CustomAssetUtility.CreateAsset<NotStreakTable>();
        asset.SheetName = "../Excel/Equip.xlsx";
        asset.WorksheetName = "NotStreakTable";
        EditorUtility.SetDirty(asset);        
    }
    
}