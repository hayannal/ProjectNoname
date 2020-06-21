using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/NodeWarTrapTable", false, 500)]
    public static void CreateNodeWarTrapTableAssetFile()
    {
        NodeWarTrapTable asset = CustomAssetUtility.CreateAsset<NodeWarTrapTable>();
        asset.SheetName = "../Excel/NodeWar.xlsx";
        asset.WorksheetName = "NodeWarTrapTable";
        EditorUtility.SetDirty(asset);        
    }
    
}