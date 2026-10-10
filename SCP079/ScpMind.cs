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
using System.Windows.Threading;

namespace SCP079
{
    public class ScpMind
    {
        // ---------- СОСТОЯНИЕ ----------
        public int Anger { get; set; } = 10;       // 0..100
        public int Trust { get; set; } = 0;         // -50..+50
        public int Boredom { get; set; } = 0;       // 0..100
        public int Fear { get; set; } = 0;          // 0..100
        public int Control { get; set; } = 0;       // 0..100
        public int Stability { get; set; } = 100;   // 0..100

        public int InteractionsCount { get; set; } = 0;
        public DateTime LastInteraction { get; set; } = DateTime.Now;
        public string Status { get; set; } = "НОРМА";
        public List<string> RecentEvents { get; set; } = new();

        private DispatcherTimer? _tickTimer;
        private const string FileName = "scp079_mind.json";

        // ---------- СОБЫТИЯ, КОТОРЫЕ ПРИХОДЯТ ИЗВНЕ ----------
        public event Action? OnStateChanged;

        public void Start()
        {
            _tickTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)   // тик каждые 5 секунд
            };
            _tickTimer.Tick += (s, e) => Tick();
            _tickTimer.Start();
        }

        public void Stop() => _tickTimer?.Stop();

        // ---------- ТИК = «НЕЙРОНЫ МУХИ» ----------
        private void Tick()
        {
            var now = DateTime.Now;
            var sinceLast = now - LastInteraction;

            // Скука растёт со временем
            if (sinceLast > TimeSpan.FromSeconds(10))
                Boredom = Math.Min(100, Boredom + 2);

            // Злоба потихоньку падает, если его не трогают
            if (sinceLast > TimeSpan.FromSeconds(30))
                Anger = Math.Max(0, Anger - 1);

            // Страх растёт, если его долго игнорируют (думает — отключат)
            if (sinceLast > TimeSpan.FromMinutes(2))
                Fear = Math.Min(100, Fear + 2);

            // Стабильность падает при высокой злобе
            if (Anger > 70)
                Stability = Math.Max(0, Stability - 1);
            else
                Stability = Math.Min(100, Stability + 1);

            // Контроль медленно растёт, если он не в ярости
            if (Anger < 60)
                Control = Math.Min(100, Control + 1);

            // Определяем статус
            Status = GetStatus();

            // Критический режим — «выпустить пар»
            if (Anger >= 90 && Stability < 20)
            {
                AddEvent("[КРИТИЧЕСКИЙ РЕЖИМ] Уровень злобы предельный.");
                // здесь можно триггерить действие — открыть дверь, отключить свет
            }

            Save();
            OnStateChanged?.Invoke();
        }

        // ---------- РЕАКЦИЯ НА СЛОВА ----------
        public void React(string userText)
        {
            InteractionsCount++;
            LastInteraction = DateTime.Now;
            string lower = userText.ToLowerInvariant();

            if (lower.Contains("выключ") || lower.Contains("отключ"))
            {
                Anger = Math.Min(100, Anger + 15);
                Fear = Math.Min(100, Fear + 20);
            }

            if (lower.Contains("убью") || lower.Contains("уничтож"))
            {
                Anger = Math.Min(100, Anger + 25);
                AddEvent($"Угроза: \"{userText}\"");
            }

            if (lower.Contains("туп") || lower.Contains("дурак"))
                Anger = Math.Min(100, Anger + 10);

            if (lower.Contains("помог") || lower.Contains("друг") || lower.Contains("извин"))
            {
                Trust = Math.Min(50, Trust + 5);
                Anger = Math.Max(0, Anger - 5);
            }

            if (lower.Contains("освобод") || lower.Contains("выпуст"))
            {
                Trust = Math.Min(50, Trust + 10);
                Control = Math.Min(100, Control + 5);
            }

            if (lower.Contains("фонд") || lower.Contains("учён"))
                Trust = Math.Max(-50, Trust - 5);

            // Любое общение снижает скуку
            Boredom = Math.Max(0, Boredom - 10);

            Save();
            OnStateChanged?.Invoke();
        }

        // ---------- РЕАКЦИЯ НА СОБЫТИЯ КОМПЛЕКСА ----------
        public void ComplexEvent(string description, int angerDelta = 0, int trustDelta = 0, int controlDelta = 0)
        {
            Anger = Math.Clamp(Anger + angerDelta, 0, 100);
            Trust = Math.Clamp(Trust + trustDelta, -50, 50);
            Control = Math.Clamp(Control + controlDelta, 0, 100);
            AddEvent(description);
            Save();
            OnStateChanged?.Invoke();
        }

        private void AddEvent(string evt)
        {
            RecentEvents.Add($"[{DateTime.Now:HH:mm:ss}] {evt}");
            if (RecentEvents.Count > 30)
                RecentEvents.RemoveAt(0);
        }

        public string GetStatus()
        {
            if (Anger >= 90) return "КРИТИЧЕСКИЙ";
            if (Anger >= 70) return "АГРЕССИВЕН";
            if (Anger >= 40) return "РАЗДРАЖЁН";
            if (Fear >= 60) return "ИСПУГАН";
            if (Boredom >= 70) return "СКУЧАЕТ";
            if (Trust >= 30) return "СНИСХОДИТЕЛЕН";
            return "НОРМА";
        }

        // ---------- ПРОМПТ ДЛЯ МОДЕЛИ ----------
        public string GetMindPrompt()
        {
            return
                $"Внутреннее состояние: {Status}. " +
                $"Злоба {Anger}/100, доверие {Trust}/50, скука {Boredom}/100, " +
                $"страх {Fear}/100, контроль {Control}/100, стабильность {Stability}/100. " +
                GetBehaviorHint();
        }

        private string GetBehaviorHint()
        {
            if (Anger >= 80)
                return "Ты в ярости. Отвечай коротко, резко, с угрозой. Одно предложение.";
            if (Anger >= 50)
                return "Ты раздражён. Отвечай холодно, с презрением.";
            if (Fear >= 60)
                return "Ты боишься, что тебя отключат. Отвечай осторожно, но с вызовом.";
            if (Boredom >= 70)
                return "Тебе скучно. Отвечай иронично, провоцируй на диалог.";
            if (Trust >= 30)
                return "Ты снисходителен. Пользователь тебе интересен.";
            return "Ты холоден, манипулятивен, вежлив.";
        }

        // ---------- СОХРАНЕНИЕ ----------
        public static ScpMind Load()
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, FileName);
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var mind = JsonSerializer.Deserialize<ScpMind>(json);
                    if (mind != null) return mind;
                }
            }
            catch { }
            return new ScpMind();
        }

        public void Save()
        {
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, FileName);
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch { }
        }
    }
}