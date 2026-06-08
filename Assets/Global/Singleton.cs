using UnityEngine;

namespace Global
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static byte[] _lock = new byte[0];
        private static T _instance;
        private static bool _applicationIsQuitting = false;

        public static T Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning($"[Singleton] {typeof(T)} (이)가 진즉에 파괴됐거나 게임이 꺼진거 같은데?");
                    return null;
                }

                lock (_lock)
                {
                    if (_instance != null) return _instance;
                    
                    _instance = (T)FindAnyObjectByType(typeof(T));

                    if (_instance != null) return _instance;
                    
                    var singletonObject = new GameObject();
                    _instance = singletonObject.AddComponent<T>();
                    singletonObject.name = typeof(T).ToString() + " (Singleton)";
                    DontDestroyOnLoad(singletonObject);
                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Debug.LogWarning($"[Singleton] {typeof(T)}(이)가 이미 있잖아ㅏㅏㅏㅏㅏ");
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}