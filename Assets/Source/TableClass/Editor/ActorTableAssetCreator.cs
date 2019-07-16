using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ActorTable", false, 500)]
    public static void CreateActorTableAssetFile()
    {
        ActorTable asset = CustomAssetUtility.CreateAsset<ActorTable>();
        asset.SheetName = "../Excel/Actor.xlsx";
        asset.WorksheetName = "ActorTable";
        EditorUtility.SetDirty(asset);        
    }
    
}