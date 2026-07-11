using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PhotonPoolingManager : MonoBehaviour, IPunPrefabPool
{
    public static PhotonPoolingManager instance;

    private Dictionary<string, Queue<GameObject>> poolDict = new Dictionary<string, Queue<GameObject>>();

    private Transform poolRoot;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            GameObject root = new GameObject("Pooling");
            poolRoot = root.transform;

            PhotonNetwork.PrefabPool = this;

            DontDestroyOnLoad(poolRoot.gameObject);
            DontDestroyOnLoad(gameObject);
        }
    }

    public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
    {
        if (poolRoot == null)
        {
            GameObject root = new GameObject("Pooling");
            poolRoot = root.transform;
            DontDestroyOnLoad(poolRoot.gameObject);
        }

        string actualKey = prefabId;

        if (!poolDict.ContainsKey(actualKey))
        {
            foreach (string key in poolDict.Keys)
            {
                if (key.EndsWith("/" + actualKey) || actualKey.EndsWith("/" + key))
                {
                    actualKey = key;
                    break;
                }
            }
        }

        if (!poolDict.ContainsKey(actualKey))
        {
            poolDict.Add(actualKey, new Queue<GameObject>());
        }

        if (poolDict[actualKey].Count > 0)
        {
            GameObject obj = poolDict[actualKey].Dequeue();

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            PrepareForNetworkReuse(obj);
            return obj;
        }

        GameObject prefab = Resources.Load<GameObject>(prefabId);
        GameObject newObj = Object.Instantiate(prefab, position, rotation);

        newObj.name = actualKey;

        newObj.transform.SetParent(poolRoot);
        newObj.SetActive(false);
        PrepareForNetworkReuse(newObj);
        return newObj;
    }

    private static void PrepareForNetworkReuse(GameObject obj)
    {
        if (obj == null) return;

        Photon.Pun.UtilityScripts.MoveByKeys[] movementScripts =
            obj.GetComponentsInChildren<Photon.Pun.UtilityScripts.MoveByKeys>(true);

        foreach (Photon.Pun.UtilityScripts.MoveByKeys movement in movementScripts)
        {
            movement.PrepareForNetworkReuse();
        }
    }

    public void Destroy(GameObject gameObject)
    {
        string prefabId = gameObject.name.Replace("(Clone)", "").Trim();

        if (!poolDict.ContainsKey(prefabId))
        {
            foreach (string key in poolDict.Keys)
            {
                if (key.EndsWith("/" + prefabId))
                {
                    prefabId = key;
                    break;
                }
            }
            if (!poolDict.ContainsKey(prefabId))
            {
                poolDict.Add(prefabId, new Queue<GameObject>());
            }
        }

        gameObject.SetActive(false);
        poolDict[prefabId].Enqueue(gameObject);
    }
    public void ClearPool()
    {
        foreach (var queue in poolDict.Values)
        {
            while (queue.Count > 0)
            {
                GameObject obj = queue.Dequeue();
                if (obj != null) Object.Destroy(obj);
            }
        }
        poolDict.Clear();

        if (poolRoot != null)
        {
            Object.Destroy(poolRoot.gameObject);
            poolRoot = null;
        }
    }
}
