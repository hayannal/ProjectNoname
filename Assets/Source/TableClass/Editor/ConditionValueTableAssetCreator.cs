using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ConditionValueTable", false, 500)]
    public static void CreateConditionValueTableAssetFile()
    {
        ConditionValueTable asset = CustomAssetUtility.CreateAsset<ConditionValueTable>();
        asset.SheetName = "../Excel/AffectorValue.xlsx";
        asset.WorksheetName = "ConditionValueTable";
        EditorUtility.SetDirty(asset);        
    }
    
}