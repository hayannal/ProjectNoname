using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/LanguageTable", false, 500)]
    public static void CreateLanguageTableAssetFile()
    {
        LanguageTable asset = CustomAssetUtility.CreateAsset<LanguageTable>();
        asset.SheetName = "../Excel/String.xlsx";
        asset.WorksheetName = "LanguageTable";
        EditorUtility.SetDirty(asset);        
    }
    
}