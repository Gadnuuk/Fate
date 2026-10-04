using System;
using UnityEngine;

namespace Fate.Systems.Game
{
    /// <summary>
    /// Authored set of split-screen viewport layouts, one entry per player count, so designers can
    /// author alternate layouts as separate assets and swap them on <see cref="PlayerControllerManager"/>
    /// at runtime instead of being locked to a single hardcoded layout.
    /// </summary>
    [CreateAssetMenu(fileName = "SplitScreenLayoutSet", menuName = "Fate/Game/Split Screen Layout Set")]
    public class SplitScreenLayoutSet : ScriptableObject
    {
        [Serializable]
        public struct SplitScreenLayout
        {
            public Rect[] PlayerRects;
        }

        [Tooltip("Index 0 = 1 player ... index 3 = 4 players.")]
        [SerializeField]
        private SplitScreenLayout[] layoutsByPlayerCount =
        {
            new() { PlayerRects = new[] { new Rect(0f, 0f, 1f, 1f) } },
            new()
            {
                PlayerRects = new[]
                {
                    new Rect(0f, 0f, 0.5f, 1f),
                    new Rect(0.5f, 0f, 0.5f, 1f),
                },
            },
            new()
            {
                PlayerRects = new[]
                {
                    new Rect(0f, 0.5f, 1f, 0.5f),
                    new Rect(0f, 0f, 0.5f, 0.5f),
                    new Rect(0.5f, 0f, 0.5f, 0.5f),
                },
            },
            new()
            {
                PlayerRects = new[]
                {
                    new Rect(0f, 0.5f, 0.5f, 0.5f),
                    new Rect(0.5f, 0.5f, 0.5f, 0.5f),
                    new Rect(0f, 0f, 0.5f, 0.5f),
                    new Rect(0.5f, 0f, 0.5f, 0.5f),
                },
            },
        };

        public SplitScreenLayout[] LayoutsByPlayerCount => layoutsByPlayerCount;
    }
}
