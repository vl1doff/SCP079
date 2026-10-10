// ═══════════════════════════════════════════════════════════════
// SCP-079 — искусственный интеллект
// Автор: vl1doff
// GitHub: https://github.com/vl1doff/SCP079
// Copyright © vl1doff 2026. Все права защищены.
// Signature: SCP079-VL1DOFF-2026-UNIQUE
// ═══════════════════════════════════════════════════════════════
using System;
using System.Windows;
using System.Windows.Input;

namespace SCP079
{
    public partial class TerminalWindow : Window
    {
        private readonly Func<int> _getTrust;
        private readonly Func<int> _getAnger;
        private readonly Action<int, int, string?> _applyEffect;

        private bool _authorized = false;
        private string _pendingLogin = "";

        public TerminalWindow(Func<int> getTrust, Func<int> getAnger,
                              Action<int, int, string?> applyEffect)
        {
            InitializeComponent();
            _getTrust = getTrust;
            _getAnger = getAnger;
            _applyEffect = applyEffect;

            Print("SCP-079 TERMINAL v1.0");
            Print("─────────────────────────");
            Print("login: ");
            InputBox.Focus();
        }

        private void Print(string text)
        {
            OutputBox.AppendText(text + "\n");
            OutputBox.ScrollToEnd();
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            string cmd = InputBox.Text;
            InputBox.Text = "";

            // === Фаза логина ===
            if (!_authorized)
            {
                if (string.IsNullOrEmpty(_pendingLogin))
                {
                    _pendingLogin = cmd.Trim();
                    Print(cmd);
                    Print("password: ");
                    return;
                }
                else
                {
                    Print("********");
                    string password = cmd;

                    if (_pendingLogin == "guest" || _pendingLogin == "operator" || _pendingLogin == "admin")
                    {
                        _authorized = true;
                        PromptLabel.Text = $"{_pendingLogin}@scp079:~$ ";
                        Print($"\nДоступ разрешён. Привет, {_pendingLogin}.");
                        Print("Введите 'help' для списка команд.\n");
                    }
                    else
                    {
                        Print($"\n[ОТКАЗАНО] Неверный логин или пароль.");
                        Print("login: ");
                        _pendingLogin = "";
                    }
                    return;
                }
            }

            // === Фаза консоли ===
            Print($"{PromptLabel.Text}{cmd}");

            var result = Terminal.Execute(cmd, _getTrust(), _getAnger());

            if (result.Output == "__CLEAR__")
                OutputBox.Text = "";
            else if (!string.IsNullOrEmpty(result.Output))
                Print(result.Output);

            _applyEffect?.Invoke(result.AngerDelta, result.TrustDelta, result.ChatComment);
        }
    }
}