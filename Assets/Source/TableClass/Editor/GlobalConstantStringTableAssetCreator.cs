using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/GlobalConstantStringTable", false, 500)]
    public static void CreateGlobalConstantStringTableAssetFile()
    {
        GlobalConstantStringTable asset = CustomAssetUtility.CreateAsset<GlobalConstantStringTable>();
        asset.SheetName = "../Excel/GlobalConstant.xlsx";
        asset.WorksheetName = "GlobalConstantStringTable";
        EditorUtility.SetDirty(asset);        
    }
    
}