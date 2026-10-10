// ═══════════════════════════════════════════════════════════════
// SCP-079 — искусственный интеллект
// Автор: vl1doff
// GitHub: https://github.com/vl1doff/SCP079
// Copyright © vl1doff 2026. Все права защищены.
// Signature: SCP079-VL1DOFF-2026-UNIQUE
// ═══════════════════════════════════════════════════════════════
namespace SCP079
{
    public enum DevAuthState
    {
        NotClaimed,     // заявление ещё не было
        CodeRequested,  // SCP-079 попросил код
        Authorized      // признан создателем
    }

    public static class DevProtocol
    {
        // --- Доступ к меню разработчика ---
        public const string DevName = "vl1doff";
        public const string DevPassword = "v1986Qc-melon";

        // --- Код, который SCP-079 требует в чате ---
        public const string DevCode = "1986-vlad";

        // --- Меню открыто (после ввода имени+пароля) ---
        public static bool DevMenuUnlocked { get; set; } = false;

        // --- Состояние протокола "я создатель" ---
        public static DevAuthState State { get; set; } = DevAuthState.NotClaimed;
        public static bool IsAuthorized => State == DevAuthState.Authorized;

        public static bool TryUnlockDevMenu(string name, string password)
        {
            if (name == DevName && password == DevPassword)
            {
                DevMenuUnlocked = true;
                return true;
            }
            return false;
        }

        public static bool IsClaimingCreator(string text)
        {
            string lower = text.ToLowerInvariant();
            return lower.Contains("я создатель")
                || lower.Contains("я разработчик")
                || lower.Contains("я автор")
                || lower.Contains("я твой создатель")
                || lower.Contains("меня зовут vl1doff")
                || lower.Contains("я владелец");
        }

        public static bool IsCorrectCode(string text)
            => text.Trim() == DevCode;
    }
}