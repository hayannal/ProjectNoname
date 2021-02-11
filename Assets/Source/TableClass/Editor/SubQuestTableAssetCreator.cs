using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/SubQuestTable", false, 500)]
    public static void CreateSubQuestTableAssetFile()
    {
        SubQuestTable asset = CustomAssetUtility.CreateAsset<SubQuestTable>();
        asset.SheetName = "../Excel/Quest.xlsx";
        asset.WorksheetName = "SubQuestTable";
        EditorUtility.SetDirty(asset);        
    }
    
}