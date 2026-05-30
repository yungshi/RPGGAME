using UnityEngine;
using UnityEngine.AI;

public class BossMissile : Bullet //Bullet 클래스 기반
{
    public Transform target;
    NavMeshAgent nav;//길을 찾기 위해 선언
    void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
    }

    void Update()
    {
        nav.SetDestination(target.position);//플레이어 실시간 추적
        
    }
}
