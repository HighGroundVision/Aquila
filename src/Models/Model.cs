using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace HGV.Aquila.Models
{
    public class Hero
    {
        [JsonProperty("id")]
        public int Id { get; set;} // Partition key

        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("aliases")]
        public List<string> Aliases { get; set; } = new List<string>();

        [JsonProperty("image_banner")]
        public string ImageBanner { get; set; }

        [JsonProperty("image_icon")]
        public string ImageIcon { get; set; }

        [JsonProperty("image_profile")]
        public string ImageProfile { get; set; }

        [JsonProperty("ability_replace_required")]
        public bool AbilityReplaceRequired { get; set; }

        [JsonProperty("strength_gain")]
        public double StrengthGain { get; set; }

        [JsonProperty("max_strength")]
        public double MaxStrength { get; set; }

        [JsonProperty("intelligence_gain")]
        public double IntelligenceGain { get; set; }

        [JsonProperty("max_intelligence")]
        public double MaxIntelligence { get; set; }

        [JsonProperty("agility_gain")]
        public double AgilityGain { get; set; }

        [JsonProperty("max_agility")]
        public double MaxAgility { get; set; }

        [JsonProperty("primary_attribute")]
        public string AttributePrimary { get; set; }

        [JsonProperty("attack_capabilities")]
        public string AttackCapabilities { get; set; }

        [JsonProperty("win_rate")]
        public double WinRate { get; set; }

        [JsonProperty("picked")]
        public bool Picked { get; set; }

        [JsonProperty("damage")]
        public int Damage { get; set; }

        [JsonProperty("roles")]
        public List<string> Roles { get; set; }
    }

    public class Patches
    {
        [JsonProperty("overall")]
        public List<string> Overall { get; set; }

        [JsonProperty("majorCounts")]
        public List<int> MajorCounts { get; set; }

        [JsonProperty("majors")]
        public List<string> Majors { get; set; }

        [JsonProperty("majorsMinors")]
        public Dictionary<string, List<string>> Details { get; set; }
    }

    public class HeroData
    {
        [JsonProperty("wins")]
        public int Wins { get; set; }

        [JsonProperty("numGames")]
        public int Total { get; set; }

        [JsonProperty("winrate")]
        public double WinRate { get; set; }
    }

    public class Data
    {
        [JsonProperty("patches")]
        public Patches Patches { get; set; }

        [JsonProperty("heroStats")]
        public Dictionary<int, Dictionary<string, HeroData>> Heroes { get; set;}
    }

    public class Root
    {
        [JsonProperty("data")]
        public Data Data { get; set; }
    }
}
