using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TelegramCasinoBot.Models.Character;
using TelegramCasinoBot.Services.UI;

public partial class Player
{
    public class PlayerBuilder : Player
    {
        public PlayerBuilder() : base() { }

        public PlayerBuilder SetChatId(long chatId) { ChatId = chatId; return this; }
        public PlayerBuilder SetName(string name) { Name = name; return this; }
        public PlayerBuilder SetGender(string gender) { Gender = gender; return this; }
        public PlayerBuilder SetRace(Race race)
        {
            if (race == null) throw new ArgumentNullException(nameof(race), "Раса не может быть null. Пожалуйста, выберите расу.");
            Race = race.Name;
            CharacterStatsList.Add(race);
            return this;
        }
        public PlayerBuilder SetClass(Class cls)
        {
            if (cls == null) throw new ArgumentNullException(nameof(cls), "Класс не может быть null. Пожалуйста, выберите класс.");
            Class = cls.Name;
            CharacterStatsList.Add(cls);
            return this;
        }
        public PlayerBuilder SetIconPath(string iconPath) { IconPath = iconPath; return this; }

        public Race GetRace() => CharacterStatsList.OfType<Race>().FirstOrDefault();
        public Class GetClass() => CharacterStatsList.OfType<Class>().FirstOrDefault();
        public string GetGender() => Gender;

        public Player Build()
        {
            var errors = new List<string>();
            if (string.IsNullOrEmpty(Name))
                errors.Add("Имя не задано.");
            if (GetRace() == null)
                errors.Add("Раса не выбрана.");
            if (GetClass() == null)
                errors.Add("Класс не выбран.");
            if (errors.Any())
                throw new InvalidOperationException($"Невозможно создать персонажа из-за следующих ошибок:\n{string.Join("\n", errors)}");
            return new Player(ChatId, Name, Gender, GetRace(), GetClass(), IconPath);
        }
    }
}