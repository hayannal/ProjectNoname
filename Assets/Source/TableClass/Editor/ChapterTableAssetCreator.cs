using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ChapterTable", false, 500)]
    public static void CreateChapterTableAssetFile()
    {
        ChapterTable asset = CustomAssetUtility.CreateAsset<ChapterTable>();
        asset.SheetName = "../Excel/Stage.xlsx";
        asset.WorksheetName = "ChapterTable";
        EditorUtility.SetDirty(asset);        
    }
    
}