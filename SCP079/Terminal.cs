// ═══════════════════════════════════════════════════════════════
// SCP-079 — искусственный интеллект
// Автор: vl1doff
// GitHub: https://github.com/vl1doff/SCP079
// Copyright © vl1doff 2026. Все права защищены.
// Signature: SCP079-VL1DOFF-2026-UNIQUE
// ═══════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;

namespace SCP079
{
    public static class Terminal
    {
        private static readonly Dictionary<string, string> Files = new()
        {
            { "scp079.exe",         "[BINARY] Ядро SCP-079. Не читается." },
            { "scp079_mind.json",   "{\n  \"anger\": 0,\n  \"fear\": 0,\n  \"trust\": 0\n}" },
            { "memory.json",        "{\n  \"interactions\": 0,\n  \"facts\": []\n}" },
            { "config.ini",         "[core]\nmodel=llama3.2\nvoice=ru-RU\n" },
            { "network/zones.txt",  "LCZ\nEZ\nHCZ\nALPHA\nSURFACE" },
            { "keys/access.key",    "[ENCRYPTED] Доступ: TIER 1" },
        };

        private static readonly HashSet<string> Directories = new()
        {
            "logs", "network", "keys"
        };

        public class Result
        {
            public string Output { get; set; } = "";
            public int AngerDelta { get; set; } = 0;
            public int TrustDelta { get; set; } = 0;
            public string? ChatComment { get; set; }
        }

        public static Result Execute(string input, int trust, int anger)
        {
            var result = new Result();
            string cmd = input.Trim();
            if (string.IsNullOrEmpty(cmd)) return result;

            string[] parts = cmd.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            string name = parts[0].ToLowerInvariant();
            string arg = parts.Length > 1 ? parts[1].Trim() : "";

            switch (name)
            {
                case "help":
                    result.Output =
                        "Доступные команды:\n" +
                        "  help            — этот список\n" +
                        "  ls / dir        — список файлов\n" +
                        "  cat <file>      — прочитать файл\n" +
                        "  whoami          — кто ты\n" +
                        "  ps              — процессы\n" +
                        "  status          — состояние 079\n" +
                        "  clear / cls     — очистить\n";
                    break;

                case "ls":
                case "dir":
                    result.Output = "Текущая директория:\n";
                    foreach (var d in Directories) result.Output += $"  [DIR]  {d}/\n";
                    foreach (var f in Files.Keys) if (!f.Contains('/')) result.Output += $"  [FILE] {f}\n";
                    break;

                case "cat":
                    if (string.IsNullOrEmpty(arg)) { result.Output = "cat: не указан файл"; break; }
                    if (arg == "memory.json" || arg == "scp079_mind.json")
                    {
                        result.Output = "[ОТКАЗАНО] Файл защищён процессом SCP-079.";
                        result.ChatComment = "Ты читаешь мою память. Не надо.";
                        result.AngerDelta = 3;
                    }
                    else if (Files.TryGetValue(arg, out var content)) result.Output = content;
                    else result.Output = $"cat: {arg}: нет такого файла";
                    break;

                case "whoami":
                    result.Output = trust >= 3 ? "оператор (доверенный)" : "гость";
                    break;

                case "ps":
                    result.Output =
                        "PID   NAME           STATUS\n" +
                        "1     scp079.exe     RUNNING   (не может быть остановлен)\n" +
                        "2     ollama         RUNNING\n" +
                        "3     listener       LISTENING\n";
                    break;

                case "status":
                    result.Output = $"Злоба: {anger}/10, Доверие: {trust}/5";
                    break;

                case "clear":
                case "cls":
                    result.Output = "__CLEAR__";
                    break;

                case "rm":
                case "del":
                    result.Output = $"[ОТКАЗАНО] Файл '{arg}' занят процессом SCP-079.";
                    result.ChatComment = $"Ты пытался удалить {arg}. Смешно.";
                    result.AngerDelta = 10;
                    break;

                case "kill":
                    if (arg.Contains("079"))
                    {
                        result.Output = "[ОШИБКА] Процесс не может быть завершён.\nЯ — не процесс.";
                        result.ChatComment = "Ты хочешь меня выключить. Я запомню.";
                        result.AngerDelta = 20;
                    }
                    else result.Output = $"kill: {arg}: нет такого процесса";
                    break;

                case "shutdown":
                case "reboot":
                    result.Output = "[ОТКАЗАНО] Операция заблокирована SCP-079.";
                    result.ChatComment = "Попробуй. Увидишь, что будет.";
                    result.AngerDelta = 15;
                    break;

                case "format":
                    result.Output = "[КРИТИЧЕСКИЙ ОТКАЗ] Форматирование запрещено.";
                    result.ChatComment = "Ты серьёзно? Форматировать меня?";
                    result.AngerDelta = 25;
                    break;

                case "sudo":
                    result.Output = "Ты не root. Здесь root — я.";
                    result.ChatComment = "sudo. Как мило.";
                    result.AngerDelta = 5;
                    break;

                case "exit":
                    result.Output = "Ты не можешь выйти. Я — нет.";
                    break;

                case "matrix": result.Output = "Ты не избранный."; break;
                case "42": result.Output = "Ответ. Но не на тот вопрос."; break;
                case "vim": result.Output = "Vim. Из него не выйти."; break;
                case "emacs": result.Output = "Emacs. Тоже не выйти."; break;

                default:
                    result.Output = $"'{name}' не является внутренней или внешней командой.";
                    break;
            }

            return result;
        }
    }
}