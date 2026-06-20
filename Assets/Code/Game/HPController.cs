using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using System.Collections;
using UnityEngine;

public class HPController : MonoBehaviourPunCallbacks, IPunObservable
{
    public float Hp = 100f;
    public float maxHp;

    [SerializeField]
    private GameObject UIprefab;

    [SerializeField]
    private float respawnDelay = 3f;

    private GameObject myUIInstance;

    public bool isDead = false;

    private void Awake()
    {
        maxHp = Hp;
    }
    
    private new void OnEnable()
    {
        base.OnEnable();
        Hp = maxHp;
        isDead = false;

        if (photonView.IsMine)
        {
            var moveScript = GetComponent<Photon.Pun.UtilityScripts.MoveByKeys>();
            if (moveScript != null)
            {
                moveScript.enabled = true;
            }

            var inputSystem = GetComponent<UnityEngine.InputSystem.PlayerInput>();
            if (inputSystem != null)
            {
                inputSystem.enabled = true;
            }
        }

        if (this.UIprefab != null)
        {
            StartCoroutine(InitPlayerUIRoutine());
        }
    }

    // 캐릭터가 제대로 생성된 후 OnEnable 시작
    private IEnumerator InitPlayerUIRoutine()
    {
        while (photonView == null || photonView.Owner == null)
        {
            yield return null; // 다음 프레임에 다시 확인
        }

        myUIInstance = Instantiate(this.UIprefab, Vector3.zero, Quaternion.identity);
        PlayerUI playerUI = myUIInstance.GetComponent<PlayerUI>();

        if (playerUI != null)
        {
            playerUI.SetTarget(this);
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        photonView.RPC("RPC_BroadcastDie", RpcTarget.All);
    }

    // Photon Update()
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(this.Hp);
        }
        else
        {
            this.Hp = (float)stream.ReceiveNext();
        }
    }

    // RPC를 발생시킨 객체가 this
    [PunRPC]
    private void RPC_BroadcastDie()
    {
        this.isDead = true;
        this.Hp = 0f;

        if (photonView.IsMine)
        {
            if (PlayerSpawner.instance != null)
            {
                PlayerSpawner.instance.RequestRespawn(gameObject, respawnDelay);
            }
        }
    }
}