using UnityEngine;

public class Missile : MonoBehaviour
{
    
    void Update()//미사일 백터 업데이트
    {
        transform.Rotate(Vector3.right * 30 * Time.deltaTime);
    }

        
    
}
