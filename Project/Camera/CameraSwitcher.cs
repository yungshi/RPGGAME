using UnityEngine;

// 기능:쿼터뷰,백뷰 전환기,
//스크립트 위치:Main Camera 
//백뷰:마우스 드래그로 케릭터 방향 전환
//쿼터뷰:마우스 포인트로 케릭터 방향 전환
public class CameraSwitcher : MonoBehaviour
{
    public enum CameraType { QuarterView, BackView }
    public CameraType cameraType = CameraType.QuarterView;
    public KeyCode toggleKey = KeyCode.V;

    public Transform target;
    public Vector3 quarterOffset;       // 기존 쿼터뷰 오프셋
    public Vector3 quarterEuler;        // 기존 쿼터뷰 회전
    public float backDistance = 6f;     // 백뷰: 플레이어와의 거리
    public float backLookHeight = 1.5f; // 백뷰: 바라보는 지점 높이

    [Header("백뷰 마우스 드래그 회전")]
    public int dragButton = 1;          // 0=좌클릭, 1=우클릭, 2=휠클릭
    public float rotateSpeed = 3f;      // 드래그 감도
    public float defaultPitch = 15f;    // 백뷰 진입 시 내려다보는 각도
    public float minPitch = -20f;
    

    [Header("백뷰 카메라 위치 보정 (카메라 로컬: x=좌우, y=상하, z=앞뒤)")]
    public Vector3 backPositionOffset = Vector3.zero;
public float maxPitch = 60f;

    Camera follow;          // 기존 커스텀 팔로우(같은 어셈블리의 클래스 Camera)
    Behaviour aimRotation;  // PlayerRotation (마우스 조준 회전)
    Behaviour aimFloow;     // FloowCamera (마우스 조준 회전)
    float yaw, pitch;
    bool backInit;

    void Awake()
    {
        follow = GetComponent<Camera>();
        if (follow != null)
        {
            if (target == null) target = follow.target;
            quarterOffset = follow.offset;
            follow.enabled = false; // 충돌 방지: 기존 팔로우 끄고 대체
        }
        quarterEuler = transform.eulerAngles;

        if (target != null)
        {
            aimRotation = target.GetComponent("PlayerRotation") as Behaviour;
            aimFloow    = target.GetComponent("FloowCamera") as Behaviour;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            cameraType = cameraType == CameraType.QuarterView
                ? CameraType.BackView : CameraType.QuarterView;
            if (cameraType == CameraType.BackView) backInit = false;
        }

        if (target == null) return;

        if (cameraType == CameraType.QuarterView)
        {
            SetAim(true);          // 쿼터뷰: 마우스 조준 회전 복구
            SetCursorLock(false);  // 커서 표시(포인트 조준용)
            transform.position = target.position + quarterOffset;
            transform.eulerAngles = quarterEuler;
        }
        else // BackView : FPS식 마우스 룩 / 핑거 룩
        {
            SetAim(false);         // 포인트 조준 회전 끔
            SetCursorLock(true);   // 커서 잠금/숨김 → 마우스 이동이 곧 시점 회전

            if (!backInit)
            {
                yaw = target.eulerAngles.y;
                pitch = defaultPitch;
                backInit = true;
            }

            // 마우스/터치 이동량 → 시점(yaw/pitch)
            if (Input.touchCount > 0) // 핑거 룩(모바일)
            {
                Vector2 d = Input.GetTouch(0).deltaPosition;
                yaw   += d.x * rotateSpeed * 0.05f;
                pitch -= d.y * rotateSpeed * 0.05f;
            }
            else // 마우스 룩(PC)
            {
                yaw   += Input.GetAxis("Mouse X") * rotateSpeed;
                pitch -= Input.GetAxis("Mouse Y") * rotateSpeed;
            }
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            // 좌우(yaw)는 캐릭터 본체를 직접 회전 = 시점이 마우스에 다이렉트 연결
            target.rotation = Quaternion.Euler(0f, yaw, 0f);

            // 카메라는 캐릭터 뒤에서 상하(pitch)까지 적용
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focus = target.position + Vector3.up * backLookHeight;
            transform.position = focus - rot * Vector3.forward * backDistance + rot * backPositionOffset;
            transform.rotation = rot;
        }
    }

    // 마우스 조준 회전 스크립트 on/off (스크립트는 수정하지 않고 enabled만 토글)
    void SetAim(bool on)
    {
        if (aimRotation != null && aimRotation.enabled != on) aimRotation.enabled = on;
        if (aimFloow    != null && aimFloow.enabled    != on) aimFloow.enabled    = on;
    }


    // 커서 잠금/숨김 토글 (마우스 룩)
    void SetCursorLock(bool locked)
    {
        CursorLockMode mode = locked ? CursorLockMode.Locked : CursorLockMode.None;
        if (Cursor.lockState != mode)
        {
            Cursor.lockState = mode;
            Cursor.visible = !locked;
        }
    }

}

