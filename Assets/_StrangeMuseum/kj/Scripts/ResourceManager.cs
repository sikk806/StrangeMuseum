using UnityEngine;
using UnityEngine.SceneManagement;

public class ResourceManager : MonoBehaviour
{
    private static ResourceManager instance;

    public static ResourceManager Instance
    {
        get
        {
            return instance;
        }
    }

    [SerializeField]
    Camera cam;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        SceneManager.sceneLoaded += MainCameraDelete;
    }


    private void MainCameraDelete(Scene scene,LoadSceneMode mode)
    {
        if(scene.name == "NetworkTest")
        {
            Debug.Log("임시 카메라 삭제");
            if(Camera.main != null)
            {
                Destroy(cam.gameObject);
            }
      
        }
        else
        {
            DontDestroyOnLoad(cam);
            Debug.LogWarning("씬이 다르므로 임시 카메라 삭제 안됨");
        }
    }

    public T Load<T>(string path) where T : Object
    {
        return Resources.Load<T>(path);
    }

    public GameObject Instantiate(string path,Transform parent = null,Transform InitPos = null)
    {
        GameObject original = Load<GameObject>($"Prefabs/{path}");

        if(original ==null)
        {
            Debug.Log("프리펩 경로 로드 실패" + path);
            return null;
        }

        if(original.GetComponent<Poolable>() != null)
        {
            return PoolManager.Instance.Pop(original, parent, position: InitPos.position).gameObject;
        }
        else
        {
            GameObject go = Object.Instantiate(original, parent, InitPos);

            go.name = original.name;

            return go;
        }
    }

    public void Destroy(GameObject go)
    {
        if(go == null)
        {
            return;
        }

        Poolable poolable = go.GetComponent<Poolable>();
        if(poolable != null)
        {
            PoolManager.Instance.Push(poolable);
            return;
        }
        else
        {
            Object.Destroy(go);
        }
    }
}
