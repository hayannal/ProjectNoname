using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/NotCharTable", false, 500)]
    public static void CreateNotCharTableAssetFile()
    {
        NotCharTable asset = CustomAssetUtility.CreateAsset<NotCharTable>();
        asset.SheetName = "../Excel/Drop.xlsx";
        asset.WorksheetName = "NotCharTable";
        EditorUtility.SetDirty(asset);        
    }
    
}