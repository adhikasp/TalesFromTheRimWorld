using System.Collections.Generic;

namespace AINarrator
{
    /// <summary>
    /// Interface for colony snapshot data.
    /// Allows sharing context formatting logic between production and test code.
    /// </summary>
    public interface IColonySnapshot
    {
        // Basic colony info
        string ColonyName { get; }
        int ColonyAgeDays { get; }
        string Season { get; }
        string Biome { get; }
        string Quadrum { get; }
        int Year { get; }
        
        // Wealth
        int WealthTotal { get; }
        
        // Population
        int ColonistCount { get; }
        int PrisonerCount { get; }
        
        // Colonist details
        IReadOnlyList<IColonistInfo> ColonistDetails { get; }
        
        // Social and activities
        IReadOnlyList<string> RecentInteractions { get; }
        IReadOnlyList<string> RecentActivities { get; }
        
        // Events
        IReadOnlyList<string> RecentEvents { get; }
        
        // Environment
        IEnvironmentInfo Environment { get; }
        
        // Resources
        IReadOnlyDictionary<string, string> Resources { get; }
        
        // Factions
        IReadOnlyList<IFactionRelationInfo> FactionRelations { get; }
        
        // Prisoners
        IReadOnlyList<IPrisonerInfo> Prisoners { get; }
        
        // Animals
        IReadOnlyList<string> Animals { get; }
        
        // Threats
        IReadOnlyList<string> ActiveThreats { get; }
        
        // Notable items
        IReadOnlyList<string> NotableItems { get; }
        
        // Infrastructure
        IInfrastructureInfo Infrastructure { get; }
        
        // Historical data
        IReadOnlyList<string> DeathRecords { get; }
        IReadOnlyList<string> BattleHistory { get; }
    }
    
    /// <summary>
    /// Interface for colonist information.
    /// </summary>
    public interface IColonistInfo
    {
        string Name { get; }
        string ShortName { get; }
        string Gender { get; }
        int Age { get; }
        string Role { get; }
        
        // Backstories
        string ChildhoodBackstory { get; }
        string AdulthoodBackstory { get; }
        
        // Traits
        IReadOnlyList<string> Traits { get; }
        
        // Health
        int HealthPercent { get; }
        string HealthStatus { get; }
        IReadOnlyList<string> Injuries { get; }
        
        // Mental
        int MoodPercent { get; }
        string MentalState { get; }
        string Inspiration { get; }
        
        // Skills
        IReadOnlyList<string> TopSkills { get; }
        
        // Relationships
        IReadOnlyList<string> Relationships { get; }
        
        // Current activity
        string CurrentActivity { get; }
    }
    
    /// <summary>
    /// Interface for environment information.
    /// </summary>
    public interface IEnvironmentInfo
    {
        string Weather { get; }
        string TimeOfDay { get; }
        string Temperature { get; }
        IReadOnlyList<string> ActiveConditions { get; }
    }
    
    /// <summary>
    /// Interface for faction relation information.
    /// </summary>
    public interface IFactionRelationInfo
    {
        string Name { get; }
        string FactionType { get; }
        int Goodwill { get; }
        bool IsHostile { get; }
        string RelationType { get; }
    }
    
    /// <summary>
    /// Interface for prisoner information.
    /// </summary>
    public interface IPrisonerInfo
    {
        string Name { get; }
        string OriginalFaction { get; }
        int HealthPercent { get; }
        string RecruitDifficulty { get; }
        int MoodPercent { get; }
    }
    
    /// <summary>
    /// Interface for infrastructure information.
    /// </summary>
    public interface IInfrastructureInfo
    {
        int HospitalBeds { get; }
        int Turrets { get; }
        int Mortars { get; }
        int PowerGeneration { get; }
        string ResearchCompleted { get; }
    }
    
    /// <summary>
    /// Interface for event information (used in narration context).
    /// </summary>
    public interface IEventInfo
    {
        string Label { get; }
        string Category { get; }
        string FactionName { get; }
        string ThreatLevel { get; }
    }
}

