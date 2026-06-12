using UnityEngine;

public class FloowCamera : MonoBehaviour
{
    public UnityEngine.Camera followCamera;
    public Attack_1 attack_1;

    void Update()//회전 업데이트
    {
        Turn();
    }

    void Turn()//마우스 방향으로 회전
    {
        if (followCamera == null)
            return;

        
        Ray ray = followCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit rayHit, 100f))
        {
            Vector3 nextVec = rayHit.point - transform.position;
            nextVec.y = 0f; 
            if (nextVec != Vector3.zero)
                transform.LookAt(transform.position + nextVec);
        }
    }
}
