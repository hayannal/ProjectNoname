using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/EquipTable", false, 500)]
    public static void CreateEquipTableAssetFile()
    {
        EquipTable asset = CustomAssetUtility.CreateAsset<EquipTable>();
        asset.SheetName = "../Excel/Equip.xlsx";
        asset.WorksheetName = "EquipTable";
        EditorUtility.SetDirty(asset);        
    }
    
}