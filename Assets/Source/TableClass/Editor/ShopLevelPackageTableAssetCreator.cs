using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ShopLevelPackageTable", false, 500)]
    public static void CreateShopLevelPackageTableAssetFile()
    {
        ShopLevelPackageTable asset = CustomAssetUtility.CreateAsset<ShopLevelPackageTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "ShopLevelPackageTable";
        EditorUtility.SetDirty(asset);        
    }
    
}