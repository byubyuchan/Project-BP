using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class AIController : MonoBehaviour
{
    public Transform[] goals;
    NavMeshAgent agent;
    private int currentGoalIndex = -1;
    private Rigidbody rb;
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            enabled = false;
            return;
        }

        FindAllGoalsByTag();

        agent = GetComponent<NavMeshAgent>();
        if (agent == null)
        {
            enabled = false;
            return;
        }

        SetRandomDestination();
    }

    void Update()
    {
        if (agent != null && animator != null)
        {
            // NavMeshAgent의 현재 속도 벡터를 가져와서 magnitude (크기)를 계산합니다.
            // 속도가 0에 가까우면 멈춘 상태, 0보다 크면 움직이는 상태로 간주합니다.
            float speed = agent.velocity.magnitude;

            if (speed > 0.1f)
            {
                // 움직이고 있을 때
                animator.SetBool("Run", true);
                animator.SetBool("Idle", false);
            }
            else
            {
                // 멈춰 있을 때
                animator.SetBool("Idle",true);
                animator.SetBool("Run", false);
            }
        }

        if (agent.hasPath && agent.remainingDistance < agent.stoppingDistance)
        {
            SetRandomDestination();
        }
    }

    // 무작위 목표 지점을 설정하는 메서드
    void SetRandomDestination()
    {
        if (goals.Length == 0)
        {
            Debug.LogError("Goals 배열이 비어 있습니다. 목표 지점을 설정해 주세요.");
            return;
        }

        int newGoalIndex;

        do
        {
            newGoalIndex = Random.Range(0, goals.Length);
        } while (newGoalIndex == currentGoalIndex);

        // 새 목표 인덱스 업데이트
        currentGoalIndex = newGoalIndex;

        // 선택된 목표의 위치로 이동 설정
        agent.SetDestination(goals[currentGoalIndex].position);
    }

    void FindAllGoalsByTag()
    {
        GameObject[] goalObjects = GameObject.FindGameObjectsWithTag("Goal");

        goals = goalObjects.Select(g => g.transform).ToArray();
    }
}