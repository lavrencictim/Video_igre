using System.Collections.Generic;
using UnityEngine;

namespace ArcherBattle
{
    /// <summary>
    /// A procedurally drawn and animated archer. Controllers (player or AI) only call
    /// SetMove / SetAim / BeginDraw / ReleaseDraw; everything else lives here.
    /// </summary>
    public class Archer : MonoBehaviour
    {
        // --- tuning
        public const float MinArrowSpeed = 9f;
        public const float MaxArrowSpeed = 25f;
        public const float FullDrawTime = 0.85f;
        public const float ReloadTime = 0.35f;
        public const float MinAimAngle = -60f, MaxAimAngle = 85f;
        public const float JumpSpeed = 9f;
        public const float JumpGravity = 26f;

        static readonly Vector2 ShoulderLocal = new Vector2(0.04f, 1.45f);
        static readonly Vector2 HeadCenter = new Vector2(0f, 2.0f);
        const float HeadRadius = 0.33f;
        static readonly Vector2 BodyCenter = new Vector2(0f, 0.86f);
        static readonly Vector2 BodyHalf = new Vector2(0.3f, 0.86f);
        const float BowDistance = 0.62f;
        const float MaxPull = 0.44f;

        // --- state
        public string displayName;
        public float maxHp = 100f;
        public float hp = 100f;
        public int facing = 1;               // +1 = looks right, -1 = looks left
        public float moveSpeed = 5f;
        public float minX = -13f, maxX = -3f;
        public bool controlsEnabled;
        public HealthBar healthBar;

        public bool Alive => hp > 0f;
        public bool Grounded => transform.position.y <= 0.0001f && velY <= 0f;
        public float AimAngle { get; private set; } = 25f;   // degrees, in "facing space" (0 = straight ahead, + = up)
        public float Charge { get; private set; }
        public bool IsDrawing { get; private set; }
        public bool CanShoot => Alive && controlsEnabled && reloadTimer <= 0f;
        public float VelocityX => velX;
        public Transform Visual => visual;
        public Vector2 ShoulderWorld => (Vector2)transform.position + new Vector2(ShoulderLocal.x * facing, ShoulderLocal.y + bob);
        public Vector2 HeadWorld => (Vector2)transform.position + HeadCenter;
        public Vector2 ChestWorld => (Vector2)transform.position + new Vector2(0, 1.15f);

        float moveInput, velX, velY, reloadTimer, walkPhase, bob, flash, recoil, deathT, stepTimer, hitKick;
        AudioSource drawSound;

        Transform shadowT;
        Transform visual, legL, legR, torso, headT, aimPivot, backArm, bowT;
        SpriteRenderer nockedArrow, backHand;
        LineRenderer bowString;
        readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        readonly List<Color> baseColors = new List<Color>();

        // ------------------------------------------------------------------ construction

        public static Archer Create(string name, int facing, Color tunic, Color hood, Color trim)
        {
            var go = new GameObject(name);
            var a = go.AddComponent<Archer>();
            a.displayName = name;
            a.facing = facing;
            a.Build(tunic, hood, trim);
            return a;
        }

        SpriteRenderer Part(string n, Transform parent, Sprite s, Color c, int order, Vector2 pos, Vector2 size, float rot = 0f)
        {
            var sr = Art.Box(n, parent, s, c, order, pos, size, rot);
            renderers.Add(sr);
            baseColors.Add(c);
            return sr;
        }

        Transform Joint(string n, Vector2 pos)
        {
            var t = new GameObject(n).transform;
            t.SetParent(visual, false);
            t.localPosition = pos;
            return t;
        }

        void Build(Color tunic, Color hood, Color trim)
        {
            Color skin = new Color(0.96f, 0.78f, 0.62f);
            Color pants = new Color(0.3f, 0.24f, 0.2f);
            Color boots = new Color(0.22f, 0.14f, 0.09f);
            Color leather = new Color(0.45f, 0.28f, 0.14f);
            var limbTop = Art.Limb(new Vector2(0.5f, 1f));
            var limbLeft = Art.Limb(new Vector2(0f, 0.5f));
            var round = Art.RoundedRect(false);

            visual = new GameObject("Visual").transform;
            visual.SetParent(transform, false);
            visual.localScale = new Vector3(facing, 1, 1);

            // shadow on the ground
            var shadow = Art.Box("shadow", transform, Art.SoftCircle, new Color(0, 0, 0, 0.35f), 1, new Vector2(0, 0.02f), new Vector2(1.3f, 0.3f));
            shadow.name = "Shadow";
            shadowT = shadow.transform;

            // legs: an unscaled hip joint each, so the leg and boot rotate together
            legR = Joint("hipBack", new Vector2(-0.1f, 0.74f));
            Part("legBack", legR, limbTop, pants * 0.8f, 8, Vector2.zero, new Vector2(0.2f, 0.76f));
            Part("bootBack", legR, round, boots * 0.85f, 8, new Vector2(0.05f, -0.7f), new Vector2(0.3f, 0.14f));
            legL = Joint("hipFront", new Vector2(0.1f, 0.74f));
            Part("legFront", legL, limbTop, pants, 12, Vector2.zero, new Vector2(0.2f, 0.76f));
            Part("bootFront", legL, round, boots, 12, new Vector2(0.05f, -0.7f), new Vector2(0.3f, 0.14f));

            // quiver on the back
            Part("quiver", visual, round, leather, 9, new Vector2(-0.3f, 1.25f), new Vector2(0.2f, 0.7f), -18f);
            for (int i = 0; i < 3; i++)
                Part("fletch" + i, visual, Art.Triangle, new Color(0.9f, 0.2f, 0.15f), 8, new Vector2(-0.42f + i * 0.07f, 1.55f + i * 0.03f), new Vector2(0.09f, 0.22f), -18f);

            // torso
            torso = Part("torso", visual, round, tunic, 10, new Vector2(0f, 1.15f), new Vector2(0.62f, 0.92f)).transform;
            Part("tunicSkirt", visual, Art.Triangle, tunic * 0.9f, 10, new Vector2(0f, 0.62f), new Vector2(0.7f, 0.32f));
            Part("belt", visual, Art.Square(), leather, 11, new Vector2(0f, 0.82f), new Vector2(0.64f, 0.1f));
            Part("buckle", visual, Art.Square(), new Color(1f, 0.82f, 0.3f), 11, new Vector2(0.12f, 0.82f), new Vector2(0.09f, 0.09f));
            Part("emblem", visual, Art.Circle, trim, 11, new Vector2(0.05f, 1.2f), new Vector2(0.2f, 0.2f));
            Part("strap", visual, Art.Square(), leather * 0.9f, 11, new Vector2(-0.02f, 1.2f), new Vector2(0.08f, 0.95f), 35f);

            // head + hood
            headT = new GameObject("Head").transform;
            headT.SetParent(visual, false);
            headT.localPosition = HeadCenter;
            Part("hoodBack", headT, Art.Circle, hood * 0.85f, 13, new Vector2(-0.06f, 0.04f), new Vector2(0.72f, 0.72f));
            Part("hoodTail", headT, Art.Triangle, hood * 0.85f, 12, new Vector2(-0.3f, -0.05f), new Vector2(0.28f, 0.55f), 115f);
            Part("face", headT, Art.Circle, skin, 14, new Vector2(0.05f, -0.02f), new Vector2(0.56f, 0.56f));
            Part("hoodTop", headT, Art.Circle, hood, 15, new Vector2(-0.04f, 0.17f), new Vector2(0.66f, 0.42f));
            Part("eye", headT, Art.Circle, new Color(0.12f, 0.1f, 0.1f), 16, new Vector2(0.2f, 0.0f), new Vector2(0.08f, 0.1f));
            Part("brow", headT, Art.Square(), new Color(0.35f, 0.22f, 0.12f), 16, new Vector2(0.19f, 0.08f), new Vector2(0.13f, 0.035f), -8f);
            Part("feather", headT, Art.Triangle, trim, 12, new Vector2(-0.25f, 0.28f), new Vector2(0.1f, 0.42f), 60f);

            // aiming rig at the shoulder
            aimPivot = new GameObject("AimPivot").transform;
            aimPivot.SetParent(visual, false);
            aimPivot.localPosition = ShoulderLocal;

            backArm = Part("backArm", aimPivot, limbLeft, tunic * 0.8f, 9, new Vector2(-0.05f, 0.02f), new Vector2(0.4f, 0.14f)).transform;
            backHand = Part("backHand", aimPivot, Art.Circle, skin, 18, new Vector2(0.3f, 0f), new Vector2(0.14f, 0.14f));

            var bowSr = Part("bow", aimPivot, Art.Bow, Color.white, 17, new Vector2(BowDistance, 0f), Vector2.one);
            bowSr.transform.localScale = Vector3.one;
            bowT = bowSr.transform;

            Part("frontArm", aimPivot, limbLeft, tunic * 0.95f, 16, new Vector2(0f, 0f), new Vector2(BowDistance + 0.04f, 0.14f));
            Part("frontHand", aimPivot, Art.Circle, skin, 18, new Vector2(BowDistance, 0f), new Vector2(0.15f, 0.15f));

            nockedArrow = Part("nockedArrow", aimPivot, Art.Arrow, Color.white, 17, Vector2.zero, Vector2.one);
            nockedArrow.transform.localScale = Vector3.one;

            var stringGo = new GameObject("String");
            stringGo.transform.SetParent(aimPivot, false);
            bowString = stringGo.AddComponent<LineRenderer>();
            bowString.useWorldSpace = false;
            bowString.positionCount = 3;
            bowString.widthMultiplier = 0.025f;
            bowString.startColor = bowString.endColor = new Color(0.95f, 0.93f, 0.85f);
            bowString.sortingOrder = 17;
            bowString.numCapVertices = 0;
            Art.ApplyMaterial(bowString);

            // hit shapes (head circle + body box) are tested analytically by arrows, see Raycast()
            UpdateRig(0f);
        }

        // ------------------------------------------------------------------ controller API

        public void SetMove(float input) => moveInput = Mathf.Clamp(input, -1f, 1f);

        public void SetAim(float angle) => AimAngle = Mathf.Clamp(angle, MinAimAngle, MaxAimAngle);

        public void AimAtWorld(Vector2 world)
        {
            Vector2 d = world - ShoulderWorld;
            if (d.sqrMagnitude < 0.01f) return;
            SetAim(Mathf.Atan2(d.y, d.x * facing) * Mathf.Rad2Deg);
        }

        public Vector2 AimDirection
        {
            get
            {
                float r = AimAngle * Mathf.Deg2Rad;
                return new Vector2(Mathf.Cos(r) * facing, Mathf.Sin(r));
            }
        }

        public static float SpeedForCharge(float charge) => Mathf.Lerp(MinArrowSpeed, MaxArrowSpeed, Mathf.Sqrt(Mathf.Clamp01(charge)));

        public static float ChargeForSpeed(float speed)
        {
            float k = Mathf.Clamp01(Mathf.InverseLerp(MinArrowSpeed, MaxArrowSpeed, speed));
            return k * k;
        }

        /// <summary>Where the arrow tip starts for a given aim angle (used by AI prediction too).</summary>
        public Vector2 LaunchPoint(float angleDeg)
        {
            float r = angleDeg * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(r) * facing, Mathf.Sin(r));
            return ShoulderWorld + dir * (BowDistance + 0.25f);
        }

        public bool Jump()
        {
            if (!Alive || !controlsEnabled || !Grounded) return false;
            velY = JumpSpeed;
            Fx.Dust(new Vector2(transform.position.x, 0f), 1f);
            Sfx.Play(Sfx.Step, 0.6f, 0.1f, 0.7f);
            return true;
        }

        public bool BeginDraw()
        {
            if (IsDrawing || !CanShoot) return false;
            IsDrawing = true;
            Charge = 0f;
            drawSound = Sfx.Play(Sfx.Draw, 0.55f, 0.04f);
            return true;
        }

        public void CancelDraw()
        {
            IsDrawing = false;
            Charge = 0f;
            if (drawSound != null && drawSound.clip == Sfx.Draw) drawSound.Stop();
        }

        public void ReleaseDraw()
        {
            if (!IsDrawing) return;
            if (!Alive || !controlsEnabled) { CancelDraw(); return; }
            float speed = SpeedForCharge(Charge);
            Vector2 dir = AimDirection;
            Arrow.Spawn(this, LaunchPoint(AimAngle), dir * speed);
            if (drawSound != null && drawSound.clip == Sfx.Draw) drawSound.Stop();
            Sfx.Play(Sfx.Release, 0.6f + 0.4f * Charge, 0.08f, 1.1f - 0.2f * Charge);
            Fx.Burst(LaunchPoint(AimAngle) - dir * 0.6f, new Color(1, 1, 1, 0.45f), 4, 0.5f, 1.5f, 0.25f, 0.3f, 0f, 0f, 360f);
            IsDrawing = false;
            Charge = 0f;
            reloadTimer = ReloadTime;
            recoil = 1f;
        }

        // ------------------------------------------------------------------ damage

        /// <summary>Segment test against head (circle) and body (box). t is 0..1 along from->to.</summary>
        public bool Raycast(Vector2 from, Vector2 to, out float t, out bool head)
        {
            t = 2f;
            head = false;
            Vector2 pos = transform.position;
            if (SegmentCircle(from, to, pos + HeadCenter, HeadRadius, out float th)) { t = th; head = true; }
            if (SegmentBox(from, to, pos + BodyCenter, BodyHalf, out float tb) && tb < t) { t = tb; head = false; }
            return t <= 1f;
        }

        static bool SegmentCircle(Vector2 a, Vector2 b, Vector2 c, float r, out float t)
        {
            t = 2f;
            Vector2 d = b - a, f = a - c;
            float A = Vector2.Dot(d, d);
            if (A < 1e-8f) return false;
            float B = 2f * Vector2.Dot(f, d), C = Vector2.Dot(f, f) - r * r;
            float disc = B * B - 4f * A * C;
            if (disc < 0f) return false;
            disc = Mathf.Sqrt(disc);
            float t1 = (-B - disc) / (2f * A), t2 = (-B + disc) / (2f * A);
            if (t1 >= 0f && t1 <= 1f) { t = t1; return true; }
            if (t1 < 0f && t2 >= 0f) { t = 0f; return true; }   // started inside
            return false;
        }

        static bool SegmentBox(Vector2 a, Vector2 b, Vector2 c, Vector2 half, out float t)
        {
            t = 2f;
            Vector2 d = b - a;
            float tmin = 0f, tmax = 1f;
            for (int i = 0; i < 2; i++)
            {
                float o = i == 0 ? a.x : a.y, dir = i == 0 ? d.x : d.y;
                float lo = (i == 0 ? c.x - half.x : c.y - half.y), hi = (i == 0 ? c.x + half.x : c.y + half.y);
                if (Mathf.Abs(dir) < 1e-8f)
                {
                    if (o < lo || o > hi) return false;
                }
                else
                {
                    float t1 = (lo - o) / dir, t2 = (hi - o) / dir;
                    if (t1 > t2) { float tmp = t1; t1 = t2; t2 = tmp; }
                    tmin = Mathf.Max(tmin, t1);
                    tmax = Mathf.Min(tmax, t2);
                    if (tmin > tmax) return false;
                }
            }
            t = tmin;
            return true;
        }

        public void TakeHit(int damage, bool head, Vector2 point, Vector2 arrowDir, Archer attacker)
        {
            if (!Alive) return;
            var gm = GameManager.Instance;
            bool counts = gm == null || gm.State == GameState.Playing;
            if (counts) hp = Mathf.Max(0f, hp - damage);

            flash = 1f;
            hitKick = Mathf.Sign(arrowDir.x) * (head ? 1f : 0.6f);
            velX += arrowDir.x * (head ? 3f : 2f);

            if (head)
            {
                Fx.HeadHit(point, arrowDir);
                Sfx.Play(Sfx.HitHead, 1f, 0.05f);
            }
            else
            {
                Fx.BodyHit(point, arrowDir);
                Sfx.Play(Sfx.HitBody, 0.9f, 0.1f);
            }

            if (gm != null) gm.OnArcherHit(this, counts ? damage : 0, head, point);
            if (!Alive) Die();
        }

        void Die()
        {
            CancelDraw();
            controlsEnabled = false;
            deathT = 0.0001f;
            Fx.Burst((Vector2)transform.position + Vector2.up * 0.1f, new Color(0.75f, 0.62f, 0.45f, 0.7f), 14, 1f, 3.5f, 0.5f, 0.8f, -0.5f, 90f, 160f);
            var gm = GameManager.Instance;
            if (gm != null) gm.OnArcherDied(this);
        }

        /// <summary>Put the archer back on its feet for a new round.</summary>
        public void ResetForRound(float x)
        {
            hp = maxHp;
            velX = 0f;
            velY = 0f;
            moveInput = 0f;
            reloadTimer = 0f;
            deathT = 0f;
            flash = 0f;
            recoil = 0f;
            hitKick = 0f;
            CancelDraw();
            AimAngle = 22f;
            transform.position = new Vector3(x, 0f, 0f);
            visual.localRotation = Quaternion.identity;
            visual.localPosition = Vector3.zero;
            // remove arrows stuck in us
            foreach (var arrow in visual.GetComponentsInChildren<Arrow>(true)) Destroy(arrow.gameObject);
            for (int i = 0; i < renderers.Count; i++) renderers[i].color = baseColors[i];
            UpdateRig(0f);
        }

        // ------------------------------------------------------------------ simulation + animation

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (deathT > 0f)
            {
                deathT += dt;
                float k = Mathf.Clamp01(deathT / 0.6f);
                float ease = 1f - (1f - k) * (1f - k) * (1f - k);
                visual.localRotation = Quaternion.Euler(0, 0, 88f * facing * ease);
                visual.localPosition = new Vector3(-facing * 0.25f * ease, Mathf.Sin(k * Mathf.PI) * 0.3f, 0);
                velX = Mathf.MoveTowards(velX, 0f, dt * 8f);
                transform.position += new Vector3(velX * dt, 0, 0);
                VerticalUpdate(dt);
                FlashUpdate(dt);
                return;
            }

            if (reloadTimer > 0f) reloadTimer -= dt;
            if (IsDrawing) Charge = Mathf.Min(1f, Charge + dt / FullDrawTime);

            float input = controlsEnabled ? moveInput : 0f;
            float target = input * moveSpeed * (IsDrawing ? 0.55f : 1f);
            velX = Mathf.MoveTowards(velX, target, dt * 30f);
            Vector3 p = transform.position;
            p.x = Mathf.Clamp(p.x + velX * dt, minX, maxX);
            if (p.x <= minX || p.x >= maxX) velX = 0f;
            transform.position = p;
            VerticalUpdate(dt);
            p = transform.position;

            // walk cycle
            float speed01 = Mathf.Clamp01(Mathf.Abs(velX) / 4f);
            walkPhase += dt * Mathf.Lerp(0f, 13f, speed01) * Mathf.Sign(velX * facing + 0.0001f);
            if (speed01 > 0.3f && Grounded)
            {
                stepTimer -= dt;
                if (stepTimer <= 0f)
                {
                    stepTimer = 0.24f;
                    Fx.Dust(new Vector2(p.x - Mathf.Sign(velX) * 0.2f, 0f), 0.6f);
                    Sfx.Play(Sfx.Step, 0.2f, 0.2f);
                }
            }

            UpdateRig(dt);
            FlashUpdate(dt);
        }

        void VerticalUpdate(float dt)
        {
            Vector3 p = transform.position;
            if (p.y <= 0f && velY <= 0f) return;
            velY -= JumpGravity * dt;
            p.y += velY * dt;
            if (p.y <= 0f)
            {
                p.y = 0f;
                if (velY < -4f)
                {
                    Fx.Dust(new Vector2(p.x, 0f), 1.2f);
                    Sfx.Play(Sfx.Step, 0.5f, 0.1f, 0.8f);
                }
                velY = 0f;
            }
            transform.position = p;
        }

        void UpdateRig(float dt)
        {
            float speed01 = Mathf.Clamp01(Mathf.Abs(velX) / 4f);
            float height = transform.position.y;
            // shadow stays on the ground and shrinks while in the air
            shadowT.localPosition = new Vector3(0, 0.02f - height, 0);
            float sh = Mathf.Clamp01(1f - height * 0.35f);
            shadowT.GetComponent<SpriteRenderer>().color = new Color(0, 0, 0, 0.35f * sh);

            float swing = Mathf.Sin(walkPhase) * 32f * speed01;
            if (height > 0.01f)
            {
                // tuck the legs in the air
                legL.localRotation = Quaternion.Euler(0, 0, 25f);
                legR.localRotation = Quaternion.Euler(0, 0, -15f);
            }
            else
            {
                legL.localRotation = Quaternion.Euler(0, 0, swing);
                legR.localRotation = Quaternion.Euler(0, 0, -swing);
            }

            float breathe = Mathf.Sin(Time.time * 2.2f) * 0.012f;
            bob = Mathf.Abs(Mathf.Sin(walkPhase)) * 0.07f * speed01 + breathe;
            hitKick = Mathf.MoveTowards(hitKick, 0f, dt * 5f);
            torso.localRotation = Quaternion.Euler(0, 0, -hitKick * 10f * facing + (IsDrawing ? -4f * Charge : 0f));
            headT.localPosition = HeadCenter + new Vector2(0, bob);
            headT.localRotation = Quaternion.Euler(0, 0, Mathf.Clamp(AimAngle * 0.25f, -10f, 18f) - hitKick * 15f * facing);

            // aim rig
            recoil = Mathf.MoveTowards(recoil, 0f, dt * 6f);
            aimPivot.localPosition = ShoulderLocal + new Vector2(-recoil * 0.06f, bob);
            aimPivot.localRotation = Quaternion.Euler(0, 0, AimAngle + recoil * 6f);

            float pull = IsDrawing ? MaxPull * Mathf.Sqrt(Charge) : 0f;
            float tipX = BowDistance - Art.BowBulge;
            float nockX = tipX - pull;
            if (IsDrawing && Charge >= 1f) nockX += Mathf.Sin(Time.time * 60f) * 0.006f;  // full-draw tremble

            bowString.SetPosition(0, new Vector3(tipX, Art.BowHalfHeight - 0.02f, 0));
            bowString.SetPosition(1, new Vector3(nockX, 0, 0));
            bowString.SetPosition(2, new Vector3(tipX, -Art.BowHalfHeight + 0.02f, 0));

            // bow flexes a bit while drawing
            bowT.localScale = new Vector3(1f + pull * 0.35f, 1f - pull * 0.08f, 1f);

            // drawing hand follows the string
            Vector2 hand = new Vector2(nockX, 0f);
            backHand.transform.localPosition = hand;
            Vector2 shoulder = new Vector2(-0.05f, 0.02f);
            Vector2 arm = hand - shoulder;
            backArm.localPosition = shoulder;
            backArm.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(arm.y, arm.x) * Mathf.Rad2Deg);
            float len = Mathf.Max(0.12f, arm.magnitude);
            var armSr = backArm.GetComponent<SpriteRenderer>();
            Vector2 b = armSr.sprite.bounds.size;
            backArm.localScale = new Vector3(len / b.x, 0.14f / b.y, 1f);

            bool showArrow = Alive && (IsDrawing || reloadTimer <= 0f) && controlsEnabled;
            nockedArrow.enabled = showArrow;
            nockedArrow.transform.localPosition = new Vector3(nockX + Art.ArrowLength, 0, 0);
        }

        void FlashUpdate(float dt)
        {
            if (flash <= 0f) return;
            flash = Mathf.MoveTowards(flash, 0f, dt * 5f);
            Color hit = new Color(1f, 0.25f, 0.2f);
            for (int i = 0; i < renderers.Count; i++)
            {
                Color c = Color.Lerp(baseColors[i], hit, flash * 0.75f);
                c.a = baseColors[i].a;
                renderers[i].color = c;
            }
        }
    }
}
