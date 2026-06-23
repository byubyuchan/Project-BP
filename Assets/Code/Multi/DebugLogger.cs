using UnityEngine;
using TMPro; // TextMeshPro가 있다면 사용하세요

public class DebugLogger : MonoBehaviour
{
    private static string myLog = "";
    private Vector2 scrollPosition;

    void OnEnable() { Application.logMessageReceived += Log; }
    void OnDisable() { Application.logMessageReceived -= Log; }

    public void Log(string logString, string stackTrace, LogType type)
    {
        myLog = myLog + "\n" + logString;
        if (myLog.Length > 500) myLog = myLog.Substring(myLog.Length - 500);
    }

    void OnGUI()
    {
        // 화면 좌측 상단에 로그창을 띄움
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Width(Screen.width), GUILayout.Height(300));
        GUILayout.Label(myLog);
        GUILayout.EndScrollView();
    }
}