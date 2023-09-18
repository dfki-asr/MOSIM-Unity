using UnityEngine;
using UnityEditor;

public class BuildIKService
{

    public static void CreateServerBuild()
    {
        Debug.Log("Building Path Planning Service Server Build");
        string[] scenes = new string[] { "Assets/Scenes/main.unity" };
        BuildPlayerOptions ops = new BuildPlayerOptions();
        ops.scenes = scenes;
        ops.locationPathName = "./build/UnityIKService.exe";
        ops.target = BuildTarget.StandaloneWindows;
        ops.subtarget = (int)StandaloneBuildSubtarget.Server;
        BuildPipeline.BuildPlayer(ops);
    }

    public static void CreateServerBuildLinux()
    {
        Debug.Log("Building Path Planning Service Server Build");
        string[] scenes = new string[] { "Assets/Scenes/main.unity" };
        BuildPlayerOptions ops = new BuildPlayerOptions();
        ops.scenes = scenes;
        ops.locationPathName = "./build/UnityIKService";
        ops.target = BuildTarget.StandaloneLinux64;
        ops.subtarget = (int)StandaloneBuildSubtarget.Server;
        BuildPipeline.BuildPlayer(ops);
    }

}