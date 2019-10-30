using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/GlobalConstantIntTable", false, 500)]
    public static void CreateGlobalConstantIntTableAssetFile()
    {
        GlobalConstantIntTable asset = CustomAssetUtility.CreateAsset<GlobalConstantIntTable>();
        asset.SheetName = "../Excel/GlobalConstant.xlsx";
        asset.WorksheetName = "GlobalConstantIntTable";
        EditorUtility.SetDirty(asset);        
    }
    
}