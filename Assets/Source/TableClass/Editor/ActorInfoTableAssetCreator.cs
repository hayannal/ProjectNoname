using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ActorInfoTable", false, 500)]
    public static void CreateActorInfoTableAssetFile()
    {
        ActorInfoTable asset = CustomAssetUtility.CreateAsset<ActorInfoTable>();
        asset.SheetName = "../Excel/Actor.xlsx";
        asset.WorksheetName = "ActorInfoTable";
        EditorUtility.SetDirty(asset);        
    }
    
}