using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using UnityEngine;

public class Item : MonoBehaviourPun
{
    [SerializeField]
    public ItemData[] dataSet;

    [SerializeField]
    private float respawnTime = 10f;

    private float originY;

    // 아이템이 중력을 받아 공중에서 떨어지는 효과를 구현하기 위해 RigidiBody를 설정
    public void OnEnable()
    {
        Rigidbody rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.isKinematic = false;
        }

        // 아이템이 풀링됐을 때 코루틴이 남아있을 경우를 대비해 남아있는 코루틴을 제거 후 다시 적용
        if (PhotonNetwork.IsMasterClient)
        {
            CancelInvoke("DestroySelf");
            Invoke("DestroySelf", respawnTime);
        }

        // 3. 기준 Y값 갱신
        originY = transform.position.y;
    }

    void FixedUpdate()
    {
        // 아이템 스폰은 15f 높이로 고정이기에 하드하게 코딩했음. 서로를 참조받는 형식보다 빠름.
        float transformY = originY - transform.position.y;

        if (transformY >= 10f && !GetComponent<Rigidbody>().isKinematic)
        {
            GetComponent<Rigidbody>().isKinematic = true;
            transform.position = new Vector3(transform.position.x, originY - 10f, transform.position.z);
        }
    }

    void DestroySelf()
    {
        if (photonView.IsMine && gameObject != null)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    // 콜라이더 형식일 경우 아이템에 걸려 순간 막히는 경험을 주기에 트리거 형식으로 설정
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (PhotonNetwork.IsMasterClient)
            {
                ApplyRandomEffect(other.gameObject);
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    // ItemData 배열에서 랜덤으로 하나를 선택해 RPC로 아이템 효과를 플레이어에게 전달하는 함수
    // 아이템 프리팹에 데이터를 만들어 붙여주기만 하면 된다.
    void ApplyRandomEffect(GameObject player)
    {
        PhotonView playerPV = player.GetComponent<PhotonView>();
        if (playerPV != null)
        {
            int randomIndex = Random.Range(0, dataSet.Length);

            playerPV.RPC("RPC_GetItem", playerPV.Owner, dataSet[randomIndex].name);
        }
    }
}