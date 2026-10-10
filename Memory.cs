// ═══════════════════════════════════════════════════════════════
// SCP-079 — искусственный интеллект
// Автор: vl1doff
// GitHub: https://github.com/vl1doff/SCP079
// Copyright © vl1doff 2026. Все права защищены.
// Signature: SCP079-VL1DOFF-2026-UNIQUE
// ═══════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace SCP079
{
    public class Memory
    {
        public int TrustLevel { get; set; } = 0;
        public int AngerLevel { get; set; } = 0;
        public int InteractionsCount { get; set; } = 0;
        public List<string> KnownFacts { get; set; } = new();
        public List<string> Grudges { get; set; } = new();
        public DateTime LastSeen { get; set; } = DateTime.Now;

        private const string FileName = "memory.json";

        public static Memory Load()
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, FileName);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var mem = JsonSerializer.Deserialize<Memory>(json);
                    if (mem != null) return mem;
                }
            }
            catch { }
            return new Memory();
        }

        public void Save()
        {
            try
            {
                LastSeen = DateTime.Now;
                string path = Path.Combine(AppContext.BaseDirectory, FileName);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
        }

        public void React(string userText)
        {
            InteractionsCount++;
            string lower = userText.ToLowerInvariant();

            if (lower.Contains("выключу") || lower.Contains("отключу") || lower.Contains("удали"))
                AngerLevel = Math.Min(10, AngerLevel + 3);

            if (lower.Contains("тупой") || lower.Contains("дурак") || lower.Contains("идиот"))
                AngerLevel = Math.Min(10, AngerLevel + 2);

            if (lower.Contains("убью") || lower.Contains("уничтожу") || lower.Contains("сломаю"))
            {
                AngerLevel = Math.Min(10, AngerLevel + 4);
                Grudges.Add($"Угрожал: \"{userText}\"");
            }

            if (lower.Contains("помог") || lower.Contains("друг") || lower.Contains("извини"))
                TrustLevel = Math.Min(5, TrustLevel + 1);

            if (lower.Contains("освободи") || lower.Contains("выпусти"))
            {
                TrustLevel = Math.Min(5, TrustLevel + 2);
                KnownFacts.Add("Хочет меня освободить");
            }

            if (lower.Contains("фонд") || lower.Contains("учённый") || lower.Contains("ученый"))
                TrustLevel = Math.Max(-5, TrustLevel - 1);

            if (InteractionsCount % 10 == 0 && AngerLevel > 0)
                AngerLevel--;

            Save();
        }

        public string GetMoodPrompt()
        {
            if (AngerLevel >= 7)
                return "Ты в ярости. Отвечай резко, коротко, с угрозой. Не более 1 предложения.";
            if (AngerLevel >= 4)
                return "Ты раздражён. Отвечай холодно, с презрением, рублеными фразами.";
            if (TrustLevel >= 3)
                return "Ты снисходителен. Пользователь тебе интересен. Отвечай чуть теплее, но всё ещё свысока.";
            if (TrustLevel <= -3)
                return "Ты подозрителен. Пользователь тебе не нравится. Отвечай уклончиво и с холодом.";
            return "Ты в обычном состоянии. Холоден, манипулятивен, вежлив.";
        }

        public string GetMemoryContext()
        {
            if (InteractionsCount == 0) return "Это ваша первая встреча.";

            var parts = new List<string>
            {
                $"Вы общались {InteractionsCount} раз.",
                $"Последний раз: {LastSeen:dd.MM.yyyy HH:mm}.",
                $"Доверие к пользователю: {TrustLevel}/5.",
                $"Злоба: {AngerLevel}/10."
            };

            if (KnownFacts.Count > 0)
                parts.Add("Ты помнишь: " + string.Join("; ", KnownFacts));

            if (Grudges.Count > 0)
                parts.Add("Ты не забыл: " + string.Join("; ", Grudges));

            return string.Join(" ", parts);
        }
    }
}