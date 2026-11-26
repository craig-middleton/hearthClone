using System;
using UnityEngine;

namespace HearthstoneClone.UI
{
    // A narrowly gated original-source experiment, not a replacement for every minion.
    public sealed class GrizzlyTauntPrototypeLibrary : ScriptableObject
    {
        [Serializable] public sealed class Channel
        {
            public string property;
            public AnimationCurve curve;
        }
        public Sprite grizzlyArtwork;
        public AnimationClip originalSpawnClip;
        public Channel[] spawnChannels;
        public float spawnDuration;
        public AudioClip tauntUpSound;
        public float idleDelay = .6f;
        public float scaleDuration = .4f;
        public float initialScale = .1f;
        public float settledScale = .9775735736f;
        public float soundVolume = .9f;
        public float soundPitch = 1;
    }
}
