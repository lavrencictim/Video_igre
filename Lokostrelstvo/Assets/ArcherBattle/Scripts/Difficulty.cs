using UnityEngine;

namespace ArcherBattle
{
    public enum Difficulty { Easy, Medium, Hard }

    /// <summary>Tuning values for the AI archer at a given difficulty.</summary>
    public struct AISettings
    {
        public float reactionTime;     // seconds before the AI reacts to an incoming arrow
        public float dodgeChance;      // probability it tries to dodge a threatening arrow
        public float aimError;         // standard deviation of the aim error in degrees
        public float minShotDelay;     // pause between shots (min)
        public float maxShotDelay;     // pause between shots (max)
        public float leadFactor;       // 0 = shoot where the player is, 1 = predict movement perfectly
        public float moveSpeed;
        public float headshotChance;   // how often it aims for the head
        public float minCharge;        // weakest draw it will use
        public float restlessness;     // how often it changes position (0..1)
        public float jumpsPerSecond;   // random hops to be harder to hit

        public static AISettings For(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Easy:
                    return new AISettings
                    {
                        reactionTime = 0.65f, dodgeChance = 0.25f, aimError = 6.5f,
                        minShotDelay = 2.4f, maxShotDelay = 3.6f, leadFactor = 0f,
                        moveSpeed = 3.6f, headshotChance = 0.05f, minCharge = 0.45f, restlessness = 0.35f, jumpsPerSecond = 0.15f
                    };
                case Difficulty.Hard:
                    return new AISettings
                    {
                        reactionTime = 0.14f, dodgeChance = 0.85f, aimError = 1.3f,
                        minShotDelay = 0.9f, maxShotDelay = 1.6f, leadFactor = 1f,
                        moveSpeed = 5.6f, headshotChance = 0.35f, minCharge = 0.65f, restlessness = 0.9f, jumpsPerSecond = 0.5f
                    };
                default:
                    return new AISettings
                    {
                        reactionTime = 0.35f, dodgeChance = 0.55f, aimError = 3.2f,
                        minShotDelay = 1.6f, maxShotDelay = 2.5f, leadFactor = 0.5f,
                        moveSpeed = 4.6f, headshotChance = 0.18f, minCharge = 0.55f, restlessness = 0.6f, jumpsPerSecond = 0.3f
                    };
            }
        }
    }
}
