using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/MonsterTable", false, 500)]
    public static void CreateMonsterTableAssetFile()
    {
        MonsterTable asset = CustomAssetUtility.CreateAsset<MonsterTable>();
        asset.SheetName = "../Excel/Stage.xlsx";
        asset.WorksheetName = "MonsterTable";
        EditorUtility.SetDirty(asset);        
    }
    
}