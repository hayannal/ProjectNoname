using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/SkillTable", false, 500)]
    public static void CreateSkillTableAssetFile()
    {
        SkillTable asset = CustomAssetUtility.CreateAsset<SkillTable>();
        asset.SheetName = "../Excel/Action.xlsx";
        asset.WorksheetName = "SkillTable";
        EditorUtility.SetDirty(asset);        
    }
    
}