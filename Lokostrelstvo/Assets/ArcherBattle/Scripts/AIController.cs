using System.Collections.Generic;
using UnityEngine;

namespace ArcherBattle
{
    /// <summary>
    /// Computer archer: wanders, dodges incoming arrows, and shoots with a ballistic solution
    /// plus a difficulty dependent aim error so it misses sometimes.
    /// </summary>
    public class AIController : MonoBehaviour
    {
        Archer me, target;
        AISettings s;

        float nextShotTime;
        float wanderTarget;
        float nextWanderTime;
        float desiredCharge;
        float plannedAngle;
        bool aimAtHead;
        float aimOffset;     // this shot's aim error in degrees
        float heightJitter;

        // dodging
        readonly HashSet<Arrow> judged = new HashSet<Arrow>();
        Arrow threat;
        float threatSeenAt;
        bool willDodge;
        int dodgeDir;
        bool dodgeByJump;
        float threatTime;

        public void Init(Archer archer, Archer opponent, Difficulty difficulty)
        {
            me = archer;
            target = opponent;
            SetDifficulty(difficulty);
        }

        public void SetDifficulty(Difficulty d)
        {
            s = AISettings.For(d);
            if (me != null) me.moveSpeed = s.moveSpeed;
        }

        public void ResetRound()
        {
            judged.Clear();
            threat = null;
            nextShotTime = Time.time + Random.Range(0.6f, 1.2f) + s.minShotDelay * 0.5f;
            nextWanderTime = Time.time + Random.Range(0.3f, 1f);
            wanderTarget = me.transform.position.x;
        }

        void Update()
        {
            if (me == null || target == null) return;
            var gm = GameManager.Instance;
            bool active = gm != null && gm.State == GameState.Playing && me.controlsEnabled && me.Alive;
            if (!active)
            {
                me.SetMove(0f);
                if (me.IsDrawing) me.CancelDraw();
                return;
            }

            float move = Dodge(out bool dodging);
            if (!dodging) move = Wander();
            me.SetMove(move);

            // occasional random hop (more often on higher difficulty)
            if (!dodging && me.Grounded && Random.value < s.jumpsPerSecond * Time.deltaTime) me.Jump();

            Shoot();
        }

        // ------------------------------------------------------------------ movement

        float Wander()
        {
            float x = me.transform.position.x;
            if (Time.time >= nextWanderTime)
            {
                float range = Mathf.Lerp(2f, 6f, s.restlessness);
                wanderTarget = Mathf.Clamp(x + Random.Range(-range, range), me.minX + 0.5f, me.maxX - 0.5f);
                nextWanderTime = Time.time + Random.Range(1.2f, 3.2f) * Mathf.Lerp(1.6f, 0.6f, s.restlessness);
            }
            float d = wanderTarget - x;
            if (Mathf.Abs(d) < 0.15f) return 0f;
            return Mathf.Clamp(d * 1.5f, -1f, 1f);
        }

        /// <summary>Predicts whether an incoming arrow will hit us and steps out of the way.</summary>
        float Dodge(out bool dodging)
        {
            dodging = false;
            float myX = me.transform.position.x;

            if (threat == null || !threat.Flying || !WillHit(threat, myX, 0.55f, out _))
            {
                threat = null;
                float bestTime = float.MaxValue;
                foreach (var arrow in Arrow.All)
                {
                    if (arrow == null || !arrow.Flying || arrow.Shooter == me) continue;
                    if (!WillHit(arrow, myX, 0.55f, out float tHit) || tHit >= bestTime) continue;
                    threatTime = tHit;
                    bestTime = tHit;
                    threat = arrow;
                }
                if (threat != null && judged.Add(threat))
                {
                    threatSeenAt = Time.time;
                    willDodge = Random.value < s.dodgeChance;
                    dodgeByJump = HeightAt(threat, myX) < 1.1f && Random.value < 0.65f;
                    // dodge towards the side with more room, with a bias away from the arrow's travel direction
                    float roomLeft = myX - me.minX, roomRight = me.maxX - myX;
                    dodgeDir = roomLeft > roomRight ? -1 : 1;
                    if (Mathf.Min(roomLeft, roomRight) > 2f) dodgeDir = Random.value < 0.5f ? -1 : 1;
                }
            }

            if (threat == null || !willDodge) return 0f;
            if (Time.time - threatSeenAt < s.reactionTime) return 0f;

            // jump over low/fast arrows when the timing is right, otherwise run
            if (dodgeByJump && me.Grounded)
            {
                WillHit(threat, myX, 0f, out threatTime);
                if (threatTime < 0.45f && threatTime > 0.12f) me.Jump();
                if (threatTime >= 0.12f) return 0f;
            }

            // stopping the draw makes us faster
            if (me.IsDrawing && me.Charge < 0.6f) me.CancelDraw();
            if ((dodgeDir < 0 && myX <= me.minX + 0.2f) || (dodgeDir > 0 && myX >= me.maxX - 0.2f)) dodgeDir = -dodgeDir;
            dodging = true;
            wanderTarget = myX + dodgeDir * 1.5f;
            return dodgeDir;
        }

        /// <summary>Does this arrow pass through a column around x at body height? Returns time until it does.</summary>
        static bool WillHit(Arrow arrow, float x, float halfWidth, out float time)
        {
            time = 0f;
            Vector2 p = arrow.Tip, v = arrow.Velocity;
            if (Mathf.Abs(v.x) < 0.01f) return false;
            for (int side = -1; side <= 1; side++)
            {
                float t = (x + side * halfWidth - p.x) / v.x;
                if (t < 0f || t > 3f) continue;
                float y = p.y + v.y * t - 0.5f * Arrow.Gravity * t * t;
                if (y > -0.1f && y < 2.45f) { time = t; return true; }
            }
            return false;
        }

        static float HeightAt(Arrow arrow, float x)
        {
            Vector2 p = arrow.Tip, v = arrow.Velocity;
            if (Mathf.Abs(v.x) < 0.01f) return p.y;
            float t = Mathf.Max(0f, (x - p.x) / v.x);
            return p.y + v.y * t - 0.5f * Arrow.Gravity * t * t;
        }

        // ------------------------------------------------------------------ shooting

        void Shoot()
        {
            if (!me.IsDrawing)
            {
                if (Time.time < nextShotTime || !me.CanShoot) return;
                aimAtHead = Random.value < s.headshotChance;
                heightJitter = Random.Range(-0.2f, 0.2f);
                desiredCharge = Random.Range(s.minCharge, 1f);
                if (!Plan(out plannedAngle))
                {
                    desiredCharge = 1f;
                    if (!Plan(out plannedAngle)) plannedAngle = 45f;
                }
                // the aim error that makes the AI miss sometimes
                aimOffset = Gaussian() * s.aimError;
                plannedAngle = Mathf.Clamp(plannedAngle + aimOffset, Archer.MinAimAngle, Archer.MaxAimAngle);
                me.BeginDraw();
                return;
            }

            // while drawing keep tracking the player (with a little lag), then let go
            if (Plan(out float fresh))
                plannedAngle = Mathf.Clamp(fresh + aimOffset, Archer.MinAimAngle, Archer.MaxAimAngle);
            me.SetAim(Mathf.MoveTowards(me.AimAngle, plannedAngle, 160f * Time.deltaTime));

            if (me.Charge >= desiredCharge && Mathf.Abs(me.AimAngle - plannedAngle) < 1.5f)
            {
                me.ReleaseDraw();
                nextShotTime = Time.time + Random.Range(s.minShotDelay, s.maxShotDelay);
            }
        }

        /// <summary>Low-arc angle that lands the arrow on the (predicted) target at the desired draw strength.</summary>
        bool Plan(out float angle)
        {
            float speed = Archer.SpeedForCharge(desiredCharge);
            Vector2 aimPoint = aimAtHead ? target.HeadWorld : target.ChestWorld + Vector2.up * heightJitter;
            angle = me.AimAngle;

            // iterate: flight time depends on angle, lead depends on flight time
            float a = 30f;
            bool ok = false;
            for (int i = 0; i < 3; i++)
            {
                Vector2 from = me.LaunchPoint(a);
                float dx = Mathf.Abs(aimPoint.x - from.x);
                float flight = dx / Mathf.Max(1f, speed * Mathf.Cos(a * Mathf.Deg2Rad));
                Vector2 predicted = aimPoint + Vector2.right * target.VelocityX * flight * s.leadFactor;
                dx = Mathf.Abs(predicted.x - from.x);
                float dy = predicted.y - from.y;
                if (!Solve(speed, dx, dy, out float sol)) { ok = false; break; }
                a = sol;
                ok = true;
            }
            if (ok) angle = Mathf.Clamp(a, Archer.MinAimAngle, Archer.MaxAimAngle);
            return ok;
        }

        static bool Solve(float v, float dx, float dy, out float angleDeg)
        {
            float g = Arrow.Gravity;
            float v2 = v * v;
            float disc = v2 * v2 - g * (g * dx * dx + 2f * dy * v2);
            angleDeg = 0f;
            if (disc < 0f || dx < 0.01f) return false;
            angleDeg = Mathf.Atan2(v2 - Mathf.Sqrt(disc), g * dx) * Mathf.Rad2Deg;
            return true;
        }

        static float Gaussian()
        {
            float u1 = Mathf.Max(1e-6f, Random.value), u2 = Random.value;
            return Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.PI * u2);
        }
    }
}
