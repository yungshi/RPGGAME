using UnityEngine;

public class Weapon : MonoBehaviour
{
    public enum Type { Melee, Range };//근접공격과 범위
    public Type type;/종류
    public int damage;/데미지
    public float rate;//범위
    public int maxAmmo;//총알최대수
    public int curAmmo;//현재 총알수

    public BoxCollider meleeArea;//콜리이더
    public TrailRenderer trailEffect;//잔상효과
    public Transform bulletPos;//총알 위치
    public GameObject bullet;//총알 오브젝트
    public Transform bulletCasePos;//탄피 위치
    public GameObject bulletCase;//탄피 오브젝트

    public void Use()
    {
        if(type == Type.Melee)//근접공격
        {
            StopCoroutine("Swing");
            StartCoroutine("Swing");
        }
        else if (type == Type.Range && curAmmo > 0)//총알이 있을때 총알 감소후 발사
        {
            curAmmo--;
            StartCoroutine("shot");
        }
    }

    IEnumerator Swing()//근접공격
    {
        yield return new WaitForSeconds(0.1f);//0.1초 기다림
        meleeArea.enabled = true;//근접공격활성화
        trailEffect.enabled = true;//잔상효과

        yield return new WaitForSeconds(0.3f);//0.3초 대기
        meleeArea.enabled = false;//근접공격 중지

        yield return new WaitForSeconds(0.3f);//0.3초 대기
        trailEffect.enabled = false;//잔상효과 종료
    }

    IEnumerator Shot()//원거리 공격
    {
        //총알발사
        GameObject intantBulllet = Instantiate(bullet, bulletPos.position, bulletPos.rotation);//총알 생성
        Rigidbody bulletRigid = intantBulllet.GetComponent<Rigidbody>();//물리 컴포넌트 부여
        bulletRigid.linearVelocity = bulletPos.forward * 50;//정면 방향 총알 발사

        yield return null;//대기
        //탄피배출
        GameObject intantCase = Instantiate(bulletCase, bulletCasePos.position, bulletCasePos.rotation);//탄피의 백터 계산
        Rigidbody caseRigid = intantCase.GetComponent<Rigidbody>();
        Vector3 caseVec = bulletCasePos.forward * Random.Range(-3, -2) + Vector3.up * Random.Range(2, 3);
        caseRigid.AddForce(caseVec, ForceMode.Impulse);//충갹량을 주어 탄피를 밀어냄
        caseRigid.AddTorque(Vector3.up * 10, ForceMode.Impulse);//탄피 회전력 부여
    }
}
