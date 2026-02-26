using UnityEditor;
using UnityEngine;

public class QuickBuild
{
    [MenuItem("Build/Windows Build")]
    public static void BuildWindows()
    {
        // 빌드 설정에 등록된 씬 목록 가져오기
        string[] scenes = new string[EditorBuildSettings.scenes.Length];
        for (int i = 0; i < scenes.Length; i++)
        {
            scenes[i] = EditorBuildSettings.scenes[i].path;
        }

        // 프로젝트 이름을 자동으로 가져와서 빌드 경로 생성
        string projectName = PlayerSettings.productName;
        string buildPath = $"Builds/{projectName}.exe";

        BuildPipeline.BuildPlayer(scenes, buildPath, BuildTarget.StandaloneWindows64, BuildOptions.None);

        Debug.Log($"빌드 완료! 경로: {buildPath}");
    }
}
