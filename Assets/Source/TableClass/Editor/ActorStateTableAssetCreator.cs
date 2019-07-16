using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ActorStateTable", false, 500)]
    public static void CreateActorStateTableAssetFile()
    {
        ActorStateTable asset = CustomAssetUtility.CreateAsset<ActorStateTable>();
        asset.SheetName = "../Excel/AffectorValue.xlsx";
        asset.WorksheetName = "ActorStateTable";
        EditorUtility.SetDirty(asset);        
    }
    
}