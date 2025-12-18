using System.Collections.Generic;
using UnityEngine;

public class Balls : MonoBehaviour
{
    [Header("레퍼런스")]
    public Play play;
    public Ball ball;

    [Header("공 풀 설정")]
    public int ball_count = 10;
    public Material white_ball;
    public Material orange_striped_ball;

    [Header("충돌 표면들")]
    public List<Bouncer> bouncers;

    private List<Ball> balls = new List<Ball>();
    private int cur_ball = 0;

    public float maxSubstep = 1f / 180f;
    public int maxSubsteps = 8;

    void Awake()
    {
        if (balls == null)
            balls = new List<Ball>();

        if (balls.Count == 0 && ball != null)
        {
            balls.Add(ball);
        }

        if (bouncers == null || bouncers.Count == 0)
        {
            Bouncer[] found = FindObjectsOfType<Bouncer>();
            bouncers = new List<Bouncer>(found);
        }
    }

    public Ball new_ball()
    {
        if (balls.Count < ball_count)
        {
            Ball nb = Instantiate(ball, transform);
            nb.gameObject.SetActive(true);
            nb.freeze = false;
            balls.Add(nb);
            cur_ball = balls.Count - 1;
            return nb;
        }

        for (int i = 0; i < balls.Count; ++i)
        {
            cur_ball = (cur_ball + 1) % balls.Count;
            Ball nb = balls[cur_ball];

            if (play == null || !play.ball_held(nb))
            {
                nb.gameObject.SetActive(true);
                nb.freeze = false;
                return nb;
            }
        }

        return null;
    }

    public void move_balls(float delta_t)
    {
        if (delta_t <= 0f) return;

        int steps = Mathf.CeilToInt(delta_t / maxSubstep);
        steps = Mathf.Clamp(steps, 1, maxSubsteps);
        float h = delta_t / steps;

        for (int s = 0; s < steps; s++)
        {
            foreach (Ball b in balls)
            {
                if (b == null || !b.gameObject.activeInHierarchy) continue;

                BallState bs1 = b.motion;
                BallState bs2 = b.move_ball(h);

                if (bs2 == null) continue;

                bs2 = compute_rebounds(bs1, bs2, b);
                b.set_motion(bs2);
            }
        }
    }

    public BallState compute_rebounds(BallState bs1, BallState bs2, Ball b)
    {
        if (bouncers == null || bouncers.Count == 0)
            return bs2;

        for (int r = 0; r < 2; ++r)
        {
            Rebound first_rb = null;

            foreach (Bouncer bn in bouncers)
            {
                if (bn == null) continue;

                Rebound rb = bn.check_for_bounce(bs1, bs2, b.radius, b.inertia_coef);
                if (rb != null)
                {
                    if (first_rb == null || rb.contact.time < first_rb.contact.time)
                        first_rb = rb;
                }
            }

            if (first_rb == null)
                break;

            first_rb.set_ball(b);

            if (play != null)
                play.ball_bounced(b, first_rb.bouncer);

            bs1 = first_rb.contact;
            bs2 = first_rb.final;
        }

        return bs2;
    }

    public void set_ball_coloring(string name)
    {
        Material m = (name == "orange striped") ? orange_striped_ball : white_ball;

        if (balls == null) return;

        foreach (Ball b in balls)
        {
            if (b == null) continue;
            MeshRenderer mr = b.GetComponent<MeshRenderer>();
            if (mr != null)
                mr.material = m;
        }
    }
}
