using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace Scream100.Client
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("EscapeFromTarkov.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.bensburnedwaffles.scream100.client";
        public const string PluginName = "Scream 100 Client";
        public const string PluginVersion = "1.0.8";

        internal static ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        private void Awake()
        {
            try
            {
                Log = Logger;
                ScreamAudio.Configure(Logger);
                _harmony = new Harmony(PluginGuid);
                _harmony.PatchAll(typeof(Plugin).Assembly);
                Logger.LogInfo("Scream 100 loaded: deferred persistent audio and harmless detonation are active.");
            }
            catch (Exception exception)
            {
                Logger.LogError("Scream 100 failed to initialize: " + exception);
            }
        }

        private void OnDestroy()
        {
            if (_harmony != null)
            {
                _harmony.UnpatchSelf();
            }

            ScreamAudio.Unload();
            Log = null;
        }
    }
}
