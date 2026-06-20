using Photon.Pun;
using Photon.Pun.UtilityScripts;
using UnityEngine;

// NPC와 동일하게 아이템을 생성하고 사망할 수 있지만 저격총을 발사하는 스나이퍼
public class NPC_Sniper : NPC
{
    // NPC의 정면에서 플레이어를 감지하는 시야각과 감지 범위, 플레이어를 조준하는데 걸리는 시간, 시야각은 좌우로 나누어 적용 ex) 60도 -> 30도 왼쪽, 30도 오른쪽
    [Header("Sniper Settings")]
    public float viewAngle = 60f;
    public float lockOnTime = 3f;
    public float detectionRange = 50f;

    [Header("Current Status")]
    [SerializeField] private Transform targetPlayer;
    [SerializeField] private float lockOnTimer = 0f;

    // 시야 내의 모든 플레이어를 담아 둠
    [Header("Optimization")]
    public LayerMask playerLayer; 
    private Collider[] targets = new Collider[20];

    [Header("Shoot Settings")]
    [SerializeField] 
    private Transform firePoint;
    [SerializeField]
    private string projectileName = "NPCProjectile";

    [Header("Laser Settings")]
    [SerializeField] private LineRenderer laserLine;

    private Transform targetTransform;

    private new void OnEnable()
    {
        base.OnEnable();

        if (laserLine != null) laserLine.enabled = false;
        targetPlayer = null;
        lockOnTimer = 0f;
    }

    // RPC로 라인 렌더러를 관리하지 않고 필요한 부분만 방장 관리 처리
    protected override void Update()
    {
        DetectClosestPlayer();

        if (targetPlayer != null)
        {
            lockOnTimer += Time.deltaTime;
            if (PhotonNetwork.IsMasterClient && agent.hasPath) agent.ResetPath();

            RotateTowards(targetTransform.position);
            DrawLaser(targetTransform.position);
            if (PhotonNetwork.IsMasterClient && lockOnTimer >= lockOnTime)
            {
                FireRPC();
                lockOnTimer = 0;
            }
        }
        else
        {
            if (laserLine != null) laserLine.enabled = false;
            lockOnTimer = Mathf.Max(0, lockOnTimer - Time.deltaTime);

            if (PhotonNetwork.IsMasterClient) base.Update();
        }
    }

    // OverlapSphereNonAlloc을 활용하여 최적화, 최대 크기를 결정해놓고 (큰 접시) 필요한 정보만 담기에 가비지 최소화
    void DetectClosestPlayer()
    {
        int count = Physics.OverlapSphereNonAlloc(transform.position, detectionRange, targets, playerLayer);

        float closestDist = float.MaxValue;
        Transform bestTarget = null;

        // NPC 주변의 플레이어가 시야각에 들어오는지 확인하고 가장 가까운 플레이어를 타겟으로 플레이어가 맵 뒤에 숨지 않았는지 3중 확인
        for (int i = 0; i < count; i++)
        {
            Transform t = targets[i].transform;

            Transform tAimPoint = t.GetComponent<MoveByKeys>().aimPoint;

            Vector3 dirToTarget = (t.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle * 0.5f)
            {
                float dist = Vector3.Distance(transform.position, t.position);

                if (dist < closestDist)
                {
                    if (IsTargetVisible(tAimPoint.position, t))
                    {
                        closestDist = dist;
                        bestTarget = t;
                    }
                }
            }
        }

        if (targetPlayer != bestTarget)
        {
            targetPlayer = bestTarget;
            lockOnTimer = 0;
        }

        if (targetPlayer != null)
        {
            targetTransform = targetPlayer.GetComponent<MoveByKeys>().aimPoint;
        }
    }

    // 정면에서 레이캐스트를 쏴서 타겟이 벽 뒤에 숨어있는지 확인, 타겟이 시야각 안에 들어와도 벽 뒤에 숨어있으면 공격하지 않도록 함
    bool IsTargetVisible(Vector3 targetPos, Transform targetRoot)
    {
        Vector3 start = firePoint.position;
        Vector3 dir = (targetPos - start).normalized;
        float dist = Vector3.Distance(start, targetPos);
        int layerMask = playerLayer | LayerMask.GetMask("Map");

        RaycastHit hit;
        if (Physics.Raycast(start, dir, out hit, dist, layerMask))
        {
            if (hit.transform.root == targetRoot.root)
            {
                return true;
            }
        }
        return false;
    }

    void RotateTowards(Vector3 targetPos)
    {
        Vector3 direction = (targetPos - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }

    void FireRPC()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (targetPlayer == null) return;

        Vector3 fireDir = (targetTransform.position - firePoint.position).normalized;

        PhotonNetwork.Instantiate("NPC/"+projectileName, firePoint.position, Quaternion.LookRotation(fireDir));

        // RPC로 타이머를 초기화하여 모든 클라이언트에서 동기화, 방장만 발사 처리를 하고 타이머 초기화는 모두에게 RPC로 처리하여 타이머가 꼬이는 것을 방지
        photonView.RPC("RPC_ResetSniperTimer", RpcTarget.All);

        AudioManager.instance.PlaySFX("Test",this.transform.position);
    }

    void DrawLaser(Vector3 targetPos)
    {
        if (laserLine == null) return;

        if (!laserLine.enabled) laserLine.enabled = true;

        float currentWidth = Mathf.Lerp(0.3f, 0.01f, lockOnTimer / lockOnTime);

        laserLine.startWidth = currentWidth;

        laserLine.SetPosition(0, firePoint.position);

        Vector3 dir = (targetPos - firePoint.position).normalized;
        RaycastHit hit;

        int layerMask = playerLayer | LayerMask.GetMask("Map");

        if (Physics.Raycast(firePoint.position, dir, out hit, detectionRange, layerMask))
        {
            laserLine.SetPosition(1, hit.point);
        }
        else
        {
            laserLine.SetPosition(1, firePoint.position + (dir * detectionRange));
        }
    }

    [PunRPC]
    void RPC_ResetSniperTimer()
    {
        lockOnTimer = 0f;
    }
}