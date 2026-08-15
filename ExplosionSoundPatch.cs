using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using UnityEngine;
using EffectsSystem = Systems.Effects.Effects;

namespace Scream100.Client
{
    internal static class ScreamExplosionCameraShake
    {
        private const float ShakeRadius = 55f;

        internal static void Apply(Vector3 explosionPosition)
        {
            try
            {
                GameWorld world = Singleton<GameWorld>.Instance;
                Player player = world == null ? null : world.MainPlayer;
                if (player == null || player.ProceduralWeaponAnimation == null ||
                    player.ProceduralWeaponAnimation.ForceReact == null)
                {
                    return;
                }

                float distance = Vector3.Distance(player.Transform.position, explosionPosition);
                if (distance >= ShakeRadius)
                {
                    return;
                }

                float proximity = 1f - Mathf.Clamp01(distance / ShakeRadius);
                float hardImpulse = Mathf.Lerp(0.08f, 4.5f, proximity * proximity);
                float sustainedShake = Mathf.Lerp(0.12f, 3f, proximity);
                ForceEffector force = player.ProceduralWeaponAnimation.ForceReact;
                player.ProceduralWeaponAnimation.StartCoroutine(ApplyAftershocks(force, hardImpulse));
                player.ProceduralWeaponAnimation.StartCoroutine(force.GrenadeShake_CO(sustainedShake));
            }
            catch (Exception exception)
            {
                Plugin.Log?.LogWarning("Scream 100 camera shake could not be applied: " + exception.Message);
            }
        }

        private static IEnumerator ApplyAftershocks(ForceEffector force, float strength)
        {
            force.HardShake(strength);
            yield return new WaitForSeconds(0.07f);
            force.HardShake(strength * 0.65f);
            yield return new WaitForSeconds(0.10f);
            force.HardShake(strength * 0.35f);
        }
    }

    internal static class ScreamExplosionSoundGate
    {
        private struct PendingExplosion
        {
            internal Vector3 Position;
            internal float ExpiresAt;
        }

        private static readonly List<PendingExplosion> Pending = new List<PendingExplosion>();

        internal static void Register(Vector3 position)
        {
            Pending.Add(new PendingExplosion
            {
                Position = position,
                ExpiresAt = Time.realtimeSinceStartup + 1f,
            });
        }

        internal static bool ShouldSuppress(Vector3 position)
        {
            float now = Time.realtimeSinceStartup;
            bool matched = false;
            for (int index = Pending.Count - 1; index >= 0; index--)
            {
                PendingExplosion pending = Pending[index];
                if (pending.ExpiresAt < now)
                {
                    Pending.RemoveAt(index);
                    continue;
                }

                if ((pending.Position - position).sqrMagnitude <= 0.25f)
                {
                    matched = true;
                }
            }

            return matched;
        }
    }

    [HarmonyPatch]
    internal static class ScreamGrenadeExplosionPatch
    {
        private static MethodBase TargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(Grenade), nameof(Grenade.Explosion), Type.EmptyTypes);
        }

        private static void Prefix(Grenade __instance)
        {
            if (__instance == null || __instance.WeaponSource == null ||
                __instance.WeaponSource.TemplateId.ToString() != Scream100Constants.TemplateId)
            {
                return;
            }

            Vector3 position = __instance.transform.position;
            ScreamExplosionCameraShake.Apply(position);
            if (ScreamAudio.PlayExplosionAt(position))
            {
                ScreamExplosionSoundGate.Register(position);
            }
        }
    }

    [HarmonyPatch(typeof(EffectsSystem.Effect), nameof(EffectsSystem.Effect.PlaySound))]
    internal static class ScreamVanillaExplosionSoundPatch
    {
        private static bool Prefix(Vector3 position)
        {
            return !ScreamExplosionSoundGate.ShouldSuppress(position);
        }
    }
}
