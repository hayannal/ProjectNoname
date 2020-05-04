using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ShopDiamondTable", false, 500)]
    public static void CreateShopDiamondTableAssetFile()
    {
        ShopDiamondTable asset = CustomAssetUtility.CreateAsset<ShopDiamondTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "ShopDiamondTable";
        EditorUtility.SetDirty(asset);        
    }
    
}