using System.Collections.Generic;

public class Race
{
    public string Id { get; } // Id неизменяем
    public string Name { get; } // Name задаётся в конструкторе и не меняется
    public string Description { get; set; }
    public int HealthBonus { get; set; }
    public int ManaBonus { get; set; }
    public int StaminaBonus { get; set; }
    public int DefenseBonus { get; set; }
    public double ExperienceMultiplier { get; set; }
    public double MeleeDamageBonus { get; set; }
    public double RangedDamageBonus { get; set; }
    public double MagicDamageBonus { get; set; }
    public string[] AvailableGenders { get; set; }

    private List<string> _specialAbilities = new List<string>();
    public IReadOnlyList<string> SpecialAbilities => _specialAbilities; // только чтение

    public Race(string id, string name)
    {
        Id = id;
        Name = name;
        // _specialAbilities уже инициализирован
    }

    // Методы для управления способностями
    public void AddSpecialAbility(string ability) => _specialAbilities.Add(ability);
    public bool RemoveSpecialAbility(string ability) => _specialAbilities.Remove(ability);
}