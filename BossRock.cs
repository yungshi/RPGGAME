using UnityEngine;

public class BossRock : Bullet//Bullet 클래스 기반
{
    Rigidbody rigid;
    float angularPower = 2;//회전력
    float scaleValue = 0.1f;//크기
    bool isShoot;//False 기본

    void Awake()//최초 실행동작
    {
        rigid = GetComponent<Rigidbody>();
        StartCoroutine(GainPowerTimer());
        StartCoroutine(GainPower());
    }

    IEnumerator GainPowerTimer()//2.2 대기후 발사 루프
    {
        yield return new WaitForSeconds(2.2f);
        isShoot = true;
    }

    IEnumerator GainPower()//크기를 커지게함
    {
        while (!isShoot)
        {
            angularPower += 0.02f;
            scaleValue += 0.005f;
            transform.localScale = Vector3.one * scaleValue;
            rigid.AddTorque(transform.right * angularPower, ForceMode.Acceleration);
            yield return null; 
        }
    }
    
}
