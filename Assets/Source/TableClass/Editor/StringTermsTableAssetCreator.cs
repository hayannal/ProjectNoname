using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/StringTermsTable", false, 500)]
    public static void CreateStringTermsTableAssetFile()
    {
        StringTermsTable asset = CustomAssetUtility.CreateAsset<StringTermsTable>();
        asset.SheetName = "../Excel/StringTerms.xlsx";
        asset.WorksheetName = "StringTermsTable";
        EditorUtility.SetDirty(asset);        
    }
    
}