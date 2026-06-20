using Photon.Pun;
using UnityEngine.AI;
using UnityEngine;

[RequireComponent(typeof(NavMeshAgent), typeof(PhotonView))]
public class NPC : MonoBehaviourPun
{
    // 최대 이동할 수 있는 거리
    public float wanderRadius = 150f;
    // 새로운 목적지로 이동하기 전 대기 시간 (적용되는지는 테스트 필요)
    public float wanderTimer = 7f;

    protected NavMeshAgent agent;
    protected float timer;

    [Header("Settings")]
    // 죽을 때 호출되는 이펙트 인덱스
    [SerializeField]
    private int explosionEffectIndex = 1;
    // NPC를 종류별로 배열에 할당하여 랜덤하게 리스폰할 수 있도록 설정
    [SerializeField]
    private string[] respawnPrefabs;
    [SerializeField]
    private float respawnTime;
    public bool isExploded = false;

    // 재사용될 때마다 필요한 값을 초기화
    public void OnEnable()
    {
        // 1. 논리 상태 리셋
        isExploded = false;
        timer = wanderTimer;

        if (PhotonNetwork.IsMasterClient)
        {
            if (agent == null) agent = GetComponent<NavMeshAgent>();

            // 2. 에이전트 초기화 (이게 핵심!)
            if (agent != null)
            {
                agent.enabled = false;
                agent.enabled = true;
                agent.ResetPath(); // 이전 경로 삭제
            }
        }
    }

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        timer = wanderTimer;
    }

    protected virtual void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        timer += Time.deltaTime;

        if (timer >= wanderTimer || agent.remainingDistance <= agent.stoppingDistance)
        {
            Vector3 newPos = RandomNavMeshLocation(wanderRadius);
            agent.SetDestination(newPos);
            timer = 0;
        }
    }

    // 정해진 최대 위치 반경 내에서 NavMesh 위의 랜덤한 위치를 반환하는 함수
    protected Vector3 RandomNavMeshLocation(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;
        NavMeshHit hit;
        Vector3 finalPosition = transform.position;

        if (NavMesh.SamplePosition(randomDirection, out hit, radius, 1))
        {
            finalPosition = hit.position;
        }
        return finalPosition;
    }

    // IgnoreNPC를 제외한 장애물, 투사체와 충돌 시 사망하고 아이템을 스폰 Death()
    protected void OnTriggerEnter(Collider other)
    {

        if (!PhotonNetwork.IsMasterClient || isExploded) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("IgnoreNPC"))
        {
            return;
        }

        // 장애물(Obstacle) 혹은 투사체(Projectile) 태그 확인
        if (other.CompareTag("Obstacle") || other.CompareTag("Projectile") || other.CompareTag("NPCProjectile"))
        {

            isExploded = true;

            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.RequestExplosion(explosionEffectIndex, transform.position);
            }

            if (RespawnManager.Instance != null)
            {
                int randomIndex = Random.Range(0, respawnPrefabs.Length);
                string selectedPrefab = respawnPrefabs[randomIndex];
                RespawnManager.Instance.RespawnNPC("NPC/" + selectedPrefab, transform.position, respawnTime);
            }
            Death();
        }
    }

    protected void OnParticleCollision(GameObject other)
    {
        if (!PhotonNetwork.IsMasterClient || isExploded) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("IgnoreNPC"))
        {
            return;
        }

        if (other.CompareTag("Projectile") || other.CompareTag("NPCProjectile"))
        {
            isExploded = true;

            if (EffectManager.Instance != null)
            {
                EffectManager.Instance.RequestExplosion(explosionEffectIndex, transform.position);
            }
            if (RespawnManager.Instance != null)
            {
                int randomIndex = Random.Range(0, respawnPrefabs.Length);
                string selectedPrefab = respawnPrefabs[randomIndex];
                RespawnManager.Instance.RespawnNPC("NPC/" + selectedPrefab, transform.position, respawnTime);
            }
            Death();
        }
    }

    void Death()
    {
        PhotonNetwork.Instantiate("NPC/Item", transform.position + new Vector3(0,15f,0), Quaternion.identity);
        PhotonNetwork.Destroy(gameObject);
    }
}
