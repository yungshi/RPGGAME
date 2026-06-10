using UnityEngine;

public class FloowCamera : MonoBehaviour
{
    public UnityEngine.Camera followCamera;
    public Attack_1 attack_1;

    void Update()
    {
        Turn();
    }

    void Turn()
    {
        if (followCamera == null)
            return;

        // 마우스 포인트 방향으로 항상 회전 (무기/발사 여부 무관)
        Ray ray = followCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit rayHit, 100f))
        {
            Vector3 nextVec = rayHit.point - transform.position;
            nextVec.y = 0f; // Y축 고정: 플레이어가 앞뒤로 기울어지지 않게
            if (nextVec != Vector3.zero)
                transform.LookAt(transform.position + nextVec);
        }
    }
}
