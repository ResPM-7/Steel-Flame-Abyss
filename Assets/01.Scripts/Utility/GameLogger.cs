using System.Diagnostics;

//에디터와 개발 빌드 전용 게임 로그
public static class GameLogger
{
    //개발 환경의 일반 메시지 출력
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void Log(object message, UnityEngine.Object context = null)
    {
        UnityEngine.Debug.Log(message, context);
    }

    //개발 환경의 경고 메시지 출력
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(object message, UnityEngine.Object context = null)
    {
        UnityEngine.Debug.LogWarning(message, context);
    }

    //개발 환경의 오류 메시지 출력
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogError(object message, UnityEngine.Object context = null)
    {
        UnityEngine.Debug.LogError(message, context);
    }

    //개발 환경의 예외 정보 출력
    [Conditional("UNITY_EDITOR")]
    [Conditional("DEVELOPMENT_BUILD")]
    public static void LogException(System.Exception exception, UnityEngine.Object context = null)
    {
        UnityEngine.Debug.LogException(exception, context);
    }
}
