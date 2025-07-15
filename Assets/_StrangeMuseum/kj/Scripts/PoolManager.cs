using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    private static PoolManager instance;

    public static PoolManager Instance
    {
        get
        {
            return instance;
        }
    }

    class Pool
    {
        //Pool_Root -> Unitychan_Root 빈 객체가 있다면 Unitychang은 Unitychan_Root 자식으로 이동.

        public GameObject Original
        {
            get;
            private set;
        }
        public Transform Root
        {
            get;
            set;
        }

        Stack<Poolable> _poolstack = new Stack<Poolable>(); //Pool은 stack구조로 관리

        public void  Init(GameObject _originial,int count = 2)
        {
            Original = _originial;

            Root = new GameObject().transform;

            Root.name = $"{_originial.name}_Root";

            for(int i = 0; i< count; i++)
            {
                Push(Create());
            }
        }

        Poolable Create()
        {
            GameObject go = Object.Instantiate<GameObject>(Original);
            go.name = Original.name;


            return go.GetOrAddComponent<Poolable>();
        }

        public void Push(Poolable poolable)
        {
            if(poolable == null)
            {
                return;
            }

            poolable.transform.parent = Root;
            poolable.gameObject.SetActive(false);
            poolable.isUsing = false;

            _poolstack.Push(poolable);
        }

        public Poolable Pop(Transform _parent)
        {
            Poolable poolable;

            if(_poolstack.Count > 0)
            {
                poolable = _poolstack.Pop();
            }
            else
            {
                poolable = Create();
            }

            poolable.transform.parent = _parent;
            poolable.gameObject.SetActive(true);
            poolable.isUsing = true;

            return poolable;
        }
    }

    Dictionary<string, Pool> _poolDictionary = new Dictionary<string, Pool>(); //Pool목록들은 딕셔너리 형태로 저장, string을 키로 하여 특정 pool을 반환받는 느낌
    Transform rootParent;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
        Init();

    }

    Transform root;

    public void Init()
    {
        //1. 정리 차원에서 풀링할 오브젝트들을 @Pool_Root 부모의 자식으로 넣어둘 것임

        if(root == null)
        {
            root = new GameObject
            {
                name = "@Pool_Root"
            }.transform;

            Object.DontDestroyOnLoad(root);
        }
    }

    public void CreatePool(GameObject original,int count = 2)
    {
        Pool pool = new Pool();
        pool.Init(original, count);
        pool.Root.parent = rootParent;

        _poolDictionary.Add(original.name, pool);
    }

    public void Push(Poolable poolable)
    {
        string name = poolable.gameObject.name;

        if(_poolDictionary.ContainsKey(name) == false)
        {
            GameObject.Destroy(poolable.gameObject);
            return;
        }
        _poolDictionary[name].Push(poolable);
    }

    public Poolable Pop(GameObject original, Transform parent = null, Vector3? position = null, Quaternion? rotation = null)
    {
        //@Pool_Root 가 PoolManagager가 되서, 원본 프리펩을 전달 받으면 자식으로 있는 오브젝트들 중 일치한 프리펩을 뽑는다.

        if (_poolDictionary.ContainsKey(original.name) == false)
        {
            CreatePool(original);
        }

        Poolable poolable = _poolDictionary[original.name].Pop(parent);

        if (position.HasValue)
            poolable.transform.position = position.Value;

        if (rotation.HasValue)
            poolable.transform.rotation = rotation.Value;

        return poolable;
    }

    public GameObject GetOriginal(string name) //Pop 메서드에서 원본 프리펩이 있다면 반환하는 메서드
    {
        if (_poolDictionary.ContainsKey(name) == false)
        {
            return null;
        }

        return _poolDictionary[name].Original ;
    }

    public void Clear()
    {
        foreach(Transform child in rootParent)
        {
            GameObject.Destroy(child.gameObject);
        }

        _poolDictionary.Clear();
    }
}
