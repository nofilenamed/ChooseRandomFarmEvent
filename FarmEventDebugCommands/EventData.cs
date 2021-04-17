using System;
using System.Collections.Generic;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Events;
using StardewValley.Locations;

namespace ChooseRandomFarmEvent
{
    internal class EventData
    {
        internal string Name { get; }
        internal List<KeyValuePair<Func<bool>, string>> Conditions { get; set; }
        internal FarmEvent FarmEvent { get; set; }
        internal FarmEvent PersonalFarmEvent { get; set; }
        internal string SuccessMessage { get; set; }
        internal string FailureMessage { get; set; }
        internal static List<string> EventTypes { get; } = new List<string>() { "capsule", "meteorite", "wild_animal_attack", "owl_statue", "fairy", "witch", 
            "NPC_child_request", "PC_child_request", "animal_birth" };

        private static IModHelper Helper;
        private static IMonitor Monitor;
        public static void Initialize(IMonitor monitor, IModHelper helper)
        {
            Monitor = monitor;
            Helper = helper;
        }

        internal EventData(string n)
        {
            Name = n;
        }

        internal void SetUp()
        {
            FailureMessage = $"Could not set tonight's event to {Name}: a condition for this event has not been fulfilled, or another event is taking precedence.";
            Conditions = new List<KeyValuePair<Func<bool>, string>>();
            AddCondition(() => !Game1.weddingToday, "there's a wedding today");
            //Conditions.Add(() => !(Game1.stats.DaysPlayed == 31));
            switch (Name)
            {
                case "capsule":
                    FarmEvent = new SoundInTheNightEvent(0);
                    PersonalFarmEvent = null;
                    SuccessMessage = "A strange capsule event will occur tonight.";
                    AddCondition(() => Game1.year > 1, 
                        "the game year is 1");
                    AddCondition(() => !Game1.MasterPlayer.mailReceived.Contains("Got_Capsule"), 
                        "the strange capsule event has happened before");
                    break;

                case "meteorite":
                    FarmEvent = new SoundInTheNightEvent(1);
                    PersonalFarmEvent = null;
                    SuccessMessage = "A meteorite event will occur tonight.";
                    break;

                case "wild_animal_attack":
                    FarmEvent = null;
                    PersonalFarmEvent = new SoundInTheNightEvent(2);
                    SuccessMessage = "A wild animal attack event will occur tonight.";
                    break;

                case "owl_statue":
                    FarmEvent = new SoundInTheNightEvent(3);
                    PersonalFarmEvent = null;
                    SuccessMessage = "An owl statue event will occur tonight.";
                    break;

                case "fairy":
                    FarmEvent = new FairyEvent();
                    PersonalFarmEvent = null;
                    SuccessMessage = "A crop fairy event will occur tonight.";
                    AddCondition(() => !Game1.currentSeason.Equals("winter"), "it's winter");
                    break;

                case "witch":
                    FarmEvent = new WitchEvent();
                    PersonalFarmEvent = null;
                    SuccessMessage = "A witch event will occur tonight.";
                    break;

                case "NPC_child_request":
                    FarmEvent = null;
                    PersonalFarmEvent = new QuestionEvent(1);
                    SuccessMessage = "Your NPC spouse will request a child tonight.";
                    AddCondition(() => Game1.player.isMarried(), 
                        "you are not married");
                    AddCondition(() => Game1.player.spouse != null, 
                        "you do not have a spouse");
                    AddCondition(() => Game1.getCharacterFromName(Game1.player.spouse).canGetPregnant(), 
                        "your spouse cannot have children (or is a roommate)");
                    AddCondition(() => Game1.player.currentLocation == Game1.getLocationFromName(Game1.player.homeLocation), 
                        "you are not at home");
                    break;

                case "PC_child_request":
                    FarmEvent = null;
                    PersonalFarmEvent = new QuestionEvent(3);
                    SuccessMessage = "You or your PC spouse will request a child tonight.";
                    AddCondition(() => Context.IsMultiplayer, "this is a single-player game");
                    AddCondition(() => Game1.player.isMarried(), "you are not married");
                    AddCondition(() => Game1.player.team.GetSpouse(Game1.player.UniqueMultiplayerID).HasValue, 
                        "you are not married to another farmer");
                    AddCondition(() => Game1.player.GetSpouseFriendship().NextBirthingDate == null, 
                        "you are already going to have a child");
                    AddCondition(() => Game1.otherFarmers.ContainsKey(Game1.player.team.GetSpouse(Game1.player.UniqueMultiplayerID).Value), 
                        "your spouse is not in the game");
                    AddCondition(() => Game1.otherFarmers[Game1.player.team.GetSpouse(Game1.player.UniqueMultiplayerID).Value].currentLocation == Game1.player.currentLocation, 
                        "you and your spouse are not in the same location");
                    AddCondition(() => Game1.otherFarmers[Game1.player.team.GetSpouse(Game1.player.UniqueMultiplayerID).Value].currentLocation == Game1.getLocationFromName(Game1.otherFarmers[Game1.player.team.GetSpouse(Game1.player.UniqueMultiplayerID).Value].homeLocation) ||
                        Game1.otherFarmers[Game1.player.team.GetSpouse(Game1.player.UniqueMultiplayerID).Value].currentLocation == Game1.getLocationFromName(Game1.player.homeLocation), 
                        "you and your spouse are not in either of your houses");
                    AddCondition(() => Helper.Reflection.GetMethod(typeof(Utility), "playersCanGetPregnantHere").Invoke<bool>(Game1.otherFarmers[Game1.player.team.GetSpouse(Game1.player.UniqueMultiplayerID).Value].currentLocation as FarmHouse), 
                        "you don't have a crib, your crib is already occupied, or you already have 2 children");
                    break;

                case "animal_birth":
                    FarmEvent = null;
                    PersonalFarmEvent = new QuestionEvent(2);
                    SuccessMessage = "An animal birth event will occur tonight.";
                    break;

                default:
                    break;

            }
        }

        internal bool EnforceEventConditions(out string message)
        {
            message = "";
            bool fulfillConditions = true;
            foreach (var condition in Conditions)
            {
                if (!condition.Key.Invoke())
                {
                    message = condition.Value;
                    fulfillConditions = false;
                    break;
                }
            }
            return fulfillConditions;
        }

        private void AddCondition(Func<bool> condition, string failureReason)
        {
            Conditions.Add(new KeyValuePair<Func<bool>, string>(condition, failureReason));
        }
    }

}