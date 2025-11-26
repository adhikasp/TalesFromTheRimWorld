using System.Collections.Generic;
using System.Linq;

// Use shared interfaces from main project
using AINarrator;

namespace AINarrator.Test
{
    /// <summary>
    /// Mock colony scenarios for testing LLM narration at different game stages.
    /// </summary>
    public static class MockScenarios
    {
        /// <summary>
        /// Early game: 3 colonists, just crashed, minimal resources.
        /// </summary>
        public static MockColonyContext GetEarlyGameScenario()
        {
            return new MockColonyContext
            {
                ColonyName = "New Hope",
                ColonyAgeDays = 3,
                Season = "spring",
                Biome = "temperate forest",
                Quadrum = "Aprimay",
                Year = 5500,
                WealthTotal = 8500,
                ColonistCount = 3,
                PrisonerCount = 0,

                Colonists = new List<MockColonist>
                {
                    new MockColonist
                    {
                        Name = "Marcus 'Trigger' Hayes",
                        ShortName = "Marcus",
                        Gender = "male",
                        Age = 34,
                        Role = "shooter",
                        ChildhoodBackstory = "Child soldier",
                        AdulthoodBackstory = "Marine",
                        Traits = new List<string> { "Tough", "Trigger-happy", "Night owl" },
                        HealthPercent = 100,
                        HealthStatus = "healthy",
                        MoodPercent = 65,
                        MentalState = "stable",
                        TopSkills = new List<string> { "Shooting 12 (passionate)", "Melee 8", "Construction 5" },
                        Relationships = new List<string>(),
                        CurrentActivity = "Building wooden wall"
                    },
                    new MockColonist
                    {
                        Name = "Dr. Elena Vasquez",
                        ShortName = "Elena",
                        Gender = "female",
                        Age = 42,
                        Role = "medic",
                        ChildhoodBackstory = "Urbworld doctor's kid",
                        AdulthoodBackstory = "Surgeon",
                        Traits = new List<string> { "Kind", "Careful shooter", "Neurotic" },
                        HealthPercent = 85,
                        HealthStatus = "injured",
                        Injuries = new List<string> { "Bruise on left leg" },
                        MoodPercent = 48,
                        MentalState = "stressed",
                        TopSkills = new List<string> { "Medicine 15 (passionate)", "Intellectual 10", "Social 7" },
                        Relationships = new List<string>(),
                        CurrentActivity = "Tending to own wounds"
                    },
                    new MockColonist
                    {
                        Name = "Jake 'Farmer' Wilson",
                        ShortName = "Jake",
                        Gender = "male",
                        Age = 28,
                        Role = "farmer",
                        ChildhoodBackstory = "Farm kid",
                        AdulthoodBackstory = "Botanist",
                        Traits = new List<string> { "Industrious", "Ascetic", "Too smart" },
                        HealthPercent = 100,
                        HealthStatus = "healthy",
                        MoodPercent = 72,
                        MentalState = "stable",
                        TopSkills = new List<string> { "Plants 14 (passionate)", "Animals 9", "Cooking 8 (interested)" },
                        Relationships = new List<string>(),
                        CurrentActivity = "Sowing rice in growing zone"
                    }
                },

                Resources = new Dictionary<string, string>
                {
                    { "Silver", "450" },
                    { "Steel", "280" },
                    { "Wood", "620" },
                    { "Food", "4.2 days worth" },
                    { "Medicine", "8" },
                    { "Components", "12" }
                },

                FactionRelations = new List<MockFactionRelation>
                {
                    new MockFactionRelation { Name = "The Tribe of Running Waters", FactionType = "tribal", Goodwill = 0, IsHostile = false, RelationType = "neutral" },
                    new MockFactionRelation { Name = "The Forsaken Raiders", FactionType = "pirate", Goodwill = -100, IsHostile = true, RelationType = "hostile" },
                    new MockFactionRelation { Name = "Proxima Trading Company", FactionType = "outlander civil", Goodwill = 10, IsHostile = false, RelationType = "neutral" }
                },

                Environment = new MockEnvironment
                {
                    Weather = "clear",
                    TimeOfDay = "morning",
                    Temperature = "18°C (comfortable)",
                    ActiveConditions = new List<string>()
                },

                RecentEvents = new List<string>
                {
                    "Crash landing - Day 1",
                    "Established basic shelter"
                },

                ActiveThreats = new List<string>(),
                DeathRecords = new List<string>(),
                BattleHistory = new List<string>(),
                Prisoners = new List<MockPrisoner>(),
                Animals = new List<string>(),
                NotableItems = new List<string>(),

                Infrastructure = new MockInfrastructure
                {
                    HospitalBeds = 0,
                    Turrets = 0,
                    Mortars = 0,
                    PowerGeneration = 0,
                    ResearchCompleted = "none"
                }
            };
        }

        /// <summary>
        /// Mid game: 8 colonists, established base, some history.
        /// </summary>
        public static MockColonyContext GetMidGameScenario()
        {
            return new MockColonyContext
            {
                ColonyName = "Ironhold",
                ColonyAgeDays = 45,
                Season = "summer",
                Biome = "arid shrubland",
                Quadrum = "Jugust",
                Year = 5500,
                WealthTotal = 85000,
                ColonistCount = 8,
                PrisonerCount = 2,

                Colonists = new List<MockColonist>
                {
                    new MockColonist
                    {
                        Name = "Marcus 'Trigger' Hayes",
                        ShortName = "Marcus",
                        Gender = "male",
                        Age = 34,
                        Role = "shooter",
                        Traits = new List<string> { "Tough", "Trigger-happy", "Night owl" },
                        HealthPercent = 78,
                        HealthStatus = "injured",
                        Injuries = new List<string> { "Scar on torso", "Missing left pinky" },
                        MoodPercent = 58,
                        MentalState = "stable",
                        TopSkills = new List<string> { "Shooting 14 (passionate)", "Melee 10", "Construction 7" },
                        Relationships = new List<string> { "rival of Chen", "friend with Elena" },
                        CurrentActivity = "Patrolling perimeter"
                    },
                    new MockColonist
                    {
                        Name = "Dr. Elena Vasquez",
                        ShortName = "Elena",
                        Gender = "female",
                        Age = 42,
                        Role = "medic",
                        Traits = new List<string> { "Kind", "Careful shooter", "Neurotic" },
                        HealthPercent = 100,
                        HealthStatus = "healthy",
                        MoodPercent = 75,
                        MentalState = "stable",
                        TopSkills = new List<string> { "Medicine 17 (passionate)", "Intellectual 12", "Social 9" },
                        Relationships = new List<string> { "lover of Jake", "friend with Marcus" },
                        CurrentActivity = "Researching gun turrets"
                    },
                    new MockColonist
                    {
                        Name = "Jake 'Farmer' Wilson",
                        ShortName = "Jake",
                        Gender = "male",
                        Age = 28,
                        Role = "farmer",
                        Traits = new List<string> { "Industrious", "Ascetic", "Too smart" },
                        HealthPercent = 100,
                        HealthStatus = "healthy",
                        MoodPercent = 82,
                        MentalState = "stable",
                        Inspiration = "inspired creativity",
                        TopSkills = new List<string> { "Plants 16 (passionate)", "Animals 12", "Cooking 11 (interested)" },
                        Relationships = new List<string> { "lover of Elena", "close friend with Lily" },
                        CurrentActivity = "Harvesting devilstrand"
                    },
                    new MockColonist
                    {
                        Name = "Chen Wei",
                        ShortName = "Chen",
                        Gender = "male",
                        Age = 52,
                        Role = "crafter",
                        Traits = new List<string> { "Pessimist", "Bloodlust", "Iron-willed" },
                        HealthPercent = 90,
                        HealthStatus = "healthy",
                        MoodPercent = 45,
                        MentalState = "stressed",
                        TopSkills = new List<string> { "Crafting 15 (passionate)", "Mining 10", "Melee 9" },
                        Relationships = new List<string> { "rival of Marcus", "hates prisoners" },
                        CurrentActivity = "Crafting assault rifle"
                    },
                    new MockColonist
                    {
                        Name = "Lily 'Patch' Monroe",
                        ShortName = "Lily",
                        Gender = "female",
                        Age = 24,
                        Role = "handler",
                        Traits = new List<string> { "Animal friend", "Nimble", "Wimp" },
                        HealthPercent = 100,
                        HealthStatus = "healthy",
                        MoodPercent = 88,
                        MentalState = "stable",
                        TopSkills = new List<string> { "Animals 14 (passionate)", "Medicine 8", "Plants 7" },
                        Relationships = new List<string> { "close friend with Jake", "bonded to husky Rex" },
                        CurrentActivity = "Training attack dog"
                    },
                    new MockColonist
                    {
                        Name = "Victor 'Boom' Kowalski",
                        ShortName = "Victor",
                        Gender = "male",
                        Age = 38,
                        Role = "builder",
                        Traits = new List<string> { "Pyromaniac", "Steadfast", "Sanguine" },
                        HealthPercent = 95,
                        HealthStatus = "healthy",
                        MoodPercent = 70,
                        MentalState = "stable",
                        TopSkills = new List<string> { "Construction 16 (passionate)", "Mining 12", "Shooting 6" },
                        Relationships = new List<string> { "friend with Marcus" },
                        CurrentActivity = "Building stone wall"
                    },
                    new MockColonist
                    {
                        Name = "Sarah Chen",
                        ShortName = "Sarah",
                        Gender = "female",
                        Age = 31,
                        Role = "negotiator",
                        Traits = new List<string> { "Beautiful", "Greedy", "Fast learner" },
                        HealthPercent = 100,
                        HealthStatus = "healthy",
                        MoodPercent = 62,
                        MentalState = "stable",
                        TopSkills = new List<string> { "Social 15 (passionate)", "Artistic 10", "Intellectual 8" },
                        Relationships = new List<string> { "daughter of Chen" },
                        CurrentActivity = "Recruiting prisoner"
                    },
                    new MockColonist
                    {
                        Name = "Brother Thomas",
                        ShortName = "Thomas",
                        Gender = "male",
                        Age = 55,
                        Role = "cook",
                        Traits = new List<string> { "Cannibal", "Psychopath", "Gourmand" },
                        HealthPercent = 85,
                        HealthStatus = "injured",
                        Injuries = new List<string> { "Frail" },
                        MoodPercent = 78,
                        MentalState = "stable",
                        TopSkills = new List<string> { "Cooking 18 (passionate)", "Melee 7", "Social 5" },
                        Relationships = new List<string>(),
                        CurrentActivity = "Preparing lavish meal"
                    }
                },

                Resources = new Dictionary<string, string>
                {
                    { "Silver", "4500" },
                    { "Gold", "120" },
                    { "Steel", "1850" },
                    { "Plasteel", "45" },
                    { "Wood", "2200" },
                    { "Food", "18.5 days worth" },
                    { "Medicine", "52" },
                    { "Components", "38" },
                    { "Chemfuel", "280" }
                },

                FactionRelations = new List<MockFactionRelation>
                {
                    new MockFactionRelation { Name = "The Tribe of Running Waters", FactionType = "tribal", Goodwill = 45, IsHostile = false, RelationType = "warm" },
                    new MockFactionRelation { Name = "The Forsaken Raiders", FactionType = "pirate", Goodwill = -100, IsHostile = true, RelationType = "hostile" },
                    new MockFactionRelation { Name = "Proxima Trading Company", FactionType = "outlander civil", Goodwill = 65, IsHostile = false, RelationType = "warm" },
                    new MockFactionRelation { Name = "The Mechanoid Hive", FactionType = "mechanoid", Goodwill = -100, IsHostile = true, RelationType = "hostile" },
                    new MockFactionRelation { Name = "Empire of Stellarch", FactionType = "empire", Goodwill = 20, IsHostile = false, RelationType = "neutral" }
                },

                Environment = new MockEnvironment
                {
                    Weather = "dry thunderstorm",
                    TimeOfDay = "afternoon",
                    Temperature = "38°C (hot)",
                    ActiveConditions = new List<string> { "Heat wave" }
                },

                RecentEvents = new List<string>
                {
                    "Raid by Forsaken Raiders - Day 38 (Victory)",
                    "Trade caravan from Proxima - Day 40",
                    "Psychic drone (male) - Day 42",
                    "Wanderer joined: Brother Thomas - Day 44"
                },

                RecentInteractions = new List<string>
                {
                    "Marcus insulted Chen during meal",
                    "Elena and Jake had a deep talk",
                    "Sarah successfully converted prisoner",
                    "Chen brooding after insult"
                },

                ActiveThreats = new List<string>(),

                DeathRecords = new List<string>
                {
                    "Rodrigo 'Swift' Martinez - Day 22 (killed by raider)"
                },

                BattleHistory = new List<string>
                {
                    "Day 15: Manhunter pack (squirrels) - no casualties",
                    "Day 22: Forsaken Raiders attack - 1 colonist killed (Rodrigo), 4 raiders killed",
                    "Day 38: Forsaken Raiders retaliation - Victory, 2 prisoners captured"
                },

                Prisoners = new List<MockPrisoner>
                {
                    new MockPrisoner { Name = "Slag", OriginalFaction = "Forsaken Raiders", HealthPercent = 65, RecruitDifficulty = "42", MoodPercent = 35 },
                    new MockPrisoner { Name = "Viper", OriginalFaction = "Forsaken Raiders", HealthPercent = 90, RecruitDifficulty = "78", MoodPercent = 42 }
                },

                Animals = new List<string>
                {
                    "Rex the husky (bonded to Lily) [obedient, attack-trained, hauls]",
                    "Shadow the wolf (bonded to Marcus) [obedient, attack-trained]",
                    "3 chickens",
                    "2 muffalo"
                },

                NotableItems = new List<string>
                {
                    "Masterwork assault rifle (wielded by Marcus)",
                    "Excellent sniper rifle",
                    "Persona monosword (stored)"
                },

                Infrastructure = new MockInfrastructure
                {
                    HospitalBeds = 4,
                    Turrets = 2,
                    Mortars = 1,
                    PowerGeneration = 3600,
                    ResearchCompleted = "Gun turrets"
                }
            };
        }

        /// <summary>
        /// Late game: 15 colonists, wealthy, complex history.
        /// </summary>
        public static MockColonyContext GetLateGameScenario()
        {
            return new MockColonyContext
            {
                ColonyName = "New Arcadia",
                ColonyAgeDays = 180,
                Season = "fall",
                Biome = "boreal forest",
                Quadrum = "Septober",
                Year = 5501,
                WealthTotal = 450000,
                ColonistCount = 15,
                PrisonerCount = 4,

                Colonists = new List<MockColonist>
                {
                    new MockColonist
                    {
                        Name = "Marcus 'Trigger' Hayes",
                        ShortName = "Marcus",
                        Gender = "male",
                        Age = 35,
                        Role = "shooter",
                        Traits = new List<string> { "Tough", "Trigger-happy", "Night owl" },
                        HealthPercent = 72,
                        HealthStatus = "injured",
                        Injuries = new List<string> { "Scar on torso", "Missing left pinky", "Bionic arm" },
                        MoodPercent = 68,
                        MentalState = "stable",
                        TopSkills = new List<string> { "Shooting 18 (passionate)", "Melee 14", "Construction 9" },
                        Relationships = new List<string> { "husband of newcomer Yuki", "father figure to Alex", "rival of Chen" },
                        CurrentActivity = "Manning mortar during raid"
                    },
                    new MockColonist
                    {
                        Name = "Dr. Elena Vasquez",
                        ShortName = "Elena",
                        Gender = "female",
                        Age = 43,
                        Role = "medic",
                        Traits = new List<string> { "Kind", "Careful shooter", "Neurotic" },
                        HealthPercent = 100,
                        HealthStatus = "healthy",
                        MoodPercent = 85,
                        MentalState = "stable",
                        TopSkills = new List<string> { "Medicine 20 (passionate)", "Intellectual 15", "Social 11" },
                        Relationships = new List<string> { "wife of Jake", "mother of baby Maria" },
                        CurrentActivity = "Emergency surgery on wounded colonist"
                    },
                    new MockColonist
                    {
                        Name = "Jake 'Farmer' Wilson",
                        ShortName = "Jake",
                        Gender = "male",
                        Age = 29,
                        Role = "farmer",
                        Traits = new List<string> { "Industrious", "Ascetic", "Too smart" },
                        HealthPercent = 65,
                        HealthStatus = "severely injured",
                        Injuries = new List<string> { "Gunshot wound torso", "Bleeding" },
                        MoodPercent = 42,
                        MentalState = "stressed",
                        TopSkills = new List<string> { "Plants 20 (passionate)", "Animals 15", "Cooking 14 (interested)" },
                        Relationships = new List<string> { "husband of Elena", "father of baby Maria" },
                        CurrentActivity = "Being rescued from battlefield"
                    }
                },

                Resources = new Dictionary<string, string>
                {
                    { "Silver", "45000" },
                    { "Gold", "1200" },
                    { "Steel", "8500" },
                    { "Plasteel", "850" },
                    { "Uranium", "120" },
                    { "Wood", "5500" },
                    { "Food", "45 days worth" },
                    { "Medicine", "180" },
                    { "Components", "95" },
                    { "Advanced Components", "22" },
                    { "Chemfuel", "1200" }
                },

                FactionRelations = new List<MockFactionRelation>
                {
                    new MockFactionRelation { Name = "The Tribe of Running Waters", FactionType = "tribal", Goodwill = 85, IsHostile = false, RelationType = "allied" },
                    new MockFactionRelation { Name = "The Forsaken Raiders", FactionType = "pirate", Goodwill = -100, IsHostile = true, RelationType = "hostile" },
                    new MockFactionRelation { Name = "Proxima Trading Company", FactionType = "outlander civil", Goodwill = 92, IsHostile = false, RelationType = "allied" },
                    new MockFactionRelation { Name = "The Mechanoid Hive", FactionType = "mechanoid", Goodwill = -100, IsHostile = true, RelationType = "hostile" },
                    new MockFactionRelation { Name = "Empire of Stellarch", FactionType = "empire", Goodwill = 75, IsHostile = false, RelationType = "allied" }
                },

                Environment = new MockEnvironment
                {
                    Weather = "foggy rain",
                    TimeOfDay = "evening",
                    Temperature = "8°C (cool)",
                    ActiveConditions = new List<string>()
                },

                RecentEvents = new List<string>
                {
                    "Major raid by Forsaken Raiders - IN PROGRESS",
                    "Trade caravan from Empire - Day 178",
                    "Marriage: Marcus and Yuki - Day 175",
                    "Birth: Baby Maria (Elena & Jake) - Day 170"
                },

                RecentInteractions = new List<string>
                {
                    "Elena crying over Jake's injuries",
                    "Marcus rallying defenders",
                    "Chen executing wounded raider",
                    "Yuki worried about Marcus on the front line"
                },

                ActiveThreats = new List<string>
                {
                    "32 hostile raiders (Forsaken Raiders) - major assault in progress",
                    "3 breachers with explosives",
                    "1 raider with doomsday rocket launcher"
                },

                DeathRecords = new List<string>
                {
                    "Rodrigo 'Swift' Martinez - Day 22 (killed by raider)",
                    "Old Ben - Day 85 (heart attack)",
                    "Whiskers the cat - Day 120 (killed by mechanoids)",
                    "Private Ryan - Day 145 (executed by Empire deserter)"
                },

                BattleHistory = new List<string>
                {
                    "Day 22: Forsaken Raiders - 1 colonist killed, 4 raiders killed",
                    "Day 65: Mechanoid cluster - destroyed with mortars",
                    "Day 100: Siege by pirates - Victory after 8 days",
                    "Day 145: Empire deserters - 1 colonist executed, all deserters killed",
                    "Day 180: Forsaken Raiders major assault - ONGOING"
                },

                Prisoners = new List<MockPrisoner>
                {
                    new MockPrisoner { Name = "Slag", OriginalFaction = "Forsaken Raiders", HealthPercent = 100, RecruitDifficulty = "12", MoodPercent = 65 },
                    new MockPrisoner { Name = "Dr. Hart", OriginalFaction = "Empire (deserter)", HealthPercent = 80, RecruitDifficulty = "35", MoodPercent = 50 },
                    new MockPrisoner { Name = "Bones", OriginalFaction = "Forsaken Raiders", HealthPercent = 45, RecruitDifficulty = "88", MoodPercent = 25 },
                    new MockPrisoner { Name = "Whisper", OriginalFaction = "Forsaken Raiders", HealthPercent = 70, RecruitDifficulty = "62", MoodPercent = 38 }
                },

                Animals = new List<string>
                {
                    "Rex the husky (bonded to Lily) [attack-trained, rescues]",
                    "Shadow the wolf (bonded to Marcus) [attack-trained]",
                    "Thunder the thrumbo (bonded to Jake)",
                    "8 huskies (trained haulers)",
                    "12 chickens",
                    "6 muffalo",
                    "2 boomalopes"
                },

                NotableItems = new List<string>
                {
                    "Legendary charge rifle (wielded by Marcus)",
                    "Masterwork marine armor (worn by Chen)",
                    "AI Persona Core (stored for ship)",
                    "Archotech eye on Elena",
                    "Persona monosword (wielded by Yuki)",
                    "Legendary devilstrand parka (worn by Jake)"
                },

                Infrastructure = new MockInfrastructure
                {
                    HospitalBeds = 12,
                    Turrets = 18,
                    Mortars = 6,
                    PowerGeneration = 28000,
                    ResearchCompleted = "Fabrication, charged shot"
                }
            };
        }

        /// <summary>
        /// Crisis scenario: Colony under severe stress.
        /// </summary>
        public static MockColonyContext GetCrisisScenario()
        {
            return new MockColonyContext
            {
                ColonyName = "Last Stand",
                ColonyAgeDays = 90,
                Season = "winter",
                Biome = "ice sheet",
                Quadrum = "Decembary",
                Year = 5500,
                WealthTotal = 35000,
                ColonistCount = 5,
                PrisonerCount = 0,

                Colonists = new List<MockColonist>
                {
                    new MockColonist
                    {
                        Name = "Commander Reyes",
                        ShortName = "Reyes",
                        Gender = "female",
                        Age = 45,
                        Role = "shooter",
                        Traits = new List<string> { "Iron-willed", "Tough", "Psychopath" },
                        HealthPercent = 55,
                        HealthStatus = "severely injured",
                        Injuries = new List<string> { "Gunshot wound to chest", "Frostbite on feet", "Missing right ear" },
                        MoodPercent = 28,
                        MentalState = "about to break",
                        TopSkills = new List<string> { "Shooting 16", "Melee 12", "Social 10" },
                        Relationships = new List<string> { "widow (husband died Day 88)" },
                        CurrentActivity = "Fighting off mental break"
                    },
                    new MockColonist
                    {
                        Name = "Young Tommy",
                        ShortName = "Tommy",
                        Gender = "male",
                        Age = 16,
                        Role = "farmer",
                        Traits = new List<string> { "Optimist", "Wimp", "Kind" },
                        HealthPercent = 85,
                        HealthStatus = "injured",
                        Injuries = new List<string> { "Frostbite on hands" },
                        MoodPercent = 35,
                        MentalState = "very stressed",
                        TopSkills = new List<string> { "Plants 8", "Animals 6", "Construction 5" },
                        Relationships = new List<string> { "son of Reyes and deceased father" },
                        CurrentActivity = "Crying in bedroom"
                    },
                    new MockColonist
                    {
                        Name = "Dr. Sarah Kim",
                        ShortName = "Sarah",
                        Gender = "female",
                        Age = 38,
                        Role = "medic",
                        Traits = new List<string> { "Industrious", "Neurotic", "Teetotaler" },
                        HealthPercent = 70,
                        HealthStatus = "injured",
                        Injuries = new List<string> { "Exhaustion", "Malnutrition" },
                        MoodPercent = 40,
                        MentalState = "stressed",
                        TopSkills = new List<string> { "Medicine 14 (passionate)", "Intellectual 10", "Cooking 7" },
                        Relationships = new List<string>(),
                        CurrentActivity = "Treating Reyes desperately"
                    }
                },

                Resources = new Dictionary<string, string>
                {
                    { "Silver", "800" },
                    { "Steel", "120" },
                    { "Wood", "85" },
                    { "Food", "1.2 days worth" },
                    { "Medicine", "3" },
                    { "Components", "2" },
                    { "Chemfuel", "0" }
                },

                FactionRelations = new List<MockFactionRelation>
                {
                    new MockFactionRelation { Name = "Ice Raiders", FactionType = "pirate", Goodwill = -100, IsHostile = true, RelationType = "hostile" },
                    new MockFactionRelation { Name = "Northern Traders", FactionType = "outlander civil", Goodwill = -15, IsHostile = false, RelationType = "cold" }
                },

                Environment = new MockEnvironment
                {
                    Weather = "blizzard",
                    TimeOfDay = "late night",
                    Temperature = "-42°C (freezing)",
                    ActiveConditions = new List<string> { "Cold snap", "Volcanic winter" }
                },

                RecentEvents = new List<string>
                {
                    "Raid survivors' last attack - Day 88 (Commander Marcus killed)",
                    "Volcanic winter began - Day 85",
                    "Food supplies destroyed by fire - Day 86",
                    "Cold snap began - Day 89"
                },

                ActiveThreats = new List<string>
                {
                    "Extreme cold (-42°C)",
                    "Starvation imminent (1.2 days food)",
                    "Volcanic winter blocking sun",
                    "Possible manhunter pack detected"
                },

                DeathRecords = new List<string>
                {
                    "Private Chen - Day 45 (hypothermia)",
                    "Maria the cook - Day 60 (infection)",
                    "Commander Marcus Reyes - Day 88 (killed defending colony)",
                    "Baby Hope - Day 75 (malnutrition)"
                },

                BattleHistory = new List<string>
                {
                    "Day 45: Ice Raiders surprise attack - 1 colonist killed",
                    "Day 88: Ice Raiders revenge attack - Commander killed, all raiders eliminated"
                },

                Prisoners = new List<MockPrisoner>(),

                Animals = new List<string>
                {
                    "Luna the husky (bonded to Tommy) - starving"
                },

                NotableItems = new List<string>
                {
                    "Commander Marcus's masterwork rifle (memorial)"
                },

                Infrastructure = new MockInfrastructure
                {
                    HospitalBeds = 2,
                    Turrets = 1,
                    Mortars = 0,
                    PowerGeneration = 800,
                    ResearchCompleted = "Geothermal power (no vent available)"
                }
            };
        }
    }

    #region Data Classes

    /// <summary>
    /// Mock colony context implementing IColonySnapshot for shared formatting.
    /// </summary>
    public class MockColonyContext : IColonySnapshot
    {
        public string ColonyName { get; set; }
        public int ColonyAgeDays { get; set; }
        public string Season { get; set; }
        public string Biome { get; set; }
        public string Quadrum { get; set; }
        public int Year { get; set; }
        public int WealthTotal { get; set; }
        public int ColonistCount { get; set; }
        public int PrisonerCount { get; set; }

        public List<MockColonist> Colonists { get; set; } = new List<MockColonist>();
        public Dictionary<string, string> Resources { get; set; } = new Dictionary<string, string>();
        public List<MockFactionRelation> FactionRelations { get; set; } = new List<MockFactionRelation>();
        public MockEnvironment Environment { get; set; }
        public List<string> RecentEvents { get; set; } = new List<string>();
        public List<string> RecentInteractions { get; set; } = new List<string>();
        public List<string> RecentActivities { get; set; } = new List<string>();
        public List<string> ActiveThreats { get; set; } = new List<string>();
        public List<string> DeathRecords { get; set; } = new List<string>();
        public List<string> BattleHistory { get; set; } = new List<string>();
        public List<MockPrisoner> Prisoners { get; set; } = new List<MockPrisoner>();
        public List<string> Animals { get; set; } = new List<string>();
        public List<string> NotableItems { get; set; } = new List<string>();
        public MockInfrastructure Infrastructure { get; set; }
        public MockRoomSummary RoomSummary { get; set; } = new MockRoomSummary();
        
        // IColonySnapshot interface implementation
        IReadOnlyList<IColonistInfo> IColonySnapshot.ColonistDetails => Colonists.Cast<IColonistInfo>().ToList();
        IReadOnlyList<string> IColonySnapshot.RecentInteractions => RecentInteractions;
        IReadOnlyList<string> IColonySnapshot.RecentActivities => RecentActivities;
        IReadOnlyList<string> IColonySnapshot.RecentEvents => RecentEvents;
        IEnvironmentInfo IColonySnapshot.Environment => Environment;
        IReadOnlyDictionary<string, string> IColonySnapshot.Resources => Resources;
        IReadOnlyList<IFactionRelationInfo> IColonySnapshot.FactionRelations => FactionRelations.Cast<IFactionRelationInfo>().ToList();
        IReadOnlyList<IPrisonerInfo> IColonySnapshot.Prisoners => Prisoners.Cast<IPrisonerInfo>().ToList();
        IReadOnlyList<string> IColonySnapshot.Animals => Animals;
        IReadOnlyList<string> IColonySnapshot.ActiveThreats => ActiveThreats;
        IReadOnlyList<string> IColonySnapshot.NotableItems => NotableItems;
        IInfrastructureInfo IColonySnapshot.Infrastructure => Infrastructure;
        IRoomSummaryInfo IColonySnapshot.RoomSummary => RoomSummary;
        IReadOnlyList<string> IColonySnapshot.DeathRecords => DeathRecords;
        IReadOnlyList<string> IColonySnapshot.BattleHistory => BattleHistory;
    }

    /// <summary>
    /// Mock colonist implementing IColonistInfo for shared formatting.
    /// </summary>
    public class MockColonist : IColonistInfo
    {
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string Gender { get; set; }
        public int Age { get; set; }
        public string Role { get; set; }
        public string ChildhoodBackstory { get; set; }
        public string AdulthoodBackstory { get; set; }
        public List<string> Traits { get; set; } = new List<string>();
        public int HealthPercent { get; set; }
        public string HealthStatus { get; set; }
        public List<string> Injuries { get; set; } = new List<string>();
        public int MoodPercent { get; set; }
        public string MentalState { get; set; }
        public string Inspiration { get; set; }
        public List<string> TopSkills { get; set; } = new List<string>();
        public List<string> Relationships { get; set; } = new List<string>();
        public string CurrentActivity { get; set; }
        
        // IColonistInfo interface implementation
        IReadOnlyList<string> IColonistInfo.Traits => Traits;
        IReadOnlyList<string> IColonistInfo.Injuries => Injuries;
        IReadOnlyList<string> IColonistInfo.TopSkills => TopSkills;
        IReadOnlyList<string> IColonistInfo.Relationships => Relationships;
    }

    /// <summary>
    /// Mock faction relation implementing IFactionRelationInfo for shared formatting.
    /// </summary>
    public class MockFactionRelation : IFactionRelationInfo
    {
        public string Name { get; set; }
        public string FactionType { get; set; }
        public int Goodwill { get; set; }
        public bool IsHostile { get; set; }
        public string RelationType { get; set; }
    }

    /// <summary>
    /// Mock environment implementing IEnvironmentInfo for shared formatting.
    /// </summary>
    public class MockEnvironment : IEnvironmentInfo
    {
        public string Weather { get; set; }
        public string TimeOfDay { get; set; }
        public string Temperature { get; set; }
        public List<string> ActiveConditions { get; set; } = new List<string>();
        
        // IEnvironmentInfo interface implementation
        IReadOnlyList<string> IEnvironmentInfo.ActiveConditions => ActiveConditions;
    }

    /// <summary>
    /// Mock prisoner implementing IPrisonerInfo for shared formatting.
    /// </summary>
    public class MockPrisoner : IPrisonerInfo
    {
        public string Name { get; set; }
        public string OriginalFaction { get; set; }
        public int HealthPercent { get; set; }
        public string RecruitDifficulty { get; set; }
        public int MoodPercent { get; set; }
    }

    /// <summary>
    /// Mock infrastructure implementing IInfrastructureInfo for shared formatting.
    /// </summary>
    public class MockInfrastructure : IInfrastructureInfo
    {
        public int HospitalBeds { get; set; }
        public int Turrets { get; set; }
        public int Mortars { get; set; }
        public int PowerGeneration { get; set; }
        public string ResearchCompleted { get; set; }
    }
    
    /// <summary>
    /// Mock room summary implementing IRoomSummaryInfo for shared formatting.
    /// </summary>
    public class MockRoomSummary : IRoomSummaryInfo
    {
        public int PrivateBedrooms { get; set; }
        public int Barracks { get; set; }
        public int DiningRooms { get; set; }
        public int Kitchens { get; set; }
        public int RecreationRooms { get; set; }
        public int Hospitals { get; set; }
        public int PrisonCells { get; set; }
        public List<string> Highlights { get; set; } = new List<string>();
        
        IReadOnlyList<string> IRoomSummaryInfo.Highlights => Highlights;
    }

    #endregion
}

