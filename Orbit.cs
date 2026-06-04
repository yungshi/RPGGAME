using UnityEngine;

public class Orbit : MonoBehaviour
{
    public Transform target;//타겟 위치 변수
    public float orbitSpeed;//회전 속도
    Vector3 offSet;//간격에 대한 백터

    void Start()//타겟과 오브젝트 간격
    {
        offSet = transform.position - target.position;
    }

    void Update()//프래임 마다 위치 업데이트
    {
        transform.position = target.position + offSet;
        transform.RotateAround(target.position, Vector3.up, orbitSpeed * Time.deltaTime);
        offSet = transform.position - target.position;
    }
}
