using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace MaskboundJinosi.Gameplay.Scene
{
	[AddComponentMenu("Maskbound/Audio/Scene BGM Manager")]
	public class SceneBgmManager : MonoBehaviour
	{
		[Serializable]
		public class SceneBgmEntry
		{
			public string SceneName;
			public AudioClip Clip;
			[Range(0f, 1f)] public float Volume = 0.6f;
		}

		[SerializeField] private SceneBgmEntry[] sceneMusic;
		[SerializeField] private string[] ignoredSceneNames = { "LoadingScreen" };
		[SerializeField] private bool silenceDuringIgnoredScenes = true;
		[SerializeField] private AudioMixerGroup musicMixerGroup;
		[SerializeField, Range(0f, 1f)] private float defaultVolume = 0.6f;
		[SerializeField, Min(0f)] private float fadeDuration = 0.5f;
		[SerializeField] private bool stopMusicWhenSceneHasNoEntry;

		private AudioSource _audioSource;
		private AudioListener _fallbackAudioListener;
		private Coroutine _fadeCoroutine;
		private readonly Dictionary<string, SceneBgmEntry> _entries =
			new Dictionary<string, SceneBgmEntry>(StringComparer.Ordinal);

		public AudioClip CurrentClip => _audioSource != null ? _audioSource.clip : null;

		private void Awake()
		{
			_audioSource = GetComponent<AudioSource>();
			if (_audioSource == null)
			{
				_audioSource = gameObject.AddComponent<AudioSource>();
			}

			_audioSource.playOnAwake = false;
			_audioSource.loop = true;
			_audioSource.spatialBlend = 0f;
			_audioSource.outputAudioMixerGroup = musicMixerGroup;
			EnsureAudioListener();
			BuildLookup();
			SceneManager.sceneLoaded += HandleSceneLoaded;
		}

		private void Start()
		{
			ApplySceneMusic(SceneManager.GetActiveScene().name);
		}

		private void OnDestroy()
		{
			SceneManager.sceneLoaded -= HandleSceneLoaded;
		}

		private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, LoadSceneMode mode)
		{
			EnsureAudioListener();
			if (silenceDuringIgnoredScenes && IsIgnoredScene(scene.name))
			{
				StopMusic();
				return;
			}
			ApplySceneMusic(scene.name);
		}

		private void EnsureAudioListener()
		{
			AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			bool hasOtherActiveListener = false;
			foreach (AudioListener listener in listeners)
			{
				if (listener != null && listener != _fallbackAudioListener && listener.isActiveAndEnabled)
				{
					hasOtherActiveListener = true;
					break;
				}
			}

			if (_fallbackAudioListener == null && !hasOtherActiveListener)
			{
				_fallbackAudioListener = gameObject.AddComponent<AudioListener>();
			}

			if (_fallbackAudioListener != null)
			{
				_fallbackAudioListener.enabled = !hasOtherActiveListener;
			}
		}

		public void ApplySceneMusic(string sceneName)
		{
			if (string.IsNullOrWhiteSpace(sceneName) || IsIgnoredScene(sceneName))
			{
				return;
			}

			if (_entries.TryGetValue(sceneName, out SceneBgmEntry entry) && entry.Clip != null)
			{
				PlayMusic(entry.Clip, entry.Volume);
			}
			else if (stopMusicWhenSceneHasNoEntry)
			{
				StopMusic();
			}
		}

		public void PlayMusic(AudioClip clip)
		{
			PlayMusic(clip, defaultVolume);
		}

		public void PlayMusic(AudioClip clip, float volume)
		{
			if (clip == null)
			{
				return;
			}

			float targetVolume = Mathf.Clamp01(volume);
			if (_audioSource.clip == clip && _audioSource.isPlaying)
			{
				_audioSource.volume = targetVolume;
				return;
			}

			StartFade(ChangeMusicRoutine(clip, targetVolume));
		}

		public void StopMusic()
		{
			StartFade(StopMusicRoutine());
		}

		private IEnumerator ChangeMusicRoutine(AudioClip clip, float targetVolume)
		{
			if (_audioSource.isPlaying && fadeDuration > 0f)
			{
				yield return FadeVolume(_audioSource.volume, 0f);
			}

			_audioSource.Stop();
			_audioSource.clip = clip;
			_audioSource.volume = fadeDuration > 0f ? 0f : targetVolume;
			_audioSource.Play();

			if (fadeDuration > 0f)
			{
				yield return FadeVolume(0f, targetVolume);
			}
			else
			{
				_audioSource.volume = targetVolume;
			}

			_fadeCoroutine = null;
		}

		private IEnumerator StopMusicRoutine()
		{
			if (_audioSource.isPlaying && fadeDuration > 0f)
			{
				yield return FadeVolume(_audioSource.volume, 0f);
			}

			_audioSource.Stop();
			_audioSource.clip = null;
			_fadeCoroutine = null;
		}

		private IEnumerator FadeVolume(float from, float to)
		{
			float elapsed = 0f;
			while (elapsed < fadeDuration)
			{
				elapsed += Time.unscaledDeltaTime;
				_audioSource.volume = Mathf.Lerp(from, to, elapsed / fadeDuration);
				yield return null;
			}
			_audioSource.volume = to;
		}

		private void StartFade(IEnumerator routine)
		{
			if (_fadeCoroutine != null)
			{
				StopCoroutine(_fadeCoroutine);
			}
			_fadeCoroutine = StartCoroutine(routine);
		}

		private void BuildLookup()
		{
			_entries.Clear();
			if (sceneMusic == null)
			{
				return;
			}

			foreach (SceneBgmEntry entry in sceneMusic)
			{
				if (entry != null && !string.IsNullOrWhiteSpace(entry.SceneName))
				{
					_entries[entry.SceneName] = entry;
				}
			}
		}

		private bool IsIgnoredScene(string sceneName)
		{
			if (ignoredSceneNames == null)
			{
				return false;
			}

			foreach (string ignoredSceneName in ignoredSceneNames)
			{
				if (string.Equals(sceneName, ignoredSceneName, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}
	}
}
