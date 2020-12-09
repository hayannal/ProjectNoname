using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ChapterTrapTable", false, 500)]
    public static void CreateChapterTrapTableAssetFile()
    {
        ChapterTrapTable asset = CustomAssetUtility.CreateAsset<ChapterTrapTable>();
        asset.SheetName = "../Excel/Stage.xlsx";
        asset.WorksheetName = "ChapterTrapTable";
        EditorUtility.SetDirty(asset);        
    }
    
}