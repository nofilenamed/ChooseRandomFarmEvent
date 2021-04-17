using System;
using System.Collections.Generic;
using System.Text;
using Harmony;
using Microsoft.Xna.Framework;
using Netcode;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Events;
using StardewValley.TerrainFeatures;

namespace ChooseRandomFarmEvent
{

    /// <summary>The mod entry point.</summary>
    public class ModEntry : Mod
    {
        internal static EventData eventData;
        internal static ModConfig Config;

        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = helper.ReadConfig<ModConfig>();
            FarmEventPatch.Initialize(Monitor, helper);
            EventData.Initialize(Monitor, helper);

            helper.ConsoleCommands.Add("set_farmevent", "Sets tonight's farm event.\n\n" +
                "Usage: set_farmevent <event>\n- event: the type of event.\n\n" +
                "Valid events: " + String.Join(", ", EventData.EventTypes), this.SetFarmEvent);

            helper.Events.GameLoop.DayStarted += OnDayStarted;

            var harmony = HarmonyInstance.Create(this.ModManifest.UniqueID);

            harmony.Patch(
               original: AccessTools.Method(typeof(StardewValley.Utility), nameof(StardewValley.Utility.pickFarmEvent)),
               postfix: new HarmonyMethod(typeof(FarmEventPatch), nameof(FarmEventPatch.PickFarmEvent_PostFix))
            );
            harmony.Patch(
               original: AccessTools.Method(typeof(StardewValley.Utility), nameof(StardewValley.Utility.pickPersonalFarmEvent)),
               postfix: new HarmonyMethod(typeof(FarmEventPatch), nameof(FarmEventPatch.PickPersonalFarmEvent_PostFix))
            );
        }

        private void OnDayStarted(object sender, DayStartedEventArgs e)
        {
            eventData = null;
        }

        private void SetFarmEvent(string command, string[] args)
        {
            if (args.Length != 1)
            {
                Monitor.Log("Command requires exactly one argument. For more info, type: help set_farmevent", LogLevel.Info);
                return;
            }
            string eventType = args[0];
            if (!EventData.EventTypes.Contains(eventType))
            {
                Monitor.Log($"{eventType} is not a valid event type.\n Valid event types: {String.Join(", ", EventData.EventTypes)}", LogLevel.Info);
                return;
            }
            eventData = new EventData(eventType);
            eventData.SetUp();
            
            if (Config.EnforceEventConditions && !eventData.EnforceEventConditions(out string reason))
                Monitor.Log($"Under current game conditions, the event \"{eventData.Name}\" will not be able to run tonight because {reason}.\n" +
                    $"The event will still try to run tonight and check if this condition has changed by the time the players go to bed.", LogLevel.Info);
            else
                Monitor.Log(eventData.SuccessMessage, LogLevel.Info);
        }

    }

    public class FarmEventPatch
    {
        private static IMonitor Monitor;
        private static IModHelper Helper;

        public static void Initialize(IMonitor monitor, IModHelper helper)
        {
            Monitor = monitor;
            Helper = helper;
        }
        public static FarmEvent PickFarmEvent_PostFix(FarmEvent __result)
        {
            Monitor.Log("Attempting to apply Harmony postfix to Utility.pickFarmEvent()", LogLevel.Trace);
            try
            {
                if (ModEntry.eventData != null)
                {
                    if (ModEntry.Config.EnforceEventConditions && (__result is WorldChangeEvent ||
                       ( __result is SoundInTheNightEvent ev && Helper.Reflection.GetField<NetInt>(ev, "behavior").GetValue() == 4)))
                    {
                        Monitor.Log("World change event has overriden player-provided event.", LogLevel.Debug);
                        return __result;
                    }
                    if (ModEntry.Config.EnforceEventConditions && !ModEntry.eventData.EnforceEventConditions(out string reason))
                    {
                        Monitor.Log($"Player-provided event {ModEntry.eventData.Name} could not run because {reason}.", LogLevel.Debug);
                        return __result;
                    }
                    if (ModEntry.eventData.FarmEvent != null)
                        return ModEntry.eventData.FarmEvent;
                    if (ModEntry.eventData.PersonalFarmEvent != null)
                        return null;
                }
                return __result;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(PickFarmEvent_PostFix)}:\n{ex}", LogLevel.Error);
                return __result;
            }

        }
        public static FarmEvent PickPersonalFarmEvent_PostFix(FarmEvent __result)
        {
            Monitor.Log("Attempting to apply Harmony postfix to Utility.pickPersonalFarmEvent()", LogLevel.Trace);
            try
            {
                if (ModEntry.eventData != null 
                    && ModEntry.eventData.PersonalFarmEvent != null
                    && (!ModEntry.Config.EnforceEventConditions
                    || ModEntry.eventData.EnforceEventConditions(out _)))
                {
                    if (ModEntry.Config.EnforceEventConditions && 
                        (__result is BirthingEvent || __result is PlayerCoupleBirthingEvent))
                    {
                        Monitor.Log("Birth event has overriden player-provided event.", LogLevel.Debug);
                        return __result;
                    }
                    return ModEntry.eventData.PersonalFarmEvent;
                }
                return __result;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(PickPersonalFarmEvent_PostFix)}:\n{ex}", LogLevel.Error);
                return __result;
            }

        }
    }

    public class ModConfig
    {
        public bool EnforceEventConditions { get; set; } = true;
    }
}