using UnityEngine;

public class playerBugFixed : MonoBehaviour
{
    Rigidbody rigid;
    PlayerControl playerControl;
    FloowCamera floowCamera;
    Quaternion lastFacing;

    void Awake()
    {
        rigid = GetComponent<Rigidbody>();
        playerControl = GetComponent<PlayerControl>();
        floowCamera = GetComponent<FloowCamera>();
        if (rigid != null)
        {
            rigid.constraints = RigidbodyConstraints.FreezeRotation;
            rigid.angularVelocity = Vector3.zero;
            rigid.maxAngularVelocity = 0f;
        }
        lastFacing = transform.rotation;
    }

void LateUpdate()
    {
        if (rigid != null)
            rigid.angularVelocity = Vector3.zero;
        // 회전은 FloowCamera(마우스 조준)가 전담한다.
        // 이동 방향으로 강제 회전하지 않아 조준과 발사 방향을 일치시킨다.
    }
}
