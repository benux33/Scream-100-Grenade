using System;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using Comfort.Common;
using UnityEngine;

namespace Scream100.Client
{
    internal static class ScreamAudio
    {
        private static AudioClip _screamClip;
        private static AudioClip _explosionClip;
        private static ManualLogSource _log;
        private static string _screamPath;
        private static string _explosionPath;

        internal static void Configure(ManualLogSource log)
        {
            _log = log;
            string pluginFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _screamPath = Path.Combine(pluginFolder ?? string.Empty, "Scream100.wav");
            _explosionPath = Path.Combine(pluginFolder ?? string.Empty, "Explosion.wav");
            if (!File.Exists(_screamPath))
            {
                log.LogError("Missing Scream 100 sound: " + _screamPath);
            }
            if (!File.Exists(_explosionPath))
            {
                log.LogError("Missing Scream 100 explosion sound: " + _explosionPath);
            }

            log.LogInfo("Found the Scream 100 audio files. They will be loaded after Tarkov's audio system is ready.");
        }

        internal static AudioClip GetOrLoad()
        {
            return GetOrLoad(ref _screamClip, _screamPath, "Scream100.wav");
        }

        private static AudioClip GetExplosionOrLoad()
        {
            return GetOrLoad(ref _explosionClip, _explosionPath, "Explosion.wav");
        }

        private static AudioClip GetOrLoad(ref AudioClip clip, string path, string displayName)
        {
            if (clip != null && clip.length > 0f && clip.loadState != AudioDataLoadState.Failed)
            {
                return clip;
            }

            if (clip != null)
            {
                UnityEngine.Object.Destroy(clip);
                clip = null;
            }

            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                _log?.LogError(displayName + " was unavailable when the grenade needed it.");
                return null;
            }

            try
            {
                AudioClip loaded = WavLoader.Load(path);
                loaded.hideFlags = HideFlags.DontUnloadUnusedAsset;
                UnityEngine.Object.DontDestroyOnLoad(loaded);

                if (loaded.length <= 0f || loaded.loadState == AudioDataLoadState.Failed)
                {
                    UnityEngine.Object.Destroy(loaded);
                    _log?.LogError("Unity created an invalid " + displayName + " clip.");
                    return null;
                }

                clip = loaded;
                _log?.LogInfo(
                    "Loaded persistent "
                    + displayName
                    + " on first use ("
                    + clip.length.ToString("0.00")
                    + " seconds, state="
                    + clip.loadState
                    + ").");
                return clip;
            }
            catch (Exception exception)
            {
                _log?.LogError(displayName + " could not be loaded on first use: " + exception);
                return null;
            }
        }

        internal static void Unload()
        {
            if (_screamClip != null)
            {
                UnityEngine.Object.Destroy(_screamClip);
                _screamClip = null;
            }
            if (_explosionClip != null)
            {
                UnityEngine.Object.Destroy(_explosionClip);
                _explosionClip = null;
            }

            _screamPath = null;
            _explosionPath = null;
            _log = null;
        }

        internal static BetterSource PlayThroughTarkov(Vector3 position)
        {
            AudioClip clip = GetOrLoad();
            if (clip == null)
            {
                return null;
            }

            try
            {
                BetterAudio audio = Singleton<BetterAudio>.Instance;
                BetterSource source = audio.PlayAtPoint(
                    position,
                    clip,
                    BetterAudio.AudioSourceGroupType.Weaponry,
                    260,
                    1.35f,
                    EOcclusionTest.None,
                    null,
                    true,
                    false,
                    true,
                    false);

                if (source == null)
                {
                    Plugin.Log?.LogWarning("Tarkov's audio pool could not provide a source for the Scream 100.");
                    return null;
                }

                return source;
            }
            catch (Exception exception)
            {
                Plugin.Log?.LogError("Scream 100 audio playback failed: " + exception);
                return null;
            }
        }

        internal static AudioSource PlayFallback(Vector3 position)
        {
            AudioClip clip = GetOrLoad();
            if (clip == null)
            {
                return null;
            }

            return PlayOnWorldMixer(position, clip, "Scream 100 emergency spatial speaker", true);
        }

        internal static bool PlayExplosionAt(Vector3 position)
        {
            AudioClip clip = GetExplosionOrLoad();
            if (clip == null)
            {
                return false;
            }

            return PlayOnWorldMixer(position, clip, "Scream 100 custom explosion", false) != null;
        }

        private static AudioSource PlayOnWorldMixer(
            Vector3 position,
            AudioClip clip,
            string objectName,
            bool logFallback)
        {

            try
            {
                GameObject speaker = new GameObject(objectName);
                speaker.transform.position = position;
                UnityEngine.Object.DontDestroyOnLoad(speaker);

                AudioSource source = speaker.AddComponent<AudioSource>();
                source.clip = clip;
                source.playOnAwake = false;
                source.loop = false;
                source.spatialBlend = 1f;
                source.dopplerLevel = 0f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.minDistance = 4f;
                source.maxDistance = 260f;
                source.priority = 64;
                source.volume = 1f;

                BetterAudio audio = Singleton<BetterAudio>.Instance;
                if (audio != null && audio.WorldMixer != null)
                {
                    source.outputAudioMixerGroup = audio.WorldMixer;
                }

                source.Play();
                UnityEngine.Object.Destroy(speaker, clip.length + 0.5f);
                if (logFallback)
                {
                    _log?.LogWarning("Used the direct Tarkov world-mixer fallback for the Scream 100 sound.");
                }
                return source;
            }
            catch (Exception exception)
            {
                _log?.LogError("Scream 100 fallback playback failed: " + exception);
                return null;
            }
        }
    }
}
