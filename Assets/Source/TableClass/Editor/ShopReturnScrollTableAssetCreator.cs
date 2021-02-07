using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ShopReturnScrollTable", false, 500)]
    public static void CreateShopReturnScrollTableAssetFile()
    {
        ShopReturnScrollTable asset = CustomAssetUtility.CreateAsset<ShopReturnScrollTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "ShopReturnScrollTable";
        EditorUtility.SetDirty(asset);        
    }
    
}