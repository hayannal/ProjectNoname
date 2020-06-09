using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/NodeWarSpawnTable", false, 500)]
    public static void CreateNodeWarSpawnTableAssetFile()
    {
        NodeWarSpawnTable asset = CustomAssetUtility.CreateAsset<NodeWarSpawnTable>();
        asset.SheetName = "../Excel/NodeWar.xlsx";
        asset.WorksheetName = "NodeWarSpawnTable";
        EditorUtility.SetDirty(asset);        
    }
    
}