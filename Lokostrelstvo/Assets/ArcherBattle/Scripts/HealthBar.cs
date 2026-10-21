using UnityEngine;

namespace ArcherBattle
{
    /// <summary>World-space health bar (plus optional bow-charge bar) floating above an archer.</summary>
    public class HealthBar : MonoBehaviour
    {
        const float Width = 1.5f, Height = 0.17f;

        Archer owner;
        Transform fill, lag, chargeFill;
        SpriteRenderer fillSr;
        GameObject chargeRoot;
        float shown = 1f, lagShown = 1f, lagDelay;
        float lastHp = -1f;
        float punch;

        public static HealthBar Create(Archer owner, Color frameColor, bool showCharge)
        {
            var go = new GameObject(owner.name + " HealthBar");
            var hb = go.AddComponent<HealthBar>();
            hb.owner = owner;
            hb.Build(frameColor, showCharge);
            return hb;
        }

        void Build(Color frameColor, bool showCharge)
        {
            var leftPivot = Art.Square(new Vector2(0f, 0.5f));
            Art.Box("frame", transform, Art.RoundedRect(false), frameColor, 50, Vector2.zero, new Vector2(Width + 0.16f, Height + 0.16f));
            Art.Box("bg", transform, Art.Square(), new Color(0.08f, 0.06f, 0.06f), 51, Vector2.zero, new Vector2(Width, Height));
            lag = Art.Box("lag", transform, leftPivot, new Color(1f, 0.95f, 0.8f), 52, new Vector2(-Width / 2, 0), new Vector2(Width, Height)).transform;
            fillSr = Art.Box("fill", transform, leftPivot, Color.green, 53, new Vector2(-Width / 2, 0), new Vector2(Width, Height));
            fill = fillSr.transform;
            Art.Box("shine", transform, Art.Square(), new Color(1, 1, 1, 0.18f), 54, new Vector2(0, Height * 0.25f), new Vector2(Width, Height * 0.35f));

            if (showCharge)
            {
                chargeRoot = new GameObject("charge");
                chargeRoot.transform.SetParent(transform, false);
                chargeRoot.transform.localPosition = new Vector3(0, -0.24f, 0);
                Art.Box("bg", chargeRoot.transform, Art.Square(), new Color(0, 0, 0, 0.6f), 50, Vector2.zero, new Vector2(Width, 0.09f));
                chargeFill = Art.Box("fill", chargeRoot.transform, leftPivot, new Color(1f, 0.8f, 0.2f), 51, new Vector2(-Width / 2, 0), new Vector2(Width, 0.09f)).transform;
                chargeRoot.SetActive(false);
            }
        }

        void LateUpdate()
        {
            if (owner == null) { Destroy(gameObject); return; }
            float frac = Mathf.Clamp01(owner.hp / owner.maxHp);
            if (!Mathf.Approximately(frac * 100f, lastHp))
            {
                if (lastHp >= 0f && frac * 100f < lastHp) { lagDelay = 0.45f; punch = 1f; }
                lastHp = frac * 100f;
            }

            float dt = Time.deltaTime;
            shown = Mathf.MoveTowards(shown, frac, dt * 3f);
            if (lagDelay > 0f) lagDelay -= dt;
            else lagShown = Mathf.MoveTowards(lagShown, shown, dt * 0.8f);
            if (lagShown < shown) lagShown = shown;

            fill.localScale = new Vector3(Width * shown, Height, 1f);
            lag.localScale = new Vector3(Width * lagShown, Height, 1f);
            fillSr.color = frac > 0.5f ? Color.Lerp(new Color(1f, 0.85f, 0.15f), new Color(0.3f, 0.9f, 0.3f), (frac - 0.5f) * 2f)
                                       : Color.Lerp(new Color(0.95f, 0.2f, 0.15f), new Color(1f, 0.85f, 0.15f), frac * 2f);

            punch = Mathf.MoveTowards(punch, 0f, dt * 4f);
            transform.localScale = Vector3.one * (1f + punch * 0.18f);
            transform.position = owner.transform.position + new Vector3(0, 2.75f, 0);

            if (chargeRoot != null)
            {
                bool on = owner.IsDrawing;
                chargeRoot.SetActive(on);
                if (on)
                {
                    chargeFill.localScale = new Vector3(Width * owner.Charge, 0.09f, 1f);
                    chargeFill.GetComponent<SpriteRenderer>().color = owner.Charge >= 1f
                        ? Color.Lerp(new Color(1f, 0.5f, 0.1f), Color.white, Mathf.PingPong(Time.time * 6f, 1f))
                        : new Color(1f, 0.8f, 0.2f);
                }
            }
        }
    }
}
