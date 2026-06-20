using UnityEngine;
using Photon.Pun;
using System.Collections;

public class RespawnManager : MonoBehaviourPun
{
    public static RespawnManager Instance;

    void Awake()
    {
        Instance = this;
    }

    // NPC가 죽을 때 호출할 함수
    public void RespawnNPC(string prefabName, Vector3 position, float delay)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            StartCoroutine(RespawnRoutine(prefabName, position, delay));
        }
    }

    IEnumerator RespawnRoutine(string prefabName, Vector3 position, float delay)
    {
        yield return new WaitForSeconds(delay);

        // 10초 뒤 새로운 NPC 생성 (SceneObject로 생성해야 방장이 바뀌어도 유지됨)
        PhotonNetwork.InstantiateRoomObject(prefabName, position, Quaternion.identity);
    }
}