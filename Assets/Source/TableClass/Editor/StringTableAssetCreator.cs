using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/StringTable", false, 500)]
    public static void CreateStringTableAssetFile()
    {
        StringTable asset = CustomAssetUtility.CreateAsset<StringTable>();
        asset.SheetName = "../Excel/String.xlsx";
        asset.WorksheetName = "StringTable";
        EditorUtility.SetDirty(asset);        
    }
    
}