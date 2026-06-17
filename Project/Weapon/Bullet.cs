using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage;//공격력
    public bool isMelee;//원거리
    public bool isRock;//근접
    void OnCollisionEnter(Collision collision)
    {
        string tag = collision.gameObject.tag;
        if (!isRock && tag == "Floor")//지면에 충돌
        {
            Destroy(gameObject, 3);
        }
        else if (!isMelee && tag == "Wall")//벽에 충돌
        {
            Destroy(gameObject);
        }
        
    }

    void OnTriggerEnter(Collider other)//원거리 무기일 경우
    {
        if (!isMelee && other.gameObject.tag == "Wall")
        {
            Destroy(gameObject);
        }
    }
}
