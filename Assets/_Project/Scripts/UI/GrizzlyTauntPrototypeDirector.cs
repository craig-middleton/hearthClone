using System.Collections.Generic;
using HearthstoneClone.Core;
using UnityEngine;

namespace HearthstoneClone.UI
{
    // State belongs to the current board, not individual disposable MinionViews.
    // Rebuilding views must not replay the original taunt birth or its sound.
    public sealed class GrizzlyTauntPrototypeDirector : MonoBehaviour
    {
        sealed class Birth
        {
            public float started, lastSeen;
            public bool soundPlayed;
        }
        readonly Dictionary<Minion, Birth> births = new Dictionary<Minion, Birth>();
        readonly List<Minion> stale = new List<Minion>();
        AudioSource voice;
        public int ActiveBirths => births.Count;
        public int PlayedTauntSounds { get; private set; }

        public float Observe(Minion minion, GrizzlyTauntPrototypeLibrary library)
        {
            if (!births.TryGetValue(minion, out var birth))
            {
                birth = new Birth { started = Time.time };
                births.Add(minion, birth);
            }
            birth.lastSeen = Time.time;
            float age = Time.time - birth.started;
            if (!birth.soundPlayed && age >= library.idleDelay)
            {
                birth.soundPlayed = true;
                if (library.tauntUpSound != null)
                {
                    if (voice == null)
                    {
                        voice = gameObject.AddComponent<AudioSource>();
                        voice.playOnAwake = false;
                        // Original state6 action: spatialBlend0, pitch1, volume.9.
                        voice.spatialBlend = 0;
                    }
                    voice.pitch = library.soundPitch;
                    voice.PlayOneShot(library.tauntUpSound, library.soundVolume);
                    PlayedTauntSounds++;
                }
            }
            return age;
        }

        void Update()
        {
            stale.Clear();
            foreach (var pair in births)
                if (Time.time - pair.Value.lastSeen > 1) stale.Add(pair.Key);
            foreach (var minion in stale) births.Remove(minion);
        }
        void OnDisable()
        {
            births.Clear(); stale.Clear(); PlayedTauntSounds = 0;
            if (voice != null) voice.Stop();
        }
    }
}
