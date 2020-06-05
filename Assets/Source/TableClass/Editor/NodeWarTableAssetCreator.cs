using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/NodeWarTable", false, 500)]
    public static void CreateNodeWarTableAssetFile()
    {
        NodeWarTable asset = CustomAssetUtility.CreateAsset<NodeWarTable>();
        asset.SheetName = "../Excel/NodeWar.xlsx";
        asset.WorksheetName = "NodeWarTable";
        EditorUtility.SetDirty(asset);        
    }
    
}