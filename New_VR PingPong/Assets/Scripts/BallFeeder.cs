using UnityEngine;

public class BallFeeder : MonoBehaviour
{
    public Balls balls;

    [Header("속도 설정")]
    public float launchInterval = 2f;
    public float minLaunchSpeed = 3f;
    public float maxLaunchSpeed = 6f;

    [Header("방향 랜덤(도 단위)")]
    public float maxYawAngle = 5f;
    public float maxPitchAngle = 3f;

    [Header("스핀 랜덤")]
    public float maxTopBackSpin = 80f;
    public float maxSideSpin = 60f;

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
                maxTopBackSpin = 0f;
                maxSideSpin = 0f;
                maxPitchAngle = 3f;
                break;

            case DifficultyLevel.Normal:
                launchInterval = 2f;
                minLaunchSpeed = 4f;
                maxLaunchSpeed = 5f;
                maxTopBackSpin = 60f;
                maxSideSpin = 40f;
                maxPitchAngle = 3f;
                break;

            case DifficultyLevel.Hard:
                launchInterval = 1f;
                minLaunchSpeed = 4f;
                maxLaunchSpeed = 7f;
                maxTopBackSpin = 120f;
                maxSideSpin = 80f;
                maxPitchAngle = 3f;
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
        float yaw = Random.Range(-maxYawAngle, maxYawAngle);
        float pitch = Random.Range(0f, maxPitchAngle);
        dir = (Quaternion.Euler(pitch, yaw, 0f) * dir).normalized;

        float speed = Random.Range(minLaunchSpeed, maxLaunchSpeed);
        Vector3 vel = dir * speed;

        // 스핀 maxTopBackSpin / maxSideSpin 이 0이면 자동으로 0
        float topBackSpin = Random.Range(-maxTopBackSpin, maxTopBackSpin);
        float sideSpin = Random.Range(-maxSideSpin, maxSideSpin);

        Vector3 w = transform.right * topBackSpin + transform.up * sideSpin;

        ball.motion = null;
        ball.transform.position = pos;
        ball.transform.rotation = Quaternion.identity;

        BallState state = new BallState(ball, 0f, pos, vel, w);
        ball.set_motion(state);
    }
}
