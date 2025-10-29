using UnityEngine;
using System.Collections;

public class AmbientMusicPlayer : MonoBehaviour {
    [Header("Music Tracks")]
    [SerializeField] private AudioClip[] tracks;

    [Header("Timing")]
    [SerializeField] private Vector2 silenceBetweenTracks = new Vector2(10f, 30f); // случайная пауза

    [SerializeField] private float fadeTime = 2f; // плавное затухание / появление

    [SerializeField] AudioSource source;
    private Coroutine _musicRoutine;
    private AudioClip _currentClip;

    private int _nextIndex = 0;
    private bool _switchToRandom = false;

    private void Awake() {
        source.loop = false;
        source.playOnAwake = false;
        source.spatialBlend = 0f; // 2D-звук
        source.volume = 0f;
    }

    private void OnEnable() {
        if (_musicRoutine == null)
            _musicRoutine = StartCoroutine(MusicLoop());
    }

    private void OnDisable() {
        if (_musicRoutine != null) {
            StopCoroutine(_musicRoutine);
            _musicRoutine = null;
        }
    }

    private IEnumerator MusicLoop() {
        while (true) {
            _currentClip = GetNextClip();
            source.clip = _currentClip;

            // плавное появление
            yield return StartCoroutine(FadeVolume(0f, 1f, fadeTime));
            source.Play();

            // ждем окончания трека
            yield return new WaitForSeconds(_currentClip.length - fadeTime);

            // плавное затухание
            yield return StartCoroutine(FadeVolume(1f, 0f, fadeTime));
            source.Stop();

            // случайная пауза
            float silence = Random.Range(silenceBetweenTracks.x, silenceBetweenTracks.y);
            yield return new WaitForSeconds(silence);
        }
    }

    private AudioClip GetNextClip() {
        if (!_switchToRandom) {
            var clip = tracks[_nextIndex];
            _nextIndex++;

            if (_nextIndex >= tracks.Length) {
                _nextIndex = 0;
                _switchToRandom = true; // после полного цикла включаем случайный режим
            }

            return clip;
        } else {
            return tracks[Random.Range(0, tracks.Length)];
        }
    }

    private IEnumerator FadeVolume(float from, float to, float duration) {
        float time = 0f;
        while (time < duration) {
            time += Time.deltaTime;
            source.volume = Mathf.Lerp(from, to, time / duration);
            yield return null;
        }

        source.volume = to;
    }
}