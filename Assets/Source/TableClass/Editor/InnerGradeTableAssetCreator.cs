using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/InnerGradeTable", false, 500)]
    public static void CreateInnerGradeTableAssetFile()
    {
        InnerGradeTable asset = CustomAssetUtility.CreateAsset<InnerGradeTable>();
        asset.SheetName = "../Excel/Equip.xlsx";
        asset.WorksheetName = "InnerGradeTable";
        EditorUtility.SetDirty(asset);        
    }
    
}