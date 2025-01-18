using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Hoge
{
    public class PostXcodeBuild
    {
        [PostProcessBuild]
        public static void SetXcodePlist(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (buildTarget != BuildTarget.iOS) return;

            var plistPath = pathToBuiltProject + "/Info.plist";
            var plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));

            var rootDict = plist.root;
            // ここに記載したKey-ValueがXcodeのinfo.plistに反映されます
            rootDict.SetBoolean("Application supports iTunes file sharing", true);
            rootDict.SetBoolean("Supports opening documents in place", true);

            //File.WriteAllText(plistPath, plist.WriteToString());
            plist.WriteToFile(plistPath);
        }
    }
}
