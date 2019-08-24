using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/GlobalConstantFloatTable", false, 500)]
    public static void CreateGlobalConstantFloatTableAssetFile()
    {
        GlobalConstantFloatTable asset = CustomAssetUtility.CreateAsset<GlobalConstantFloatTable>();
        asset.SheetName = "../Excel/GlobalConstant.xlsx";
        asset.WorksheetName = "GlobalConstantFloatTable";
        EditorUtility.SetDirty(asset);        
    }
    
}