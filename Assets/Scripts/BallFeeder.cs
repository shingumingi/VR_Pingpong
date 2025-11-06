using UnityEngine;
using System.Collections;

public class BallFeeder : MonoBehaviour
{
    public Play play;
    public Transform muzzle;

    [Header("Fire Settings")]
    public float interval = 2f;
    public Vector3 targetOnTable = new Vector3(0, 0, -1.1f);
    public float horizontalSpeed = 6f;  // m/s (바닥 면 투영 속도)
    [Range(-8, 8)] public float topspin = 4f;     // m/s (r*omega 개념)
    [Range(-8, 8)] public float sidespin = 0f;    // m/s

    [Header("Options")]
    public bool aimUsingPhysics = true;
    public bool disableRobot = true;

    Coroutine routine;

    void OnEnable()
    {
        if (disableRobot && play != null)
        {
            play.robot.auto_serve = false;
        }
        routine = StartCoroutine(FeedLoop());
    }

    void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
    }

    IEnumerator FeedLoop()
    {
        // 모든 Awake/Start/LateUpdate가 한 번 돌도록 한 프레임 대기
        yield return null;

        // 여유 있게 초기 대기
        yield return new WaitForSeconds(0.05f);

        while (true)
        {
            FireOne();
            yield return new WaitForSeconds(interval);
        }
    }

    void FireOne()
    {
        if (play == null || play.balls == null) return;

        // 공 생성 + 씬 리스트에 등록
        Ball b = play.balls.new_ball();
        play.ball_in_play = b;
        b.freeze = false;

        // 발사 위치/자세
        Vector3 p0 = muzzle.position;
        Quaternion r0 = muzzle.rotation;

        // 속도/스핀 계산
        Vector3 v0;
        Vector3 w0;

        if (aimUsingPhysics)
        {
            // 테이블 목표점 맞추는 수평/수직 분해 조준
            Vector3 toTarget = targetOnTable - p0;
            Vector3 plane = new Vector3(toTarget.x, 0f, toTarget.z);
            float range = plane.magnitude;
            float vhorz = Mathf.Max(0.1f, horizontalSpeed);

            // Ball API 활용 (이미 프로젝트에 있음)
            float vy = b.vertical_speed_for_range(vhorz, topspin, -toTarget.y, range);
            v0 = vhorz * plane.normalized + vy * Vector3.up;

            // 스핀: 탑스핀은 (v x up) 방향, 사이드스핀은 +y
            Vector3 topAxis = Vector3.Cross(v0, Vector3.up).normalized;
            Vector3 spinLinear = -topspin * topAxis + sidespin * Vector3.up; // m/s
            w0 = spinLinear / b.radius; // 각속도(rad/s)
        }
        else
        {
            // 단순 총구 전방 발사
            v0 = muzzle.forward * horizontalSpeed + Vector3.up * 1.0f;
            Vector3 topAxis = Vector3.Cross(v0, Vector3.up).normalized;
            w0 = (-topspin * topAxis + sidespin * Vector3.up) / b.radius;
        }

        // 상태 적용
        b.set_motion(new BallState(b, 0f, p0, v0, w0));
        b.transform.rotation = r0;
    }
}
