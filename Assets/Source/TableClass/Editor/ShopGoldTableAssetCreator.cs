using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ShopGoldTable", false, 500)]
    public static void CreateShopGoldTableAssetFile()
    {
        ShopGoldTable asset = CustomAssetUtility.CreateAsset<ShopGoldTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "ShopGoldTable";
        EditorUtility.SetDirty(asset);        
    }
    
}