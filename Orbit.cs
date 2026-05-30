using UnityEngine;

public class Orbit : MonoBehaviour
{
    public Transform target;
    public float orbitSpeed;
    Vector3 offSet;

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
