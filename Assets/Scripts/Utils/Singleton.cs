using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance;

    protected void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = gameObject.GetComponent<T>();
    }
}

public class SingletonPersistent<T> : Singleton<T> where T : MonoBehaviour
{
    protected new void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
}