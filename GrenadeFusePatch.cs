using System.Collections;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using UnityEngine;

namespace Scream100.Client
{
    internal sealed class ScreamFuseController : MonoBehaviour
    {
        private float _elapsedBeforeSpawn;
        private bool _initialized;

        internal void Initialize(float elapsedBeforeSpawn)
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _elapsedBeforeSpawn = Mathf.Max(0f, elapsedBeforeSpawn);
            StartCoroutine(PlayAfterDelay());
        }

        private IEnumerator PlayAfterDelay()
        {
            float delay = Mathf.Max(0f, Scream100Constants.ScreamStartsAt - _elapsedBeforeSpawn);
            if (delay > 0f)
            {
                yield return new WaitForSeconds(delay);
            }

            AudioClip clip = ScreamAudio.GetOrLoad();
            if (clip == null || gameObject == null)
            {
                Plugin.Log?.LogError("A Scream 100 reached its scream point, but the audio clip was unavailable.");
                yield break;
            }

            ScreamSparkEffect.Spawn(transform, clip.length);
            BetterSource voice = ScreamAudio.PlayThroughTarkov(transform.position);
            if (voice == null)
            {
                ScreamAudio.PlayFallback(transform.position);
                yield break;
            }

            // Give Unity two audio frames to start the pooled source. If Tarkov reports
            // a borrowed source but silently drops the clip, use a persistent world-mixer source.
            yield return null;
            yield return null;

            AudioSource unitySource = voice.source1;
            if (unitySource == null || !unitySource.isPlaying)
            {
                Plugin.Log?.LogWarning(
                    "Tarkov's pooled source did not start Scream100.wav (state="
                    + voice.PlayBackState
                    + "). Falling back.");
                voice.Release();
                ScreamAudio.PlayFallback(transform.position);
                yield break;
            }

            Plugin.Log?.LogInfo(
                "Scream 100 playback confirmed through Tarkov's Weaponry pool (clip="
                + (unitySource.clip != null ? unitySource.clip.name : "one-shot")
                + ", state="
                + voice.PlayBackState
                + ").");
        }
    }

    [HarmonyPatch(typeof(ThrowWeap), "get_GetExplDelay")]
    internal static class ScreamFuseLengthPatch
    {
        private static void Postfix(ThrowWeap __instance, ref float __result)
        {
            if (__instance != null &&
                __instance.TemplateId.ToString() == Scream100Constants.TemplateId)
            {
                __result = Scream100Constants.TotalFuseSeconds;
            }
        }
    }

    [HarmonyPatch(typeof(Grenade), nameof(Grenade.StartTimer))]
    internal static class GrenadeStartTimerPatch
    {
        private static void Postfix(Grenade __instance)
        {
            if (__instance == null || __instance.WeaponSource == null ||
                __instance.WeaponSource.TemplateId.ToString() != Scream100Constants.TemplateId)
            {
                return;
            }

            Scream100Visuals.Apply(__instance.gameObject);
            ScreamFuseController controller = __instance.GetComponent<ScreamFuseController>();
            if (controller == null)
            {
                controller = __instance.gameObject.AddComponent<ScreamFuseController>();
            }

            controller.Initialize(0f);
        }
    }

}
