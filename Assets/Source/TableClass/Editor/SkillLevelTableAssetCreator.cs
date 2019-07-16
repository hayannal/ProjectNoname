using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/SkillLevelTable", false, 500)]
    public static void CreateSkillLevelTableAssetFile()
    {
        SkillLevelTable asset = CustomAssetUtility.CreateAsset<SkillLevelTable>();
        asset.SheetName = "../Excel/Action.xlsx";
        asset.WorksheetName = "SkillLevelTable";
        EditorUtility.SetDirty(asset);        
    }
    
}