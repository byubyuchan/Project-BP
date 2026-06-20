using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

// 포톤에 내장된 풀링 시스템을 사용한 풀링 매니저 클래스
public class PhotonPoolingManager : MonoBehaviour, IPunPrefabPool
{
    public static PhotonPoolingManager instance;

    // 풀링된 오브젝트들을 저장하는 딕셔너리 (키: 프리팹 이름, 값: 오브젝트 큐)
    private Dictionary<string, Queue<GameObject>> poolDict = new Dictionary<string, Queue<GameObject>>();

    // 풀링된 오브젝트들을 관리하기 위한 루트 오브젝트
    private Transform poolRoot;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;

            // 루트 오브젝트 생성
            GameObject root = new GameObject("Pooling");
            poolRoot = root.transform;

            // 포톤네트워크의 생성과 파괴를 이 스크립트가 점령
            PhotonNetwork.PrefabPool = this;

            // 이 스크립트를 갖고 있는 오브젝트와 풀링 오브젝트들을 관리하는 루트 오브젝트는 DDOL로 씬이 이동하더라도 파괴되지 않음
            DontDestroyOnLoad(poolRoot.gameObject);
            DontDestroyOnLoad(gameObject);
        }
    }

    // 주소가 다르더라도 이름이 같으면 오류가 생기는 문제를 해결하기위한 개선된 풀링
    public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
    {
        // 게임을 나갔다 들어오거나 루트 오브젝트가 파괴된 경우 다시 생성
        if (poolRoot == null)
        {
            GameObject root = new GameObject("Pooling");
            poolRoot = root.transform;
            DontDestroyOnLoad(poolRoot.gameObject);
        }

        string actualKey = prefabId;

        // 주소가 달라도 이름이 같으면 전에 같은 거 맞아요~ 라고 키 값 설정
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

        // 사용됐던 프리팹이 풀에 없다면 새로 풀 생성
        if (!poolDict.ContainsKey(actualKey))
        {
            poolDict.Add(actualKey, new Queue<GameObject>());
        }

        // 사용됐던 프리팹이 풀에 있다면 재사용, 사용 중임을 알리기 위해 Dequeue
        if (poolDict[actualKey].Count > 0)
        {
            GameObject obj = poolDict[actualKey].Dequeue();

            obj.transform.position = position;
            obj.transform.rotation = rotation;
            return obj;
        }

        // 모두 사용 중이라면 새롭게 생성
        GameObject prefab = Resources.Load<GameObject>(prefabId);
        GameObject newObj = Object.Instantiate(prefab, position, rotation);

        newObj.name = actualKey;

        newObj.transform.SetParent(poolRoot);
        newObj.SetActive(false);
        return newObj;
    }

    // 포톤이 Destroy를 호출할 때 내부적으로 이 메소드를 실행함
    public void Destroy(GameObject gameObject)
    {
        // 클론 표시 삭제
        string prefabId = gameObject.name.Replace("(Clone)", "").Trim();

        // 다른 주소, 같은 이름일 경우 같은 거 맞아요~ 라고 키 값 설정
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
            // 새 오브젝트가 반납되었을 경우를 대비해 새 바구니를 만들어 줌 (방어용)
            if (!poolDict.ContainsKey(prefabId))
            {
                poolDict.Add(prefabId, new Queue<GameObject>());
            }
        }

        // 비활성화 후 큐에 추가하여 풀링할 준비가 됐음으로 표시
        gameObject.SetActive(false);
        poolDict[prefabId].Enqueue(gameObject);
    }

    // 방을 나가거나 새로운 게임을 진행하려고 할 때 풀링 상태를 모두 초기화하는 작업 (풀에 있는 오브젝트들은 실제로 파괴)
    public void ClearPool()
    {
        foreach (var queue in poolDict.Values)
        {
            while (queue.Count > 0)
            {
                GameObject obj = queue.Dequeue();
                if (obj != null) Object.Destroy(obj); // 실제 오브젝트 파괴
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