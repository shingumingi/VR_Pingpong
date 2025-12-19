using UnityEngine;

public class BallFeeder : MonoBehaviour
{
    public Balls balls;

    [Header("속도 설정")]
    public float launchInterval = 2f;
    public float minLaunchSpeed = 3f;
    public float maxLaunchSpeed = 6f;

    [Header("방향 랜덤(도 단위)")]
    // 좌우(요) 랜덤 각도: -maxYawAngle ~ +maxYawAngle
    public float maxYawAngle = 5f;
    // 위/아래(피치) 랜덤 각도: 0 ~ maxPitchAngle (원하면 -값도 허용 가능)
    public float maxPitchAngle = 3f;

    float timer = 0f;

    void Start()
    {
        ApplyDifficulty();
    }

    void ApplyDifficulty()
    {
        switch (DifficultySettings.Current)
        {
            case DifficultyLevel.Easy:
                launchInterval = 2.5f;
                minLaunchSpeed = 4f;
                maxLaunchSpeed = 4.5f;
                maxPitchAngle = 3f;
                maxYawAngle = 5f;
                break;

            case DifficultyLevel.Normal:
                launchInterval = 2f;
                minLaunchSpeed = 4f;
                maxLaunchSpeed = 5f;
                maxPitchAngle = 3f;
                maxYawAngle = 5f;
                break;

            case DifficultyLevel.Hard:
                launchInterval = 1f;
                minLaunchSpeed = 4f;
                maxLaunchSpeed = 7f;
                maxPitchAngle = 3f;
                maxYawAngle = 5f;
                break;
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= launchInterval)
        {
            timer -= launchInterval;
            LaunchBall();
        }
    }

    void LaunchBall()
    {
        if (balls == null) return;

        Ball ball = balls.new_ball();
        if (ball == null) return;

        Vector3 pos = transform.position;

        Vector3 dir = transform.forward;
        float yaw = Random.Range(-maxYawAngle, maxYawAngle);      // 좌우 각도
        float pitch = Random.Range(0f, maxPitchAngle);            // 위로 띄우는 각도
        dir = (Quaternion.Euler(pitch, yaw, 0f) * dir).normalized;

        float speed = Random.Range(minLaunchSpeed, maxLaunchSpeed);
        Vector3 vel = dir * speed;

        // 스핀 완전 제거
        Vector3 w = Vector3.zero;

        ball.motion = null;
        ball.transform.position = pos;
        ball.transform.rotation = Quaternion.identity;

        BallState state = new BallState(ball, 0f, pos, vel, w);
        ball.set_motion(state);
    }
}
