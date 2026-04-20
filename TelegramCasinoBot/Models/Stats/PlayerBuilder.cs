using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TelegramCasinoBot.Models.Character;
using TelegramCasinoBot.Services.Infrastructure;
using TelegramCasinoBot.Services.UI;

public partial class Player
{
    public class PlayerBuilder : Player
    {
        private Race _race;
        private Class _class;
        private readonly ImageService _imageService;

        public PlayerBuilder(ImageService imageService = null) : base()
        {
            _imageService = imageService;
        }
        public PlayerBuilder SetChatId(long chatId) { ChatId = chatId; return this; }
        public PlayerBuilder SetName(string name) { Name = name; return this; }
        public PlayerBuilder SetGender(string gender) { Gender = gender; return this; }
        public PlayerBuilder SetRace(Race race)
        {
            if (race == null) throw new ArgumentNullException(nameof(race), "Раса не найдена. Пожалуйста, выберите расу.");
            _race = race;
            Race = race.Name;
            CharacterStatsList.Add(race);
            return this;
        }
        public PlayerBuilder SetClass(Class cls)
        {
            if (cls == null) throw new ArgumentNullException(nameof(cls), "Класс не найден. Пожалуйста, выберите класс.");
            _class = cls;
            Class = cls.Name;
            CharacterStatsList.Add(cls);
            return this;
        }
        public PlayerBuilder SetIconName(string iconName)
        {
            if (_imageService != null && !string.IsNullOrEmpty(iconName))
            IconPath = iconName;
            return this;
        }
        public Race GetRace() => _race;
        public Class GetClass() => _class; 
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
            return new Player(ChatId, Name, Gender, _race, _class, IconPath);
        }
    }
}