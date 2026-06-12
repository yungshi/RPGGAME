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
    public Vector3 quarterOffset;       //쿼터뷰 오프셋
    public Vector3 quarterEuler;        //쿼터뷰 회전
    public float backDistance = 6f;     //백뷰 거리
    public float backLookHeight = 1.5f; //백뷰 높이

    [Header("백뷰 마우스 드래그 회전")]
    public int dragButton = 1;          // 0=좌클릭 1=우클릭 2=휠클릭
    public float rotateSpeed = 3f;      //감도
    public float defaultPitch = 15f;    // 백뷰 각도
    public float minPitch = -20f;
    

    [Header("백뷰 카메라 위치 보정 (카메라 로컬: x=좌우, y=상하, z=앞뒤)")]
    public Vector3 backPositionOffset = Vector3.zero;
public float maxPitch = 60f;

    Camera follow;          // Camera 클래스
    Behaviour aimRotation;  //마우스 회전
    Behaviour aimFloow;     // 마우스 회전
    float yaw, pitch;
    bool backInit;

    void Awake()//시작시 실행동작
    {
        follow = GetComponent<Camera>();
        if (follow != null)
        {
            if (target == null) target = follow.target;
            quarterOffset = follow.offset;
            follow.enabled = false; 
        }
        quarterEuler = transform.eulerAngles;

        if (target != null)
        {
            aimRotation = target.GetComponent("PlayerRotation") as Behaviour;
            aimFloow    = target.GetComponent("FloowCamera") as Behaviour;
        }
    }

    void Update()//마으스 회전 클릭 등을 실시간 업데이트
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
            SetAim(true);         
            SetCursorLock(false);  
            transform.position = target.position + quarterOffset;
            transform.eulerAngles = quarterEuler;
        }
        else 
        {
            SetAim(false);         
            SetCursorLock(true);   

            if (!backInit)
            {
                yaw = target.eulerAngles.y;
                pitch = defaultPitch;
                backInit = true;
            }

            
            if (Input.touchCount > 0) 
            {
                Vector2 d = Input.GetTouch(0).deltaPosition;
                yaw   += d.x * rotateSpeed * 0.05f;
                pitch -= d.y * rotateSpeed * 0.05f;
            }
            
            {
                yaw   += Input.GetAxis("Mouse X") * rotateSpeed;
                pitch -= Input.GetAxis("Mouse Y") * rotateSpeed;
            }
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        
            target.rotation = Quaternion.Euler(0f, yaw, 0f);

            
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            Vector3 focus = target.position + Vector3.up * backLookHeight;
            transform.position = focus - rot * Vector3.forward * backDistance + rot * backPositionOffset;
            transform.rotation = rot;
        }
    }

    
    void SetAim(bool on)//마우스 조준
    {
        if (aimRotation != null && aimRotation.enabled != on) aimRotation.enabled = on;
        if (aimFloow    != null && aimFloow.enabled    != on) aimFloow.enabled    = on;
    }


    
    void SetCursorLock(bool locked)//마우스 커서 껏다 키기
    {
        CursorLockMode mode = locked ? CursorLockMode.Locked : CursorLockMode.None;
        if (Cursor.lockState != mode)
        {
            Cursor.lockState = mode;
            Cursor.visible = !locked;
        }
    }

}

