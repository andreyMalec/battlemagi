    using UnityEngine;

    public static class Ext {
        public static void Play(this AudioSource source, AudioClip[] clips) {
            source.PlayOneShot(clips[Random.Range(0, clips.Length)]);
        }
    }
