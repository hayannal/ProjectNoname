using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ShopDailyDiamondTable", false, 500)]
    public static void CreateShopDailyDiamondTableAssetFile()
    {
        ShopDailyDiamondTable asset = CustomAssetUtility.CreateAsset<ShopDailyDiamondTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "ShopDailyDiamondTable";
        EditorUtility.SetDirty(asset);        
    }
    
}