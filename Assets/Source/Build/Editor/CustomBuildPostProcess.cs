using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class CustomBuildPostProcess
{
	[PostProcessBuild(999)]
	public static void OnPostProcessBuild(BuildTarget buildTarget, string path)
	{
		/*
		if (buildTarget == BuildTarget.iOS)
		{
			// Bitcode
			{
				string projectPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";

				PBXProject pbxProject = new PBXProject();
				pbxProject.ReadFromFile(projectPath);

				string target = pbxProject.TargetGuidByName("Unity-iPhone");
				pbxProject.SetBuildProperty(target, "ENABLE_BITCODE", "NO");

				pbxProject.WriteToFile(projectPath);
			}

			// NonExemptEncryption
			{
				string infoPlistPath = path + "/Info.plist";

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
		}
		*/
	}
}