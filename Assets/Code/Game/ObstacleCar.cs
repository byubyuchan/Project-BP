using Photon.Pun;
using UnityEngine;
using System.Collections;

public class ObstacleCar : Obstacle
{
    // 자동차의 경우 바퀴와 프레임이 각각인 경우가 많아 회전하는 각도를 각각 지정해줘야 함
    [Header("Car Movement")]
    public float driveSpeed = 10f;
    public Vector3 driveDirection = Vector3.forward;
    public Vector3 wheelDirection = Vector3.forward;
    public GameObject[] wheel;
    public float resetInterval = 5f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    void Start()
    {
        // 시작할 때 현재 위치와 회전값을 저장해둡니다.
        startPosition = transform.position;
        startRotation = transform.rotation;

        // 5초마다 리셋하는 코루틴 시작
        StartCoroutine(ResetPositionRoutine());
    }

    IEnumerator ResetPositionRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(resetInterval); // 5초 대기

            // 위치와 회전을 처음 상태로 되돌림
            transform.position = startPosition;
            transform.rotation = startRotation;
        }
    }

    void Update()
    {
        transform.Translate(driveDirection * driveSpeed * Time.deltaTime);
        for (int i = 0; i < wheel.Length; i++)
        {
            wheel[i].transform.Rotate(wheelDirection, driveSpeed * 360f * Time.deltaTime);
        }
    }

    // 자동차 특유의 처리가 필요하다면 재정의
    protected override void OnPlayerHit(GameObject player)
    {

        PhotonView targetPV = player.GetComponent<PhotonView>();
        if (targetPV != null)
        {
            Vector3 pushDir = (player.transform.position - transform.position).normalized;
            pushDir.y = upwardForce;

            // 2. 넉백 실행
            targetPV.RPC("RPC_AddKnockback", RpcTarget.All, pushDir * knockbackForce);
        }
    }
}