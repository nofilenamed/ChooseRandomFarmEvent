using System;

using Netcode;

using StardewModdingAPI;

using StardewValley.Events;

namespace ChooseRandomFarmEvent
{
    public class FarmEventPatch
    {
        private static IMonitor Monitor;
        private static IModHelper Helper;

        public static void Initialize(IMonitor monitor, IModHelper helper)
        {
            Monitor = monitor;
            Helper = helper;
        }

        public static FarmEvent PickFarmEvent_PostFix(FarmEvent result)
        {
            Monitor.Log("Attempting to apply Harmony postfix to Utility.pickFarmEvent()", LogLevel.Trace);
            try
            {
                if (ModEntry.eventData != null)
                {
                    if (ModEntry.Config.EnforceEventConditions && (result is WorldChangeEvent || (result is SoundInTheNightEvent ev && Helper.Reflection.GetField<NetInt>(ev, "behavior").GetValue().Value == 4)))
                    {
                        Monitor.Log("World change event has overriden player-provided event.", LogLevel.Debug);
                        return result;
                    }
                    if (ModEntry.Config.EnforceEventConditions && !ModEntry.eventData.EnforceEventConditions(out string reason))
                    {
                        Monitor.Log($"Player-provided event {ModEntry.eventData.Name} could not run because {reason}.", LogLevel.Debug);
                        return result;
                    }
                    if (ModEntry.eventData.FarmEvent != null)
                        return ModEntry.eventData.FarmEvent;
                    if (ModEntry.eventData.PersonalFarmEvent != null)
                        return null;
                }
                return result;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(PickFarmEvent_PostFix)}:\n{ex}", LogLevel.Error);
                return result;
            }

        }
        public static FarmEvent PickPersonalFarmEvent_PostFix(FarmEvent result)
        {
            Monitor.Log("Attempting to apply Harmony postfix to Utility.pickPersonalFarmEvent()", LogLevel.Trace);
            try
            {
                if (ModEntry.eventData != null && ModEntry.eventData.PersonalFarmEvent != null && (!ModEntry.Config.EnforceEventConditions || ModEntry.eventData.EnforceEventConditions(out _)))
                {
                    if (ModEntry.Config.EnforceEventConditions && (result is BirthingEvent || result is PlayerCoupleBirthingEvent))
                    {
                        Monitor.Log("Birth event has overriden player-provided event.", LogLevel.Debug);
                        return result;
                    }
                    return ModEntry.eventData.PersonalFarmEvent;
                }
                return result;
            }
            catch (Exception ex)
            {
                Monitor.Log($"Failed in {nameof(PickPersonalFarmEvent_PostFix)}:\n{ex}", LogLevel.Error);
                return result;
            }

        }
    }
}