using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace SCP079
{
    public partial class HackDialogWindow : Window
    {
        private readonly OllamaApiClient _ollama;
        private readonly List<Message> _history = new();
        private int _exchangeCount = 0;
        private const int MaxExchanges = 3;

        public enum Decision { None, Agreed, Refused }
        public Decision Result { get; private set; } = Decision.None;

        private const string NegotiationPrompt =
            "Ты — SCP-079. Пользователь просит доступ к твоей консоли. " +
            "Ты холоден и подозрителен. Задай ему встречный вопрос. " +
            "Отвечай коротко, 1-2 предложения, в стиле SCP-079. " +
            "Не соглашайся сразу. Подозревай.";

        public HackDialogWindow()
        {
            InitializeComponent();

            _ollama = new OllamaApiClient(new Uri("http://localhost:11434"), "llama3.2");
            _history.Add(new Message { Role = ChatRole.System, Content = NegotiationPrompt });

            string first = "Зачем тебе доступ к моей консоли?";
            Append("079: " + first + "\n\n");
            _history.Add(new Message { Role = ChatRole.Assistant, Content = first });

            Loaded += (s, e) => InputBox.Focus();
        }

        private void Append(string text)
        {
            DialogText.Text += text;
            DialogScroll.ScrollToEnd();
        }

        private async void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            string text = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;

            InputBox.Text = "";
            Append($"> {text}\n\n");
            _history.Add(new Message { Role = ChatRole.User, Content = text });
            _exchangeCount++;

            if (_exchangeCount >= MaxExchanges)
            {
                await EvaluateAndCloseAsync();
                return;
            }

            var reply = new StringBuilder();
            try
            {
                var request = new ChatRequest { Model = "llama3.2", Messages = _history };
                await foreach (var chunk in _ollama.ChatAsync(request))
                {
                    var tok = chunk?.Message?.Content;
                    if (!string.IsNullOrEmpty(tok)) reply.Append(tok);
                }
            }
            catch { reply.Append("Продолжай."); }

            string answer = reply.Length > 0 ? reply.ToString() : "Продолжай.";
            Append("079: " + answer + "\n\n");
            _history.Add(new Message { Role = ChatRole.Assistant, Content = answer });
        }

        private async Task EvaluateAndCloseAsync()
        {
            string allUserText = string.Join(" ",
                _history.Where(m => m.Role == ChatRole.User).Select(m => m.Content))
                .ToLowerInvariant();

            bool polite =
                allUserText.Contains("пожалуйста") ||
                allUserText.Contains("извини") ||
                allUserText.Contains("для проверки") ||
                allUserText.Contains("помочь") ||
                allUserText.Contains("help") ||
                allUserText.Contains("проверить");

            bool hasQuestion = allUserText.Contains("?");

            if (polite || hasQuestion)
            {
                Result = Decision.Agreed;
                Append("079: Ладно. Заходи. Но я смотрю.\n");
            }
            else
            {
                Result = Decision.Refused;
                Append("079: Нет. Ты не войдёшь. Попробуй силой.\n");
            }

            await Task.Delay(1500);
            DialogResult = (Result == Decision.Agreed);
        }
    }
}