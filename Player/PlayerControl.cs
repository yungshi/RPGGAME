using System.Data.SqlTypes;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PlayerControl : MonoBehaviour
{
    public float speed;
    float hAxis;
    float vAxis;
    bool wDown;
    bool jDown;
    public UnityEngine.Camera followCamera;
    
    bool dDown;
    public bool isJump;
    public bool isDodge;
    public bool isReload; // 재장전 중 이동 잠금
    public ItemInteraction itemInteraction;

    Vector3 moveVec;
    Vector3 dodgeVec;
    Rigidbody rigid;
    Animator anim;

    void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rigid = GetComponent<Rigidbody>();

        if (itemInteraction == null)
            itemInteraction = GetComponent<ItemInteraction>();
    }

    void Update()
    {
        GetInput();
        Move();
        Jump();
        Dodge();
    }

    void GetInput()
    {
        // 기본 이동 입력은 old Input System으로 먼저 처리 (Keyboard null 여부와 무관)
        hAxis = Input.GetAxisRaw("Horizontal");
        vAxis = Input.GetAxisRaw("Vertical");
        wDown = Input.GetButton("Walk");
        jDown = Input.GetButtonDown("Jump");

        // Dodge는 new Input System(Keyboard)에서만 처리
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null)
            dDown = keyboard.leftCtrlKey.wasPressedThisFrame;
    }

    void Move()
    {
        // 카메라(백뷰) 기준 이동 — WASD가 화면에서 보이는 방향과 일치하도록 변환
        UnityEngine.Camera cam = followCamera != null ? followCamera : UnityEngine.Camera.main;
        if (cam != null)
        {
            Vector3 camF = cam.transform.forward; camF.y = 0f; camF.Normalize();
            Vector3 camR = cam.transform.right;   camR.y = 0f; camR.Normalize();
            moveVec = (camF * vAxis + camR * hAxis).normalized;
        }
        else
        {
            moveVec = new Vector3(hAxis, 0f, vAxis).normalized;
        }

        if (isDodge)
            moveVec = dodgeVec;

        if (itemInteraction != null && itemInteraction.isSwap)
            moveVec = Vector3.zero;

        if (isReload)
            moveVec = Vector3.zero;

        transform.position += moveVec * speed * (wDown ? 0.3f : 1f) * Time.deltaTime;

        if (anim != null)
        {
            anim.SetBool("isRun", moveVec != Vector3.zero);
            anim.SetBool("isWalk", wDown);
        }
    }

    void Jump()
    {
        if (jDown && !isJump && !isDodge && !(itemInteraction?.isSwap ?? false))
        {
            if (rigid != null)
                rigid.AddForce(Vector3.up * 15, ForceMode.Impulse);

            if (anim != null)
            {
                anim.SetBool("isJump", true);
                anim.SetTrigger("doJump");
            }

            isJump = true;
        }
    }

    void Dodge()
    {
        if (dDown 
            && moveVec != Vector3.zero 
            && !isJump 
            && !isDodge
            && !(itemInteraction?.isSwap ?? false))
        {
            dodgeVec = moveVec;
            speed *= 2;

            if (anim != null)
                anim.SetTrigger("doDodge");

            isDodge = true;

            Invoke("DodgeOut", 0.4f);
        }
    }

    void DodgeOut()
    {
        speed *= 0.5f;
        isDodge = false;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Floor")
        {
            anim.SetBool("isJump", false);
            isJump = false;
        }
    }

    // 외부에서 이동 방향 접근용 (playerBugFixed에서 사용)
    public Vector3 MoveVec => moveVec;
}
