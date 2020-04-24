/** watch out: UnityEditor.iOS.Xcode is missing if iOS builds are not installed
 * 
 * There is also no reliable compiler flag to check for this that works
 * on 2017 LTS and new versions. We have two options:
 * 
 * a) Check if UNITY_IOS is set
 *      * Means it must be available thus there is no risk of errors
 *        if ios unity is not installed
 *      * builds will fail to run the post processing step and generate
 *        invalid iOS builds IF the user builds the iOS version without
 *        pressing "Switch platform" first. (in this case IOS_UNITY won't
 *        be defined)
 *      * also fails if 
 * b) Don't check UNITY_IOS (happens if HAS_IOS_INSTALLED is defined)
 *      * Builds work fine with iOS support installed
 *      * everyone without iOS gets a compiler error on import!
 * 
 * We picked option a) and throw an error if the user is about to
 * generate an invalid xcode project. Uncomment HAS_IOS_INSTALLED
 * if you want to be able to build without having to press
 * "switch platform" first. This will cause a compiler error if the project
 * is opened without installed iOS support.
 * 
 */
//#define HAS_IOS_INSTALLED


using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System;

#if UNITY_IOS || HAS_IOS_INSTALLED
using UnityEditor.iOS.Xcode;
using UnityEditor.iOS.Xcode.Extensions;
#endif

namespace Byn.Unity
{
    public static class IosPostBuild
    {
        [PostProcessBuild]
        public static void OnPreprocessBuild(BuildTarget buildTarget, string path)
        {
            if (buildTarget == BuildTarget.iOS)
            {
#if UNITY_IOS || HAS_IOS_INSTALLED
                //all good
#else
                Debug.LogError("WARNING: UNITY_IOS IS NOT DEFINED DURING IOS BUILD! THIS WILL BLOCK IOS SPECIFIC BUILD SCRIPTS FROM RUNNING CORRECTLY!");
                throw new InvalidOperationException("Switch to iOS before building or uncomment #define HAS_IOS_INSTALLED above.");

#endif
            }
        }


        [PostProcessBuild]
        public static void OnPostprocessBuild(BuildTarget buildTarget, string path)
        {
            if (buildTarget == BuildTarget.iOS)
            {

#if UNITY_IOS || HAS_IOS_INSTALLED
                Debug.Log("Running OnPostprocessBuild for WebRTC Network / Video Chat asset!");
                IosXcodeFix(path);
#else
                //if we get here this means iOS is available at runtime but we
                //already excluded it during compile time ...
                Debug.LogError("Switch to iOS before building or uncomment #define HAS_IOS_INSTALLED above.");
                throw new InvalidOperationException("Switch to iOS before building or uncomment #define HAS_IOS_INSTALLED above.");
#endif


            }
        }
#if UNITY_IOS || HAS_IOS_INSTALLED
        public static void IosXcodeFix(string path)
        {
            PBXProject project = new PBXProject();
            string projPath = path + "/Unity-iPhone.xcodeproj/project.pbxproj";
            project.ReadFromString(File.ReadAllText(projPath));


#if UNITY_2019_3_OR_NEWER
            string target = project.GetUnityMainTargetGuid();
            string targetFramework = project.GetUnityFrameworkTargetGuid();            
            project.SetBuildProperty(targetFramework, "ENABLE_BITCODE", "NO");
#else
            string target = project.TargetGuidByName("Unity-iPhone");
#endif

            Debug.Log("Setting linker flag ENABLE_BITCODE to NO");
            project.SetBuildProperty(target, "ENABLE_BITCODE", "NO");
            
            //get the framework file id (check for possible different locations)
            string fileId = null;

            //universal (new)
            if (fileId == null)
            {
                fileId = project.FindFileGuidByProjectPath("Frameworks/WebRtcVideoChat/Plugins/ios/universal/webrtccsharpwrap.framework");
            }

            //armv7 only
            if (fileId == null)
            {
                fileId = project.FindFileGuidByProjectPath("Frameworks/WebRtcVideoChat/Plugins/ios/armv7/webrtccsharpwrap.framework");
            }
            //arm64 only
            if (fileId == null)
            {
                fileId = project.FindFileGuidByProjectPath("Frameworks/WebRtcVideoChat/Plugins/ios/arm64/webrtccsharpwrap.framework");
            }
            //manual placement
            if (fileId == null)
            {
                fileId = project.FindFileGuidByProjectPath("Frameworks/webrtccsharpwrap.framework");
            }

            Debug.Log("Adding build phase CopyFrameworks to copy the framework to the app Frameworks directory");

#if UNITY_2017_2_OR_NEWER

            project.AddFileToEmbedFrameworks(target, fileId);
#else
			string copyFilePhase = project.AddCopyFilesBuildPhase(target,"CopyFrameworks", "", "10");
			project.AddFileToBuildSection (target, copyFilePhase, fileId);
			//Couldn't figure out how to set that flag yet.
			Debug.LogWarning("Code Sign On Copy flag must be set manually via Xcode for webrtccsharpwrap.framework:" +
			"Project settings -> Build phases -> Copy Frameworks -> set the flag Code Sign On Copy");
#endif


            //make sure the Framework is expected in the Frameworks path. Without that ios won't find the framework
            project.AddBuildProperty(target, "LD_RUNPATH_SEARCH_PATHS", "@executable_path/Frameworks");

            File.WriteAllText(projPath, project.WriteToString());
        }
#endif
    }
}
