using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArcherBattle
{
    /// <summary>
    /// Builds the whole uGUI interface in code: main menu with difficulty selection, HUD,
    /// countdown, headshot banner, floating damage numbers, VICTORY/DEFEAT screen and pause menu.
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        static readonly Color Gold = new Color(1f, 0.8f, 0.25f);
        static readonly Color Dark = new Color(0.07f, 0.06f, 0.09f, 0.88f);
        static readonly Color PlayerBlue = new Color(0.3f, 0.55f, 1f);
        static readonly Color EnemyRed = new Color(1f, 0.32f, 0.27f);

        GameManager gm;
        Font font;
        Camera cam;

        GameObject menu, hud, gameOver, pause;
        Text menuScore, hudScore, hudAiName, countdown, banner, resultTitle, resultSub, resultScore, hint;
        RectTransform playerFill, aiFill, playerLag, aiLag;
        Image flash;
        readonly Button[] diffButtons = new Button[3];
        float countdownAnim, bannerAnim, resultAnim, flashAlpha;
        Color flashColor;
        float playerLagV = 1f, aiLagV = 1f;

        class Floating { public Text text; public Vector2 world; public float t; public bool active; }
        readonly List<Floating> floating = new List<Floating>();
        RectTransform floatRoot;

        public static GameUI Build(GameManager gm, Camera cam)
        {
            var go = new GameObject("UI");
            var ui = go.AddComponent<GameUI>();
            ui.gm = gm;
            ui.cam = cam;
            ui.Construct();
            return ui;
        }

        // ------------------------------------------------------------------ building helpers

        static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        static RectTransform Stretch(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        static Image AddImage(RectTransform rt, Color c, bool rounded = true)
        {
            var img = rt.gameObject.AddComponent<Image>();
            if (rounded)
            {
                img.sprite = Art.RoundedRect(true);
                img.type = Image.Type.Sliced;
            }
            img.color = c;
            return img;
        }

        Text Label(Transform parent, string text, int size, Color color, Vector2 anchor, Vector2 pos, Vector2 box,
                   TextAnchor align = TextAnchor.MiddleCenter, bool bold = true, bool outline = true)
        {
            var rt = Rect("Text", parent, anchor, pos, box);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            if (outline)
            {
                var o = rt.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(0, 0, 0, 0.85f);
                o.effectDistance = new Vector2(2.5f, -2.5f);
                var s = rt.gameObject.AddComponent<Shadow>();
                s.effectColor = new Color(0, 0, 0, 0.5f);
                s.effectDistance = new Vector2(0, -6f);
            }
            return t;
        }

        Button MakeButton(Transform parent, string text, Vector2 pos, Vector2 size, Color color, int fontSize,
                          UnityEngine.Events.UnityAction onClick, Color? textColor = null)
        {
            var rt = Rect("Button " + text, parent, new Vector2(0.5f, 0.5f), pos, size);
            var shadow = Rect("Shadow", rt, new Vector2(0.5f, 0.5f), new Vector2(0, -7), size);
            AddImage(shadow, new Color(0, 0, 0, 0.45f)).raycastTarget = false;
            var img = AddImage(rt, color);
            var shine = Rect("Shine", rt, new Vector2(0.5f, 1f), new Vector2(0, -size.y * 0.22f), new Vector2(size.x - 16, size.y * 0.32f));
            AddImage(shine, new Color(1, 1, 1, 0.18f)).raycastTarget = false;
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.0f, 1.0f, 1.0f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f);
            cb.selectedColor = Color.white;
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            b.onClick.AddListener(onClick);
            rt.gameObject.AddComponent<ButtonFx>();
            var label = Label(rt, text, fontSize, textColor ?? new Color(0.15f, 0.08f, 0.02f), new Vector2(0.5f, 0.5f), new Vector2(0, 2), size, TextAnchor.MiddleCenter, true, false);
            label.name = "Label";
            return b;
        }

        RectTransform Bar(Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color fill, bool rightToLeft, out RectTransform lagFill)
        {
            var frame = Rect("Bar", parent, anchor, pos, size + new Vector2(10, 10));
            AddImage(frame, new Color(0, 0, 0, 0.7f));
            var inner = Rect("Inner", frame, new Vector2(0.5f, 0.5f), Vector2.zero, size);
            AddImage(inner, new Color(0.18f, 0.1f, 0.1f, 1f), false);
            lagFill = Stretch("Lag", inner);
            AddImage(lagFill, new Color(1f, 0.95f, 0.8f), false);
            var f = Stretch("Fill", inner);
            AddImage(f, fill, false);
            var shine = Stretch("Shine", inner);
            shine.anchorMin = new Vector2(0, 0.55f);
            AddImage(shine, new Color(1, 1, 1, 0.18f), false);
            if (rightToLeft)
            {
                f.pivot = lagFill.pivot = new Vector2(1, 0.5f);
            }
            f.gameObject.name = rightToLeft ? "FillRTL" : "FillLTR";
            lagFill.gameObject.name = f.gameObject.name;
            return f;
        }

        static void SetFill(RectTransform fill, float v)
        {
            v = Mathf.Clamp01(v);
            if (fill.gameObject.name == "FillRTL") { fill.anchorMin = new Vector2(1f - v, 0); fill.anchorMax = new Vector2(1, 1); }
            else { fill.anchorMin = new Vector2(0, 0); fill.anchorMax = new Vector2(v, 1); }
            fill.offsetMin = fill.offsetMax = Vector2.zero;
        }

        // ------------------------------------------------------------------ construction

        void Construct()
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }

            floatRoot = Stretch("Floating", transform);
            BuildHud();
            BuildMenu();
            BuildGameOver();
            BuildPause();

            countdown = Label(transform, "", 190, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(900, 260));
            banner = Label(transform, "", 96, Gold, new Vector2(0.5f, 0.5f), new Vector2(0, 300), new Vector2(1200, 160));

            var fl = Stretch("Flash", transform);
            flash = AddImage(fl, new Color(1, 1, 1, 0), false);
            flash.raycastTarget = false;
        }

        void BuildHud()
        {
            hud = Stretch("HUD", transform).gameObject;
            var top = hud.transform;

            Label(top, "YOU", 40, PlayerBlue, new Vector2(0, 1), new Vector2(110, -50), new Vector2(200, 60), TextAnchor.MiddleLeft);
            playerFill = Bar(top, new Vector2(0, 1), new Vector2(330, -100), new Vector2(560, 30), PlayerBlue, false, out playerLag);

            hudAiName = Label(top, "AI", 40, EnemyRed, new Vector2(1, 1), new Vector2(-110, -50), new Vector2(400, 60), TextAnchor.MiddleRight);
            aiFill = Bar(top, new Vector2(1, 1), new Vector2(-330, -100), new Vector2(560, 30), EnemyRed, true, out aiLag);

            var box = Rect("ScoreBox", top, new Vector2(0.5f, 1), new Vector2(0, -75), new Vector2(260, 110));
            AddImage(box, Dark);
            Label(box, "WINS", 24, new Color(1, 1, 1, 0.7f), new Vector2(0.5f, 1), new Vector2(0, -22), new Vector2(200, 30), TextAnchor.MiddleCenter, true, false);
            hudScore = Label(box, "0 : 0", 52, Gold, new Vector2(0.5f, 0.5f), new Vector2(0, -12), new Vector2(240, 70));

            hint = Label(top, "A / D  premikanje     •     SPACE / W  skok     •     Drži levi klik  napni lok     •     Spusti  strel     •     ESC  pavza",
                         24, new Color(1, 1, 1, 0.75f), new Vector2(0.5f, 0), new Vector2(0, 34), new Vector2(1600, 40), TextAnchor.MiddleCenter, false);
        }

        void BuildMenu()
        {
            menu = Stretch("Menu", transform).gameObject;
            AddImage((RectTransform)menu.transform, new Color(0.02f, 0.02f, 0.05f, 0.5f), false);

            var title = Label(menu.transform, "ARCHER BATTLE", 130, Gold, new Vector2(0.5f, 0.5f), new Vector2(0, 330), new Vector2(1600, 180));
            title.GetComponent<Outline>().effectDistance = new Vector2(4, -4);
            Label(menu.transform, "PLAYER  vs  AI", 40, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 225), new Vector2(800, 60));

            var card = Rect("Card", menu.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(820, 470));
            AddImage(card, Dark);
            var border = Rect("Border", card, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(836, 486));
            border.SetAsFirstSibling();
            AddImage(border, new Color(Gold.r, Gold.g, Gold.b, 0.6f));

            Label(card, "DIFFICULTY", 34, new Color(1, 1, 1, 0.85f), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(600, 50));
            string[] names = { "EASY", "MEDIUM", "HARD" };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                diffButtons[i] = MakeButton(card, names[i], new Vector2(-250 + i * 250, 110), new Vector2(220, 78), Color.gray, 32,
                                            () => { gm.SetDifficulty((Difficulty)idx); RefreshDifficulty(); });
            }

            MakeButton(card, "PLAY", new Vector2(0, -35), new Vector2(420, 110), Gold, 60, () => gm.StartMatch());
            menuScore = Label(card, "", 34, Color.white, new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(700, 50));

            Label(menu.transform, "A / D – premikanje     •     SPACE / W – skok     •     Miška – merjenje     •     Drži levi klik – napni lok     •     Spusti – strel",
                  26, new Color(1, 1, 1, 0.85f), new Vector2(0.5f, 0), new Vector2(0, 60), new Vector2(1700, 40), TextAnchor.MiddleCenter, false);
        }

        void BuildGameOver()
        {
            gameOver = Stretch("GameOver", transform).gameObject;
            AddImage((RectTransform)gameOver.transform, new Color(0, 0, 0, 0.55f), false);
            resultTitle = Label(gameOver.transform, "VICTORY!", 190, Gold, new Vector2(0.5f, 0.5f), new Vector2(0, 230), new Vector2(1600, 260));
            resultTitle.GetComponent<Outline>().effectDistance = new Vector2(5, -5);
            resultSub = Label(gameOver.transform, "", 40, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 90), new Vector2(1200, 60));
            resultScore = Label(gameOver.transform, "", 46, Gold, new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(1200, 60));
            MakeButton(gameOver.transform, "PLAY AGAIN", new Vector2(0, -110), new Vector2(460, 110), Gold, 52, () => gm.StartMatch());
            MakeButton(gameOver.transform, "MAIN MENU", new Vector2(0, -245), new Vector2(360, 84), new Color(0.75f, 0.75f, 0.8f), 36, () => gm.ShowMenu());
        }

        void BuildPause()
        {
            pause = Stretch("Pause", transform).gameObject;
            AddImage((RectTransform)pause.transform, new Color(0, 0, 0, 0.6f), false);
            Label(pause.transform, "PAUSED", 130, Color.white, new Vector2(0.5f, 0.5f), new Vector2(0, 200), new Vector2(1000, 180));
            MakeButton(pause.transform, "RESUME", new Vector2(0, 20), new Vector2(400, 100), Gold, 48, () => gm.SetPaused(false));
            MakeButton(pause.transform, "MAIN MENU", new Vector2(0, -110), new Vector2(360, 84), new Color(0.75f, 0.75f, 0.8f), 36, () => gm.ShowMenu());
        }

        // ------------------------------------------------------------------ public API

        public void ShowMenu()
        {
            menu.SetActive(true);
            hud.SetActive(false);
            gameOver.SetActive(false);
            pause.SetActive(false);
            countdown.text = banner.text = "";
            RefreshDifficulty();
            RefreshScore();
        }

        public void ShowHud()
        {
            menu.SetActive(false);
            gameOver.SetActive(false);
            pause.SetActive(false);
            hud.SetActive(true);
            hint.enabled = true;
            playerLagV = aiLagV = 1f;
            hudAiName.text = "AI  •  " + gm.Difficulty.ToString().ToUpper();
            RefreshScore();
        }

        public void ShowPause(bool on) => pause.SetActive(on);

        public void ShowGameOver(bool victory)
        {
            gameOver.SetActive(true);
            resultTitle.text = victory ? "VICTORY!" : "DEFEAT!";
            resultTitle.color = victory ? Gold : EnemyRed;
            resultSub.text = victory ? "You defeated the " + gm.Difficulty.ToString().ToUpper() + " archer!" : "The AI archer won this duel.";
            resultAnim = 0f;
            RefreshScore();
        }

        public void RefreshScore()
        {
            string s = gm.PlayerWins + " : " + gm.AIWins;
            hudScore.text = s;
            resultScore.text = "YOU  " + s + "  AI";
            menuScore.text = "WINS     YOU  " + gm.PlayerWins + "   :   " + gm.AIWins + "  AI";
        }

        void RefreshDifficulty()
        {
            Color[] on = { new Color(0.4f, 0.85f, 0.4f), new Color(1f, 0.7f, 0.2f), new Color(1f, 0.35f, 0.3f) };
            for (int i = 0; i < 3; i++)
            {
                bool sel = (int)gm.Difficulty == i;
                diffButtons[i].targetGraphic.color = sel ? on[i] : new Color(0.32f, 0.32f, 0.36f);
                diffButtons[i].GetComponentInChildren<Text>().color = sel ? new Color(0.1f, 0.06f, 0.02f) : new Color(1, 1, 1, 0.6f);
            }
        }

        public void SetCountdown(string text)
        {
            countdown.text = text;
            countdownAnim = 0f;
        }

        public void Banner(string text, Color color)
        {
            banner.text = text;
            banner.color = color;
            bannerAnim = 0f;
        }

        public void Flash(Color c, float alpha)
        {
            flashColor = c;
            flashAlpha = alpha;
        }

        public void FloatText(Vector2 world, string text, Color color, int size)
        {
            Floating f = null;
            foreach (var x in floating) if (!x.active) { f = x; break; }
            if (f == null)
            {
                f = new Floating { text = Label(floatRoot, "", size, color, Vector2.zero, Vector2.zero, new Vector2(400, 80)) };
                floating.Add(f);
            }
            f.active = true;
            f.t = 0f;
            f.world = world + new Vector2(Random.Range(-0.3f, 0.3f), 0.4f);
            f.text.text = text;
            f.text.color = color;
            f.text.fontSize = size;
            f.text.gameObject.SetActive(true);
        }

        // ------------------------------------------------------------------ animation

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            // HUD bars
            if (hud.activeSelf && gm.Player != null && gm.AI != null)
            {
                float p = gm.Player.hp / gm.Player.maxHp, a = gm.AI.hp / gm.AI.maxHp;
                SetFill(playerFill, p);
                SetFill(aiFill, a);
                playerLagV = Mathf.Max(p, Mathf.MoveTowards(playerLagV, p, dt * 0.6f));
                aiLagV = Mathf.Max(a, Mathf.MoveTowards(aiLagV, a, dt * 0.6f));
                SetFill(playerLag, playerLagV);
                SetFill(aiLag, aiLagV);
            }

            // countdown punch
            if (countdown.text.Length > 0)
            {
                countdownAnim += dt;
                float k = Mathf.Clamp01(countdownAnim / 0.25f);
                countdown.transform.localScale = Vector3.one * Mathf.Lerp(2.2f, 1f, 1f - (1f - k) * (1f - k));
                Color c = countdown.color;
                c.a = Mathf.Clamp01(1.2f - countdownAnim * 1.2f) + 0.2f;
                countdown.color = c;
            }

            // headshot banner
            if (banner.text.Length > 0)
            {
                bannerAnim += dt;
                float k = Mathf.Clamp01(bannerAnim / 0.18f);
                banner.transform.localScale = Vector3.one * (Mathf.Lerp(1.8f, 1f, k) + Mathf.Sin(bannerAnim * 20f) * 0.03f);
                Color c = banner.color;
                c.a = Mathf.Clamp01(1.6f - bannerAnim);
                banner.color = c;
                if (bannerAnim > 1.6f) banner.text = "";
            }

            // result title slam
            if (gameOver.activeSelf)
            {
                resultAnim += dt;
                float k = Mathf.Clamp01(resultAnim / 0.35f);
                float overshoot = 1f + Mathf.Sin(k * Mathf.PI) * 0.15f;
                resultTitle.transform.localScale = Vector3.one * Mathf.Lerp(3f, 1f, k) * overshoot * (1f + Mathf.Sin(resultAnim * 3f) * 0.02f);
            }

            // screen flash
            if (flashAlpha > 0f)
            {
                flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, dt * 2.5f);
                flash.color = new Color(flashColor.r, flashColor.g, flashColor.b, flashAlpha);
            }

            // floating damage numbers
            foreach (var f in floating)
            {
                if (!f.active) continue;
                f.t += dt;
                if (f.t > 1.1f)
                {
                    f.active = false;
                    f.text.gameObject.SetActive(false);
                    continue;
                }
                Vector2 w = f.world + Vector2.up * (f.t * 1.4f);
                f.text.transform.position = cam.WorldToScreenPoint(w);
                float pop = f.t < 0.12f ? Mathf.Lerp(1.6f, 1f, f.t / 0.12f) : 1f;
                f.text.transform.localScale = Vector3.one * pop;
                Color c = f.text.color;
                c.a = Mathf.Clamp01((1.1f - f.t) * 2.5f);
                f.text.color = c;
            }
        }
    }
}
