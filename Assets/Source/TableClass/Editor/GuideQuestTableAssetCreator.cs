using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/GuideQuestTable", false, 500)]
    public static void CreateGuideQuestTableAssetFile()
    {
        GuideQuestTable asset = CustomAssetUtility.CreateAsset<GuideQuestTable>();
        asset.SheetName = "../Excel/Quest.xlsx";
        asset.WorksheetName = "GuideQuestTable";
        EditorUtility.SetDirty(asset);        
    }
    
}