using UnityEngine;

//하나의 인스턴스를 공유하는 싱글톤 관리
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    private static bool _applicationIsQuitting = false;

    public static T instance
    {
        get
        {
            if (_applicationIsQuitting)
                return null;

            if (_instance == null)
            {
                //현재 씬의 인스턴스를 한 번 검색
                //인스턴스가 없을 때 자동 생성하지 않음
                _instance = FindFirstObjectByType<T>();
            }

            return _instance;
        }
    }

    [SerializeField] protected bool isDontDestroy = false;

    //싱글톤 인스턴스 등록과 중복 제거
    protected virtual void Awake()
    {
        _applicationIsQuitting = false;

        if (_instance == null)
        {
            _instance = this as T;

            if (isDontDestroy)
                DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    //등록된 인스턴스가 제거될 때 참조 초기화
    protected virtual void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    //앱 종료 중 싱글톤 재탐색 방지
    protected virtual void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }
}
