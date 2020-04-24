using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/FixedCharTable", false, 500)]
    public static void CreateFixedCharTableAssetFile()
    {
        FixedCharTable asset = CustomAssetUtility.CreateAsset<FixedCharTable>();
        asset.SheetName = "../Excel/Actor.xlsx";
        asset.WorksheetName = "FixedCharTable";
        EditorUtility.SetDirty(asset);        
    }
    
}