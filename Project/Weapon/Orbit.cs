using UnityEngine;

public class Orbit : MonoBehaviour
{
    public Transform target;//타겟 위치 변수
    public float orbitSpeed;//회전 속도
    Vector3 offSet;//간격에 대한 백터

    void Awake()//물리엔지 버그 방지
    {
        
        foreach (Rigidbody rb in GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            if (!col.isTrigger)
                col.enabled = false;
        }
    }

    void Start()//타겟과 오브젝트 사이 간격
    {
        offSet = transform.position - target.position;
    }

    void Update()//업데이트
    {
        transform.position = target.position + offSet;
        transform.RotateAround(target.position, Vector3.up, orbitSpeed * Time.deltaTime);
        offSet = transform.position - target.position;
    }
}
