using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage;//공격력
    public bool isMelee;//원거리
    public bool isRock;//근접
    void OnCollisionEnter(Collision collision)
    {
        string tag = collision.gameObject.tag;
        if (!isRock && tag == "Floor")//땅에 부딪칠 때
        {
            Destroy(gameObject, 3);
        }
        else if (!isMelee && tag == "Wall")//벽에 부딪칠 때(솔리드 콜라이더)
        {
            Destroy(gameObject);
        }
        
    }

    void OnTriggerEnter(Collider other)//원거라 무기일 경루
    {
        if (!isMelee && other.gameObject.tag == "Wall")
        {
            Destroy(gameObject);
        }
    }
}
