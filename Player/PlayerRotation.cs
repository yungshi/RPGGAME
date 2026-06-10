using UnityEngine;

// 플레이어 회전 전담 스크립트.
// 마우스 커서가 가리키는 지점을 향해 플레이어를 수평으로 회전시킨다.
// (기존에 FloowCamera / PlayerControl 에 흩어져 있던 회전 로직을 한곳으로 분리)
public class PlayerRotation : MonoBehaviour
{
    [Tooltip("조준 기준 카메라. 비워두면 Camera.main 을 사용")]
    public UnityEngine.Camera followCamera;

    void Awake()
    {
        if (followCamera == null)
            followCamera = UnityEngine.Camera.main;
    }

    void Update()
    {
        Turn();
    }

void Turn()
    {
        if (followCamera == null)
            return;

        // 마우스 위치에서 카메라 레이를 쏘다
        Ray ray = followCamera.ScreenPointToRay(Input.mousePosition);

        // 플레이어 높이의 수평 평면과만 교차 (날아다니는 총알/벽/플레이어 콜라이더에 영향받지 않게)
        Plane ground = new Plane(Vector3.up, transform.position);
        if (ground.Raycast(ray, out float enter))
        {
            Vector3 point = ray.GetPoint(enter);
            Vector3 nextVec = point - transform.position;
            nextVec.y = 0f; // 수평 회전만
            if (nextVec.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(nextVec);
        }
    }
}
