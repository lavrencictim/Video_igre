using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ArcherBattle
{
    public enum GameState { Menu, Countdown, Playing, Paused, GameOver }

    /// <summary>
    /// Owns the match flow: menu -> countdown -> fight -> VICTORY/DEFEAT -> play again.
    /// Builds the whole scene (camera, arena, archers, UI) at startup.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Menu;
        public Difficulty Difficulty { get; private set; } = Difficulty.Medium;
        public int PlayerWins { get; private set; }
        public int AIWins { get; private set; }
        public Archer Player { get; private set; }
        public Archer AI { get; private set; }
        public readonly List<Archer> Archers = new List<Archer>();

        const float PlayerStartX = -9f, AIStartX = 9f;

        Camera cam;
        Arena arena;
        GameUI ui;
        AIController aiController;
        Coroutine flow;
        GameState stateBeforePause;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            Application.targetFrameRate = 120;
            Time.timeScale = 1f;

            PlayerWins = PlayerPrefs.GetInt("ArcherBattle.PlayerWins", 0);
            AIWins = PlayerPrefs.GetInt("ArcherBattle.AIWins", 0);
            Difficulty = (Difficulty)Mathf.Clamp(PlayerPrefs.GetInt("ArcherBattle.Difficulty", 1), 0, 2);

            Sfx.Init(transform);
            Fx.Init(transform);
            cam = CameraRig.Setup();
            arena = Arena.Build();

            Player = Archer.Create("Player", 1, new Color(0.22f, 0.42f, 0.85f), new Color(0.16f, 0.45f, 0.25f), new Color(1f, 0.85f, 0.3f));
            Player.minX = Arena.PlayerMinX;
            Player.maxX = Arena.PlayerMaxX;
            Player.moveSpeed = 5.5f;
            Player.healthBar = HealthBar.Create(Player, PlayerBlueFrame, true);
            Player.gameObject.AddComponent<PlayerController>().Init(Player, cam);

            AI = Archer.Create("AI", -1, new Color(0.8f, 0.2f, 0.18f), new Color(0.25f, 0.22f, 0.22f), new Color(0.95f, 0.95f, 0.95f));
            AI.minX = Arena.AIMinX;
            AI.maxX = Arena.AIMaxX;
            AI.healthBar = HealthBar.Create(AI, EnemyRedFrame, false);
            aiController = AI.gameObject.AddComponent<AIController>();
            aiController.Init(AI, Player, Difficulty);

            Archers.Add(Player);
            Archers.Add(AI);

            var rig = cam.GetComponent<CameraRig>();
            rig.a = Player.transform;
            rig.b = AI.transform;

            ui = GameUI.Build(this, cam);
            ShowMenu();
        }

        static readonly Color PlayerBlueFrame = new Color(0.25f, 0.5f, 1f);
        static readonly Color EnemyRedFrame = new Color(0.9f, 0.25f, 0.2f);

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ------------------------------------------------------------------ flow

        public void SetDifficulty(Difficulty d)
        {
            Difficulty = d;
            PlayerPrefs.SetInt("ArcherBattle.Difficulty", (int)d);
            aiController.SetDifficulty(d);
        }

        public void ShowMenu()
        {
            StopFlow();
            Time.timeScale = 1f;
            State = GameState.Menu;
            Arrow.ClearAll();
            Player.ResetForRound(PlayerStartX);
            AI.ResetForRound(AIStartX);
            Player.controlsEnabled = AI.controlsEnabled = false;
            ui.ShowMenu();
        }

        public void StartMatch()
        {
            StopFlow();
            Time.timeScale = 1f;
            Arrow.ClearAll();
            Player.ResetForRound(PlayerStartX);
            AI.ResetForRound(AIStartX);
            aiController.SetDifficulty(Difficulty);
            aiController.ResetRound();
            ui.ShowHud();
            flow = StartCoroutine(Countdown());
        }

        void StopFlow()
        {
            if (flow != null) StopCoroutine(flow);
            flow = null;
        }

        IEnumerator Countdown()
        {
            State = GameState.Countdown;
            Player.controlsEnabled = AI.controlsEnabled = false;
            for (int i = 3; i >= 1; i--)
            {
                ui.SetCountdown(i.ToString());
                Sfx.Play(Sfx.Beep, 0.6f, 0f);
                yield return new WaitForSecondsRealtime(0.7f);
            }
            ui.SetCountdown("FIGHT!");
            Sfx.Play(Sfx.Fight, 0.7f, 0f);
            Sfx.Play(Sfx.Crowd, 0.35f, 0.05f);
            arena.Cheer(0.6f);
            State = GameState.Playing;
            Player.controlsEnabled = AI.controlsEnabled = true;
            aiController.ResetRound();
            yield return new WaitForSecondsRealtime(0.8f);
            ui.SetCountdown("");
            flow = null;
        }

        public void OnArcherHit(Archer victim, int damage, bool head, Vector2 point)
        {
            if (damage <= 0) return;
            Color c = head ? new Color(1f, 0.85f, 0.2f) : Color.white;
            ui.FloatText(point, "-" + damage, c, head ? 64 : 48);
            arena.Cheer(head ? 1f : 0.5f);
            if (head)
            {
                ui.Banner(victim == AI ? "HEADSHOT!" : "OUCH! HEADSHOT!", new Color(1f, 0.85f, 0.2f));
                ui.Flash(new Color(1f, 0.95f, 0.7f), 0.35f);
                Sfx.Play(Sfx.Crowd, 0.45f, 0.1f);
                if (victim.Alive) StartCoroutine(HitStop(0.09f));
            }
            else if (victim == Player)
            {
                ui.Flash(new Color(1f, 0.1f, 0.1f), 0.18f);
            }
        }

        IEnumerator HitStop(float seconds)
        {
            if (State != GameState.Playing || Time.timeScale < 1f) yield break;
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(seconds);
            if (State == GameState.Playing) Time.timeScale = 1f;
        }

        public void OnArcherDied(Archer who)
        {
            if (State != GameState.Playing) return;
            State = GameState.GameOver;
            Player.controlsEnabled = AI.controlsEnabled = false;
            bool victory = who == AI;
            if (victory) PlayerWins++; else AIWins++;
            PlayerPrefs.SetInt("ArcherBattle.PlayerWins", PlayerWins);
            PlayerPrefs.SetInt("ArcherBattle.AIWins", AIWins);
            PlayerPrefs.Save();
            StopFlow();
            flow = StartCoroutine(EndSequence(victory));
        }

        IEnumerator EndSequence(bool victory)
        {
            ui.SetCountdown("");
            // dramatic slow motion on the final hit
            Time.timeScale = 0.3f;
            CameraRig.Shake(0.6f);
            arena.Cheer(1f);
            Sfx.Play(Sfx.Crowd, 0.6f, 0f, 0.9f);
            yield return new WaitForSecondsRealtime(1.3f);
            Time.timeScale = 1f;
            Sfx.Play(victory ? Sfx.Victory : Sfx.Defeat, 0.7f, 0f);
            if (victory) Fx.Burst(AI.HeadWorld + Vector2.up, new Color(1f, 0.85f, 0.25f), 30, 3f, 9f, 0.4f, 1.6f, 6f, 90f, 120f, Art.Star, 1f);
            ui.ShowGameOver(victory);
            flow = null;
        }

        public void SetPaused(bool paused)
        {
            if (paused)
            {
                if (State != GameState.Playing) return;
                stateBeforePause = State;
                State = GameState.Paused;
                Time.timeScale = 0f;
                ui.ShowPause(true);
            }
            else if (State == GameState.Paused)
            {
                State = stateBeforePause;
                Time.timeScale = 1f;
                ui.ShowPause(false);
            }
        }

        void Update()
        {
            if (GameInput.PausePressed)
            {
                if (State == GameState.Paused) SetPaused(false);
                else if (State == GameState.Playing) SetPaused(true);
                else if (State == GameState.GameOver) ShowMenu();
            }
        }
    }
}
