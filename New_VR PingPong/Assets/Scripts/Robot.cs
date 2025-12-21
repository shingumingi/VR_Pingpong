using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Robot : MonoBehaviour
{
    public static Robot i;

    public Paddle paddle;              // Robot's paddle
    public Paddle player_paddle;
    public Play play;

    public Serves serves;
    Serve[] active_serves;
    Serve cur_serve;
    float wait_to_serve = 0f;
    int return_count = 0;        // Number of returns made in rally.
    int serve_repeat_count = 1;
    int repeated_serve = 0;
    public bool auto_serve = false;
    public float strokeSpeedScale = 1f;

    public float swing_duration = 0.3f;       // seconds
    public float forehand_coverage = 0.75f;   // Fraction of table covered with forehand
    float backswing_pause_time = 0.0f;        // Time from end of backswing to start of stroke.
    public float recovery_time = 0.5f;        // Time from end of swing to reaching recovery paddle position.
    Vector3 recovery_position = new Vector3(0f, .85f, 2.5f);
    float max_grazing_slope = 1f;

    bool limit_acceleration = true;
    float max_backswing_acceleration = 40f;
    float max_stroke_acceleration = 100f;

    public BallTracking ball_tracking;
    public Table table;  // Use table dimensions

    [System.NonSerialized]  // Prevent changing these values in Unity inspector.
    public string[] patterns = { "Forehand", "Backhand", "Random forehand", "Random", "Diagonal", "Down line", "Elbow", "No return" };
    [System.NonSerialized]
    public int pattern = 0;

    [System.NonSerialized]
    public string[] speeds = { "Slow", "Medium", "Fast" };
    [System.NonSerialized]
    public int speed = 1;

    [System.NonSerialized]
    public string[] movement_speeds = { "Slow", "Medium", "Fast", "Unlimited" };
    [System.NonSerialized]
    public int movement_speed = 1;

    public bool drop = true;    // Allow robot drop shots.
    public bool smash = true;
    public bool loop = true;
    public bool chop = true;
    public bool block = true;

    float time_now = 0;
    List<PaddleMotion> motions = new List<PaddleMotion>();

    private void Awake()
    {
        i = this;
    }

    public Stroke return_ball(BallTrack ball_track)
    {
        return_count += 1;

        if (patterns[pattern] == "No return")
            return null;

        Stroke s = return_stroke(ball_track, return_count);
        set_stroke(s, ball_track.initial.time);
        return s;
    }

    Stroke return_stroke(BallTrack bt, int num_hit)
    {
        // Choose shot: incoming hit point + target + speed
        BallState incoming_ball;
        Vector3 table_target;
        float outSpeed;
        choose_shot(bt, num_hit, out incoming_ball, out table_target, out outSpeed);

        // Compute velocity (NO-SPIN)
        Vector3 bv, bav;
        aim_ball_noSpin(incoming_ball, table_target, outSpeed, out bv, out bav);

        // Compute paddle motion to achieve outgoing ball velocity (NO-SPIN)
        Vector3 paddle_velocity, paddle_normal;
        bool backhand;
        paddle_motion(incoming_ball, bv, bav, out paddle_velocity, out paddle_normal, out backhand);

        // Compute paddle stroke.
        Stroke s = paddle_stroke(incoming_ball, paddle_velocity, paddle_normal, backhand);

        // If stroke accel is too high, just lengthen swing a bit (NO-SPIN version)
        s = limit_stroke_acceleration_noSpin(s);

        return s;
    }

    void aim_ball_noSpin(BallState incoming_ball, Vector3 table_target, float outSpeed,
                         out Vector3 bv, out Vector3 bav)
    {
        // Compute table plane ball motion and vertical ball motion separately.
        Vector3 line = table_target - incoming_ball.position;
        Vector3 plane_move = new Vector3(line.x, 0f, line.z);
        float range = plane_move.magnitude;

        float vhorz = outSpeed;
        Ball b = incoming_ball.ball;

        // Spin parameter = 0
        float vy = b.vertical_speed_for_range(vhorz, 0f, -line.y, range);
        bv = vhorz * plane_move.normalized + vy * Vector3.up;

        // NO-SPIN
        bav = Vector3.zero;
    }

    void choose_shot(BallTrack bt, int num_hit,
                     out BallState incoming_ball, out Vector3 table_target,
                     out float outSpeed)
    {
        BallState tbs = bt.top_of_bounce();

        // 1) Smash at top of bounce
        if (smash)
        {
            if (tbs.position.y >= table.height + 0.40f &&
                tbs.position.z <= 0.5f * table.length + 0.50f)
            {
                incoming_ball = tbs;
                table_target = placement(tbs);

                // request short hit depth
                float hl = 0.5f * table.length;
                table_target.z = -0.5f * hl;

                float s = speed_level();
                outSpeed = 7f + 8f * s;
                Debug.Log("smash");
                return;
            }
        }

        // 2) Loop/Chop decision logic kept, but NO-SPIN so only speed differs
        BallState thbs = bt.crossing_point(table.height, Vector3.up);
        if (loop || chop)
        {
            if (thbs != null &&
                thbs.position.z >= 0.6f * table.length &&
                thbs.position.z <= paddle.position.z)
            {
                incoming_ball = thbs;
                table_target = placement(thbs);
                float s = speed_level();

                // loop/chop affect speed only now
                bool loop_it = (loop && chop ? Random.value > 0.5f : loop);
                outSpeed = loop_it ? (6f + 6f * s) : (4f + 4f * s);
                return;
            }
        }

        // 3) Block case (NO-SPIN): keep "fast incoming -> faster return"
        bool top_behind = (tbs.position.z > paddle.position.z
                        && tbs.position.z > 0.5f * table.length);
        BallState cbs = bt.crossing_point(paddle.position.z, Vector3.forward);

        if (block)
        {
            if (top_behind && cbs != null && cbs.velocity.magnitude > 6f)
            {
                incoming_ball = cbs;
                table_target = placement(cbs);

                float f = Mathf.Sqrt(0.7f);
                float bspeed = cbs.velocity.magnitude;

                // Don't move paddle backwards too much
                outSpeed = Mathf.Max(f * bspeed, 8.0f);
                return;
            }
        }

        // 4) Drop shot (NO-SPIN): speed only
        if (drop)
        {
            if (tbs.position.z <= 1f &&
                tbs.position.y <= table.height + 0.30f)
            {
                incoming_ball = tbs;
                table_target = placement(tbs);
                table_target.z = Random.Range(-0.80f, -0.50f);   // Depth

                float s = speed_level();
                outSpeed = 3f + 2f * s;
                return;
            }
        }

        // 5) Default hit
        BallState bs = (top_behind && cbs != null ? cbs : tbs);
        incoming_ball = bs;
        table_target = placement(bs);

        float distance = (table_target - bs.position).magnitude;

        // NO-SPIN: ignore topspin -> speed based on distance only
        outSpeed = speed_for_distance(distance);
    }

    void paddle_motion(BallState incoming_ball,
                       Vector3 outgoing_ball_velocity,
                       Vector3 outgoing_ball_angular_velocity,
                       out Vector3 paddle_velocity,
                       out Vector3 paddle_normal,
                       out bool backhand)
    {
        BallState bs = incoming_ball;

        Bouncer bn = paddle.forehand_rubber();
        Ball b = incoming_ball.ball;

        bn.wall_motion(bs.velocity, outgoing_ball_velocity,
                       bs.angular_velocity, outgoing_ball_angular_velocity,
                       b.radius, b.inertia_coef, max_grazing_slope,
                       out paddle_velocity, out paddle_normal);

        // Decide which side of paddle to use.
        backhand = (bs.position.x > (forehand_coverage - 0.5f) * table.width);
    }

    Stroke paddle_stroke(BallState incoming_ball,
                         Vector3 paddle_velocity, Vector3 paddle_normal,
                         bool backhand)
    {
        Vector3 pv = paddle_velocity * (1f / strokeSpeedScale);
        Vector3 pn = paddle_normal;
        BallState bs = incoming_ball;
        Ball b = incoming_ball.ball;

        // Get center of paddle blade by offset from ball position.
        Vector3 paddle_pos = bs.position - pn * (b.radius + 0.5f * paddle.thickness);

        // Shorten swing if needed to avoid hitting table.
        float pre_swing_duration = 0.5f * swing_duration * strokeSpeedScale;
        float post_swing_duration = 0.5f * swing_duration * strokeSpeedScale;
        float d;
        float ave_speed = 0.5f * pv.magnitude;

        if (table_strike_distance(paddle_pos, -pv, pn, out d)
            && ave_speed * pre_swing_duration > d)
            pre_swing_duration = d / ave_speed;

        if (table_strike_distance(paddle_pos, pv, pn, out d)
            && ave_speed * post_swing_duration > d)
            post_swing_duration = d / ave_speed;

        Stroke s = new Stroke(bs.time, paddle_pos, pn, pv,
                              pre_swing_duration, post_swing_duration,
                              backhand);
        return s;
    }
    Stroke limit_stroke_acceleration_noSpin(Stroke s)
    {
        if (!limit_acceleration) return s;

        float a = s.pre_swing_acceleration();
        if (a <= max_stroke_acceleration) return s;

        // Accel roughly inversely proportional to duration -> use sqrt ratio like original style
        float f = Mathf.Sqrt(a / max_stroke_acceleration);
        float newDur = s.pre_swing_duration * f;
        s.change_pre_swing_duration(newDur);
        return s;
    }

    bool table_strike_distance(Vector3 hit_position, Vector3 paddle_velocity, Vector3 paddle_normal,
                               out float d)
    {
        d = 0;
        if (paddle_velocity.y >= 0) return false;

        float ph = Mathf.Sqrt(1.0f - paddle_normal.y * paddle_normal.y) * 0.5f * paddle.width;
        float above_table = hit_position.y - ph - table.height;
        if (above_table <= 0) return false;

        float t = above_table / -paddle_velocity.y;
        Vector3 tpos = hit_position + t * paddle_velocity;

        bool over_table = (Mathf.Abs(tpos.x) <= 0.5f * table.width + paddle.width
                        && Mathf.Abs(tpos.z) <= 0.5f * table.length);

        if (over_table)
        {
            d = t * paddle_velocity.magnitude;
            return true;
        }
        return false;
    }

    public void new_rally()
    {
        return_count = 0;
    }

    Vector3 placement(BallState bs)
    {
        float x = 0, z = -1.1f;
        string p = patterns[pattern];

        if (p == "Forehand") x = 0.5f;
        else if (p == "Backhand") x = -0.5f;
        else if (p == "Random forehand") x = Random.Range(0f, 0.6f);
        else if (p == "Random") x = Random.Range(-0.6f, 0.6f);
        else if (p == "Diagonal") x = (bs.position.x >= 0 ? -0.5f : 0.5f);
        else if (p == "Down line") x = (bs.position.x >= 0 ? 0.5f : -0.5f);
        else if (p == "Elbow")
        {
            Vector3 b = bs.position;
            Vector3 elbow_offset = new Vector3(0, -0.45f, 0);
            Vector3 e = player_paddle.transform.TransformPoint(elbow_offset);
            float f = (z - e.z) / (b.z - e.z);
            x = (1 - f) * e.x + f * b.x;
        }

        float y = table.height + bs.ball.radius;
        return new Vector3(x, y, z);
    }

    float speed_for_distance(float distance)
    {
        // NO-SPIN: ignore topspin term
        float s = speed_level();
        return (3f + s) * Mathf.Sqrt(distance);
    }

    float speed_level()
    {
        if (speeds[speed] == "Slow") return 0f;
        if (speeds[speed] == "Medium") return 0.5f;
        if (speeds[speed] == "Fast") return 1.0f;
        return 0f;
    }

    public void move_paddle(float delta_t)
    {
        if (wait_to_serve > 0f)
        {
            wait_to_serve -= delta_t;
            if (wait_to_serve <= 0f)
                serve_toss();
            else
            {
                Serve s = current_serve();
                Ball ball = play.ball_in_play;
                s.toss.throw_motion(ball, -wait_to_serve);
            }
        }

        time_now += delta_t;

        if (motions.Count == 0)
            return;

        PaddleMotion m = motions[0];
        bool done = m.move(time_now, paddle);

        if (done)
        {
            motions.RemoveAt(0);

            if (motions.Count > 0)
            {
                motions[0].start_time = time_now;
            }
            return;
        }
    }

    public void set_stroke(Stroke s, float time_now)
    {
        this.time_now = time_now;

        motions.Clear();

        Transform t = paddle.transform;
        PaddleMotion backswing = backswing_motion(time_now, t.position, t.rotation,
            s.start_time, s.start_position, s.start_rotation);
        limit_backswing_acceleration(backswing, s);
        motions.Add(backswing);

        motions.Add(s);

        PaddleMotion recovery = recovery_motion(s.end_time, s.end_position, s.end_rotation);
        motions.Add(recovery);
    }

    public void set_motion(PaddleMotion motion, float time_now)
    {
        this.time_now = time_now;
        motions.Clear();
        motions.Add(motion);
    }

    PaddleMotion backswing_motion(float start_time, Vector3 start_position, Quaternion start_rotation,
        float end_time, Vector3 end_position, Quaternion end_rotation)
    {
        float bs_end = Mathf.Max(end_time - backswing_pause_time, start_time);
        PaddleMotion backswing = new PaddleMotion();
        backswing.set_motion(start_time, start_position, start_rotation,
                            bs_end, end_position, end_rotation);
        return backswing;
    }

    bool limit_backswing_acceleration(PaddleMotion backswing, Stroke s)
    {
        if (!limit_acceleration)
            return false;

        float ba = backswing.acceleration();
        if (ba <= max_backswing_acceleration)
            return false;

        float f = Mathf.Sqrt(ba / max_backswing_acceleration);
        float bs_start = backswing.start_time;
        float bs_end = bs_start + (backswing.end_time - bs_start) * f;
        backswing.set_motion(bs_start, backswing.start_position, backswing.start_rotation,
                             bs_end, s.start_position, s.start_rotation);

        if (bs_end > s.start_time)
        {
            float fs = Mathf.Sqrt(s.pre_swing_acceleration() / max_stroke_acceleration);
            float min_dur = fs * s.pre_swing_duration;
            float new_dur = s.pre_swing_duration - (bs_end - s.start_time);
            new_dur = Mathf.Max(new_dur, min_dur);
            s.change_pre_swing_duration(new_dur);

            backswing.set_motion(bs_start, backswing.start_position, backswing.start_rotation,
                                bs_end, s.start_position, s.start_rotation);

            if (bs_end > s.start_time)
            {
                float dt = bs_end - s.start_time;
                s.start_time += dt;
                s.end_time += dt;
            }
        }
        return true;
    }

    PaddleMotion recovery_motion(float time, Vector3 position, Quaternion rotation)
    {
        float r_end = time + recovery_time;
        Quaternion r_rot = (Quaternion.AngleAxis(-30, new Vector3(1f, 0f, 0f))
            * Quaternion.AngleAxis(20, new Vector3(0f, 1f, 0f))
            * Quaternion.AngleAxis(90, new Vector3(0f, 0f, 1f))
            * Quaternion.AngleAxis(-90, new Vector3(1f, 0f, 0f)));

        PaddleMotion recovery = new PaddleMotion();
        recovery.set_motion(time, position, rotation, r_end, recovery_position, r_rot);
        return recovery;
    }

    public void set_movement_speed()
    {
        string s = movement_speeds[movement_speed];
        if (s == "Slow")
        {
            limit_acceleration = true;
            max_backswing_acceleration = 20f;
            max_stroke_acceleration = 50f;
        }
        else if (s == "Medium")
        {
            limit_acceleration = true;
            max_backswing_acceleration = 40f;
            max_stroke_acceleration = 100f;
        }
        else if (s == "Fast")
        {
            limit_acceleration = true;
            max_backswing_acceleration = 60f;
            max_stroke_acceleration = 150f;
        }
        else if (s == "Unlimited")
        {
            limit_acceleration = false;
        }
    }

    public void serve()
    {
        new_rally();
        Serve s;
        if (repeated_serve >= serve_repeat_count || cur_serve == null)
            s = new_serve();
        else
        {
            s = cur_serve;
            repeated_serve += 1;
        }

        Ball ball = play.start_robot_serve();
        s.toss.throw_motion(ball, s.toss.throw_start_time());
        ball.freeze = true;

        PaddleMotion motion = s.setup_to_serve(paddle.transform);
        set_motion(motion, motion.start_time);

        wait_to_serve = 0.5f;
    }

    void serve_toss()
    {
        Ball ball = play.ball_in_play;
        ball.freeze = false;

        Serve s = current_serve();

        Stroke stroke = s.serve_ball(ball, play.ball_tracking, paddle);
        set_stroke(stroke, ball.motion.time);
    }

    public void use_these_serves(List<Serve> serves)
    {
        this.active_serves = serves.ToArray();
    }

    public void set_serve_repeat(int repeat)
    {
        this.serve_repeat_count = repeat;
    }

    public Serve new_serve()
    {
        if (active_serves == null)
            use_these_serves(serves.serves);

        int i = UnityEngine.Random.Range(0, active_serves.Length);
        this.cur_serve = active_serves[i];
        this.repeated_serve = 1;
        return cur_serve;
    }

    public Serve current_serve()
    {
        return cur_serve;
    }
}
