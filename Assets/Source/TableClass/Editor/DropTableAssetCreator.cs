using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/DropTable", false, 500)]
    public static void CreateDropTableAssetFile()
    {
        DropTable asset = CustomAssetUtility.CreateAsset<DropTable>();
        asset.SheetName = "../Excel/Drop.xlsx";
        asset.WorksheetName = "DropTable";
        EditorUtility.SetDirty(asset);        
    }
    
}