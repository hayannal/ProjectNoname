using UnityEngine;
using UnityEditor;
using System.IO;
using UnityQuickSheet;

///
/// !!! Machine generated code !!!
/// 
public partial class ExcelDataAssetUtility
{
    [MenuItem("Assets/Create/QuickSheet/ExcelData/ShopBoxTable", false, 500)]
    public static void CreateShopBoxTableAssetFile()
    {
        ShopBoxTable asset = CustomAssetUtility.CreateAsset<ShopBoxTable>();
        asset.SheetName = "../Excel/Shop.xlsx";
        asset.WorksheetName = "ShopBoxTable";
        EditorUtility.SetDirty(asset);        
    }
    
}