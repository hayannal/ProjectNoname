using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/FontTable", false, 500)]
    public static void CreateFontTableAssetFile()
    {
        FontTable asset = CustomAssetUtility.CreateAsset<FontTable>();
        asset.SheetName = "../Excel/String.xlsx";
        asset.WorksheetName = "FontTable";
        EditorUtility.SetDirty(asset);        
    }
    
}