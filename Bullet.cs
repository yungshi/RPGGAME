using UnityEngine;

public class Bullet : MonoBehaviour
{
    public int damage;//공격력
    public bool isMelee;//원거리
    public bool isRock;//근접
    void OnCollisionEnter(Collision collision)
    {
        if(!isRock &&collision.gameObject.tag == "Floor")//땅에 부딧칠때
        {
            Destroy(gameObject, 3);
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
