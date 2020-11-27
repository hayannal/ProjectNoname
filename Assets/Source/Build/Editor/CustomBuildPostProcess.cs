using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

public static class CustomBuildPostProcess
{
	[PostProcessBuild(999)]
	public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuildProject)
	{
		if (buildTarget != BuildTarget.iOS)
			return;

#if UNITY_IOS
		Debug.Log("[PostBuild] pathToBuildProject: " + pathToBuildProject);

		string pbxProjectPath = PBXProject.GetPBXProjectPath(pathToBuildProject);
		PBXProject pbxProject = new PBXProject();
		pbxProject.ReadFromFile(pbxProjectPath);

		//string targetGUID = pbxProject.TargetGuidByName(PBXProject.GetUnityMainTargetGuid());
		//string targetGUID = pbxProject.TargetGuidByName("Unity-iPhone");
		string targetGUID = pbxProject.GetUnityMainTargetGuid();
		string bundleId = pbxProject.GetBuildPropertyForAnyConfig(targetGUID, "PRODUCT_BUNDLE_IDENTIFIER");
		string productName = pbxProject.GetBuildPropertyForAnyConfig(targetGUID, "PRODUCT_NAME");
		Debug.LogFormat("BundleId : {0} / ProductName : {1}", bundleId, productName);

		string newProductName = "Nameless Origin";
		if (productName != newProductName)
		{
			pbxProject.SetBuildProperty(targetGUID, "PRODUCT_NAME", newProductName);
		}
		string newBundleId = "com.powersourcestudio.namelessorigin";
		if (bundleId != newBundleId)
		{
			pbxProject.SetBuildProperty(targetGUID, "PRODUCT_BUNDLE_IDENTIFIER", newBundleId);
		}
		pbxProject.WriteToFile(pbxProjectPath);


		// plist는 안바꾸고 그냥 냅둬보기로 한다.
		/*
		string infoPlistPath = pathToBuildProject + "/Info.plist";
		PlistDocument plistDoc = new PlistDocument();
		plistDoc.ReadFromFile(infoPlistPath);
		if (plistDoc.root != null)
		{
			PlistElementDict rootDict = plistDoc.root;
			rootDict.SetString("CFBundleIdentifier", bundleId);
			plistDoc.WriteToFile(infoPlistPath);
		}
		*/

		/*
		// Bitcode
		{
			string projectPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";

			PBXProject pbxProject = new PBXProject();
			pbxProject.ReadFromFile(projectPath);

			string target = pbxProject.TargetGuidByName("Unity-iPhone");
			pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");

			pbxProject.WriteToFile(projectPath);
		}
		*/

		// NonExemptEncryption
		{
			string infoPlistPath = pathToBuildProject + "/Info.plist";

			PlistDocument plistDoc = new PlistDocument();
			plistDoc.ReadFromFile(infoPlistPath);
			if (plistDoc.root != null)
			{
				plistDoc.root.SetBoolean("ITSAppUsesNonExemptEncryption", false);
				//plistDoc.root.SetString("CFBundleDisplayName", "MY APP NAME");
				plistDoc.WriteToFile(infoPlistPath);
			}
			else
			{
				Debug.LogError("ERROR: Can't open " + infoPlistPath);
			}
		}
#endif
	}
}