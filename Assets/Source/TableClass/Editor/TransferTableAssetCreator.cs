using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/TransferTable", false, 500)]
    public static void CreateTransferTableAssetFile()
    {
        TransferTable asset = CustomAssetUtility.CreateAsset<TransferTable>();
        asset.SheetName = "../Excel/Equip.xlsx";
        asset.WorksheetName = "TransferTable";
        EditorUtility.SetDirty(asset);        
    }
    
}