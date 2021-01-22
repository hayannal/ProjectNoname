using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/NotLegendCharTable", false, 500)]
    public static void CreateNotLegendCharTableAssetFile()
    {
        NotLegendCharTable asset = CustomAssetUtility.CreateAsset<NotLegendCharTable>();
        asset.SheetName = "../Excel/Drop.xlsx";
        asset.WorksheetName = "NotLegendCharTable";
        EditorUtility.SetDirty(asset);        
    }
    
}