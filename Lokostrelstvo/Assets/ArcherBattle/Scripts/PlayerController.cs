using UnityEngine;

namespace ArcherBattle
{
    /// <summary>A/D to move, mouse to aim, hold left click to draw the bow and release to shoot.</summary>
    public class PlayerController : MonoBehaviour
    {
        const int PreviewDots = 12;
        const float PreviewTime = 0.55f;   // only show the first part of the arc, the rest is skill

        Archer me;
        Camera cam;
        SpriteRenderer[] dots;

        public void Init(Archer archer, Camera camera)
        {
            me = archer;
            cam = camera;
            dots = new SpriteRenderer[PreviewDots];
            var root = new GameObject("AimPreview").transform;
            root.SetParent(transform, false);
            for (int i = 0; i < PreviewDots; i++)
            {
                dots[i] = Art.MakeRenderer("dot" + i, root, Art.Circle, Color.white, 30);
                dots[i].enabled = false;
            }
        }

        void Update()
        {
            if (me == null) return;
            var gm = GameManager.Instance;
            bool active = gm != null && gm.State == GameState.Playing && me.controlsEnabled && me.Alive;

            if (!active)
            {
                me.SetMove(0f);
                if (me.IsDrawing) me.CancelDraw();
                ShowPreview(false);
                return;
            }

            me.SetMove(GameInput.Horizontal);
            if (GameInput.JumpPressed) me.Jump();

            Vector3 mouse = GameInput.MousePosition;
            mouse.z = -cam.transform.position.z;
            me.AimAtWorld(cam.ScreenToWorldPoint(mouse));

            if (GameInput.FireDown || (GameInput.FireHeld && !me.IsDrawing)) me.BeginDraw();
            if (GameInput.FireUp || (!GameInput.FireHeld && me.IsDrawing)) me.ReleaseDraw();

            ShowPreview(me.IsDrawing);
        }

        void ShowPreview(bool on)
        {
            if (dots == null) return;
            if (!on)
            {
                foreach (var d in dots) d.enabled = false;
                return;
            }
            Vector2 p = me.LaunchPoint(me.AimAngle);
            Vector2 v = me.AimDirection * Archer.SpeedForCharge(me.Charge);
            for (int i = 0; i < dots.Length; i++)
            {
                float t = (i + 1) / (float)dots.Length * PreviewTime;
                Vector2 pos = p + v * t + 0.5f * Arrow.Gravity * t * t * Vector2.down;
                var d = dots[i];
                d.enabled = pos.y > 0f;
                d.transform.position = pos;
                float k = 1f - i / (float)dots.Length;
                d.transform.localScale = Vector3.one * Mathf.Lerp(0.05f, 0.12f, k);
                d.color = new Color(1f, 1f, 1f, 0.75f * k * Mathf.Lerp(0.5f, 1f, me.Charge));
            }
        }
    }
}
