using UnityEngine;
using UnityEditor;

public class BuildPathPlanning{

    public static void CreateServerBuild()
    {
        Debug.Log("Building Path Planning Service Server Build");
        string[] scenes = new string[] { "Assets/Scenes/pathPlanningService.unity" };
        BuildPlayerOptions ops = new BuildPlayerOptions();
        ops.scenes = scenes;
        ops.locationPathName = "./build/UnityPathPlanningService.exe";
        ops.target = BuildTarget.StandaloneWindows;
        ops.subtarget = (int)StandaloneBuildSubtarget.Server;
        BuildPipeline.BuildPlayer(ops);
    }

    public static void CreateServerBuildLinux()
    {
        Debug.Log("Building Path Planning Service Server Build");
        string[] scenes = new string[] { "Assets/Scenes/pathPlanningService.unity" };
        BuildPlayerOptions ops = new BuildPlayerOptions();
        ops.scenes = scenes;
        ops.locationPathName = "./build/UnityPathPlanningService";
        ops.target = BuildTarget.StandaloneLinux64;
        ops.subtarget = (int)StandaloneBuildSubtarget.Server;
        BuildPipeline.BuildPlayer(ops);
    }

}