using System.Collections.Generic;

public class CharacterClass
{
    public string Id { get; }
    public string Name { get; }
    public string Description { get; set; }
    public int HealthBonus { get; set; }
    public int ManaBonus { get; set; }
    public int StaminaBonus { get; set; }
    public int DefenseBonus { get; set; }
    public double MeleeDamageMultiplier { get; set; }
    public double RangedDamageMultiplier { get; set; }
    public double MagicDamageMultiplier { get; set; }
    public string[] PreferredWeaponTypes { get; set; }

    private List<string> _startingAbilities = new List<string>();
    public IReadOnlyList<string> StartingAbilities => _startingAbilities;

    public CharacterClass(string id, string name)
    {
        Id = id;
        Name = name;
    }

    public void AddStartingAbility(string ability) => _startingAbilities.Add(ability);
    public bool RemoveStartingAbility(string ability) => _startingAbilities.Remove(ability);
}