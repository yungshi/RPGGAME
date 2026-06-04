using UnityEngine;

public class Item : MonoBehaviour
{
    public enum Type { Ammo, Coin, Grenade, Heart, Weapon };//아이템 종류
    public Type type;//아이템 타입
    public int value;//아이템 고유번호

    Rigidbody rigid;/리지드바디
    SphereCollider sphereCollider;//콜라이더

    void Awake()//최초실행
    {
        rigid = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
    }

    void Update()//회전 업데이트
    {
        transform.Rotate(Vector3.up * 10 * Time.deltaTime);
    }

    void OnCollisionEnter(Collision collision)//바닥에 충돌하면
    {
        if(collision.gameObject.tag =="Floor")
            {
                rigid.isKinematic = true;
                sphereCollider.enabled = false;
            }
    }
}
