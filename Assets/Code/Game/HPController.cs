using Photon.Pun;
using Photon.Pun.Demo.PunBasics;
using Photon.Pun.UtilityScripts;
using Photon.Realtime;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // PlayerInput 사용을 위해 추가

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
                moveScript.isUIMode = false;
                moveScript.isMenuOpen = false;
            }

            StartCoroutine(SafeInputBoot());
        }

        if (this.UIprefab != null)
        {
            StartCoroutine(InitPlayerUIRoutine());
        }
    }

    private IEnumerator SafeInputBoot()
    {
        var inputSystem = GetComponent<PlayerInput>();
        if (inputSystem != null)
        {
            inputSystem.enabled = false;

            yield return new WaitForSeconds(0.2f);

            if (photonView != null && photonView.IsMine && !isDead)
            {
                inputSystem.enabled = true;
                Debug.Log("<color=yellow>[Input] 안전 부팅 완료! (Player Index 꼬임 방지)</color>");
            }
        }
    }

    // 캐릭터가 제대로 생성된 후 OnEnable 시작
    private IEnumerator InitPlayerUIRoutine()
    {
        while (photonView == null || photonView.Owner == null)
        {
            yield return null;
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

    [PunRPC]
    private void RPC_BroadcastDie()
    {
        this.isDead = true;
        this.Hp = 0f;

        if (photonView.IsMine)
        {
            var moveScript = GetComponent<MoveByKeys>();
            if (moveScript != null && moveScript.isLoadingAttack)
            {
                moveScript.isLoadingAttack = false;
                photonView.RPC("RPC_LoadAction", RpcTarget.All, "ReadyToAttack", false);
            }

            if (PlayerSpawner.instance != null)
            {
                photonView.RPC("RPC_SizeReset", RpcTarget.All);
                PlayerSpawner.instance.RequestRespawn(gameObject, respawnDelay);
            }
        }
    }
}