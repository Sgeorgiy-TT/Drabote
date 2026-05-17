using System.Text.Json.Serialization;

namespace TelegramCasinoBot.Models.Gameplay
{
    public class Item
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ItemType { get; set; } 
        public string Rarity { get; set; }
        public int Level { get; set; }

        [JsonPropertyName("WeaponType")]
        public string WeaponType { get; set; }
        [JsonPropertyName("BaseDamage")]
        public int? BaseDamage { get; set; }
        [JsonPropertyName("MagicDamage")]
        public int? MagicDamage { get; set; }

        [JsonPropertyName("ArmorType")]
        public string ArmorType { get; set; }
        [JsonPropertyName("Defense")]
        public int? Defense { get; set; }

        [JsonPropertyName("EffectType")]
        public string EffectType { get; set; }
        [JsonPropertyName("Value")]
        public int? Value { get; set; }

        [JsonPropertyName("RequiredClass")]
        public string RequiredClass { get; set; }
        [JsonPropertyName("RequiredRace")]
        public string RequiredRace { get; set; }
        [JsonPropertyName("RequiredLevel")]
        public int? RequiredLevel { get; set; }

        public string GetDisplayName()
        {
            string mark = Rarity switch
            {
                "Uncommon" => "⭐",
                "Rare" => "✨",
                "Epic" => "💎",
                "Legendary" => "🏆",
                _ => ""
            };
            return $"{mark} {Name}".Trim();
        }
    }
}