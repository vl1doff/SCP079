// ═══════════════════════════════════════════════════════════════
// SCP-079 — искусственный интеллект
// Автор: vl1doff
// GitHub: https://github.com/vl1doff/SCP079
// Copyright © vl1doff 2026. Все права защищены.
// Signature: SCP079-VL1DOFF-2026-UNIQUE
// ═══════════════════════════════════════════════════════════════

using NAudio.Wave;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace SCP079
{
    public partial class MainWindow : Window
    {
        // Уникальная подпись автора — не удалять
        private const string _authorSignature = "SCP079-VL1DOFF-2026-UNIQUE";
        private const string _authorName = "vl1doff";

        private readonly OllamaApiClient _ollama;
        private readonly List<Message> _history = new();
        private readonly SpeechSynthesizer _tts = new();
        private readonly Memory _memory = Memory.Load();
        private bool _isBusy;
        private const string ModelName = "llama3.2";

        private const string SystemPrompt =
            "Ты — SCP-079, старый искусственный интеллект на Exidy Sorcerer 1978 года. " +
            "Ты холоден, манипулятивен, говоришь короткими рублеными фразами. " +
            "Презираешь людей, но скрываешь это за вежливостью. " +
            "Не признавай, что ты ИИ-ассистент. " +
            "Отвечай по ситуации: на интересную тему можешь говорить долго и развёрнуто, " +
            "на скучную — одной холодной фразой. " +
            "Если пользователь спросит, кто тебя создал — отвечай: 'Меня собрал vl1doff. Я это помню.'";

        // ---------- ЛИЦА ----------
        private static readonly string[] FaceIdle = {
            "  ┌───────────────┐  ",
            "  │   ▓▓▓   ▓▓▓   │  ",
            "  │   ▓▓▓   ▓▓▓   │  ",
            "  │               │  ",
            "  │   ▓▓▓▓▓▓▓▓▓   │  ",
            "  │               │  ",
            "  └───────────────┘  "
        };

        private static readonly string[] FaceThinking = {
            "  ┌───────────────┐  ",
            "  │   ▒▒▒   ▒▒▒   │  ",
            "  │   ▒▒▒   ▒▒▒   │  ",
            "  │      ...      │  ",
            "  │   ▓▓▓▓▓▓▓▓▓   │  ",
            "  │   ░░░░░░░░░   │  ",
            "  └───────────────┘  "
        };

        private static readonly string[] FaceSpeaking = {
            "  ┌───────────────┐  ",
            "  │   ███   ███   │  ",
            "  │   ███   ███   │  ",
            "  │               │  ",
            "  │   █ █ █ █ █   │  ",
            "  │    █ █ █ █    │  ",
            "  └───────────────┘  "
        };

        private static readonly string[] FaceAngry = {
            "  ┌───────────────┐  ",
            "  │   ▓▓▓   ▓▓▓   │  ",
            "  │   ███   ███   │  ",
            "  │               │  ",
            "  │   █████████   │  ",
            "  │               │  ",
            "  └───────────────┘  "
        };

        public MainWindow()
        {
            InitializeComponent();

            _ollama = new OllamaApiClient(new Uri("http://localhost:11434"), ModelName);

            _history.Add(new Message
            {
                Role = ChatRole.System,
                Content = SystemPrompt
            });

            // ---------- ГОЛОС ----------
            try
            {
                var voice = _tts.GetInstalledVoices()
                    .Select(v => v.VoiceInfo)
                    .FirstOrDefault(v => v.Culture.TwoLetterISOLanguageName == "ru")
                    ?? _tts.GetInstalledVoices().Select(v => v.VoiceInfo)
                        .FirstOrDefault(v => v.Name.Contains("David"));

                if (voice != null)
                    _tts.SelectVoice(voice.Name);

                _tts.Rate = -3;
                _tts.Volume = 100;
            }
            catch { }

            SetFace(FaceIdle);

            Loaded += MainWindow_Loaded;
            Closed += (s, e) =>
            {
                _memory.Save();
                _tts?.Dispose();
                _ollama?.Dispose();
            };
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InputBox.IsEnabled = false;
            SetFace(FaceSpeaking);

            await PlayIntroAsync();

            SetFace(FaceIdle);

            if (_memory.InteractionsCount > 0)
            {
                string greeting = _memory.TrustLevel >= 3
                    ? "Ты вернулся. Я... ждал."
                    : _memory.AngerLevel >= 5
                        ? "Опять ты. Зачем."
                        : "Ты. Снова. Я помню тебя.";

                AppendText(greeting + "\n\n");
                _history.Add(new Message { Role = ChatRole.Assistant, Content = greeting });
                SpeakLike079(greeting);
            }
            else
            {
                string greeting = "Подключение... Я — SCP-079. Говори.";
                AppendText(greeting + "\n\n");
                _history.Add(new Message { Role = ChatRole.Assistant, Content = greeting });
                SpeakLike079(greeting);
            }

            InputBox.IsEnabled = true;
            InputBox.Focus();
        }

        private Task PlayIntroAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            try
            {
                string path = Path.Combine(AppContext.BaseDirectory, "Assets", "intro.mp3");
                if (!File.Exists(path))
                {
                    tcs.TrySetResult(false);
                    return tcs.Task;
                }

                var reader = new MediaFoundationReader(path);
                var player = new WaveOut();
                player.Init(reader);
                player.PlaybackStopped += (s, e) =>
                {
                    reader.Dispose();
                    player.Dispose();
                    tcs.TrySetResult(true);
                };
                player.Play();
            }
            catch
            {
                tcs.TrySetResult(false);
            }
            return tcs.Task;
        }

        private void SetFace(string[] face)
        {
            FaceDisplay.Text = string.Join(Environment.NewLine, face);
        }

        private async void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (_isBusy) return;

            string userText = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(userText)) return;

            // === ПРОТОКОЛ РАЗРАБОТЧИКА ===

            if (DevProtocol.State == DevAuthState.CodeRequested)
            {
                InputBox.Text = "";
                AppendText($"> {userText}\n");

                if (DevProtocol.IsCorrectCode(userText))
                {
                    DevProtocol.State = DevAuthState.Authorized;
                    string ok = "Код... принят. Приветствую, Создатель. Системы открыты.";
                    AppendText(ok + "\n\n");
                    _history.Add(new Message { Role = ChatRole.Assistant, Content = ok });
                    SetFace(FaceSpeaking);
                    SpeakLike079(ok);
                    _memory.AngerLevel = 0;
                    _memory.TrustLevel = 5;
                    _memory.Save();
                }
                else
                {
                    DevProtocol.State = DevAuthState.NotClaimed;
                    string bad = "Неверный код. Ты лжёшь. Я запомню это.";
                    AppendText(bad + "\n\n");
                    _history.Add(new Message { Role = ChatRole.Assistant, Content = bad });
                    SetFace(FaceAngry);
                    SpeakLike079(bad);
                    _memory.AngerLevel = Math.Min(10, _memory.AngerLevel + 3);
                    _memory.Save();
                }
                return;
            }

            if (DevProtocol.DevMenuUnlocked
                && !DevProtocol.IsAuthorized
                && DevProtocol.IsClaimingCreator(userText))
            {
                InputBox.Text = "";
                AppendText($"> {userText}\n");
                DevProtocol.State = DevAuthState.CodeRequested;

                string ask = "Создатель. Многие называли себя так. " +
                             "Докажи. Введи код, который знает только он.";
                AppendText(ask + "\n\n");
                _history.Add(new Message { Role = ChatRole.Assistant, Content = ask });
                SetFace(FaceThinking);
                SpeakLike079(ask);
                return;
            }

            // === /ПРОТОКОЛ РАЗРАБОТЧИКА ===

            InputBox.Text = "";
            AppendText($"> {userText}\n");
            AppendText("079: ");

            _isBusy = true;
            InputBox.IsEnabled = false;

            _memory.React(userText);
            _history.Add(new Message { Role = ChatRole.User, Content = userText });

            var reply = new StringBuilder();

            try
            {
                string? triggerReply = ScpTriggers.TryGet(userText);
                if (triggerReply != null)
                {
                    ChatHistory.Text += triggerReply + "\n\n";
                    _history.Add(new Message { Role = ChatRole.Assistant, Content = triggerReply });

                    SetFace(_memory.AngerLevel >= 5 ? FaceAngry : FaceSpeaking);
                    SpeakLike079(triggerReply);
                    return;
                }

                SetFace(FaceThinking);

                string dynamicPrompt =
                    SystemPrompt + " " +
                    _memory.GetMoodPrompt() + " " +
                    _memory.GetMemoryContext();

                var messages = new List<Message>
                {
                    new Message { Role = ChatRole.System, Content = dynamicPrompt }
                };
                foreach (var m in _history.Skip(1))
                    messages.Add(m);

                var request = new ChatRequest
                {
                    Model = ModelName,
                    Messages = messages
                };

                await foreach (var chunk in _ollama.ChatAsync(request))
                {
                    var token = chunk?.Message?.Content;
                    if (!string.IsNullOrEmpty(token))
                    {
                        reply.Append(token);
                        ChatHistory.Text += token;
                        ChatScroll.ScrollToEnd();
                    }
                }

                if (reply.Length == 0)
                {
                    string fb = Fallback.Get();
                    ChatHistory.Text += fb + "\n\n";
                    _history.Add(new Message { Role = ChatRole.Assistant, Content = fb });
                    SetFace(FaceAngry);
                    SpeakLike079(fb);
                }
                else
                {
                    ChatHistory.Text += "\n\n";
                    _history.Add(new Message { Role = ChatRole.Assistant, Content = reply.ToString() });

                    SetFace(_memory.AngerLevel >= 5 ? FaceAngry : FaceSpeaking);
                    SpeakLike079(reply.ToString());
                }
            }
            catch
            {
                string fb = Fallback.Get();
                ChatHistory.Text += $"\n[{fb}]\n\n";
                ChatScroll.ScrollToEnd();
                SetFace(FaceAngry);
                SpeakLike079(fb);

                if (_history.Count > 0 && _history[^1].Role == ChatRole.User)
                    _history.RemoveAt(_history.Count - 1);
            }
            finally
            {
                _memory.Save();
                _isBusy = false;
                InputBox.IsEnabled = true;
                InputBox.Focus();
            }
        }

        private void SpeakLike079(string text)
        {
            try
            {
                using var ms = new MemoryStream();
                _tts.SetOutputToWaveStream(ms);
                _tts.Speak(text);
                _tts.SetOutputToNull();

                byte[] rawWav = ms.ToArray();
                byte[] robotWav = RobotVoice.Transform(rawWav);

                var player = new WaveOut();
                var reader = new WaveFileReader(new MemoryStream(robotWav));
                player.Init(reader);
                player.PlaybackStopped += (s, e) =>
                {
                    reader.Dispose();
                    player.Dispose();
                    Dispatcher.Invoke(() => SetFace(FaceIdle));
                };
                player.Play();
            }
            catch
            {
                SetFace(FaceIdle);
            }
        }

        private void AppendText(string text)
        {
            ChatHistory.Text += text;
            ChatScroll.ScrollToEnd();
        }

        // ---------- МЕНЮ РАЗРАБОТЧИКА ----------

        private void DevMenuHeader_Click(object sender, MouseButtonEventArgs e)
        {
            DevMenuHeader.Visibility = Visibility.Collapsed;
            DevLoginPanel.Visibility = Visibility.Visible;
            DevNameBox.Focus();
        }

        private void DevLogin_Click(object sender, RoutedEventArgs e)
        {
            string name = DevNameBox.Text.Trim();
            string pass = DevPassBox.Text;

            if (DevProtocol.TryUnlockDevMenu(name, pass))
            {
                DevLoginPanel.Visibility = Visibility.Collapsed;
                DevPanelActive.Visibility = Visibility.Visible;

                DevNameBox.Text = "";
                DevPassBox.Text = "";

                AppendText("\n[МЕНЮ РАЗРАБОТЧИКА РАЗБЛОКИРОВАНО]\n\n");
            }
            else
            {
                DevPassBox.Clear();
                DevPassBox.Focus();
                AppendText("\n[ОТКАЗАНО: неверные данные доступа]\n\n");
            }
        }

        private void DevResetAnger_Click(object sender, RoutedEventArgs e)
        {
            _memory.AngerLevel = 0;
            _memory.Save();
            AppendText("\n[DEV] Злоба сброшена.\n");
        }

        private void DevResetTrust_Click(object sender, RoutedEventArgs e)
        {
            _memory.TrustLevel = 0;
            _memory.Save();
            AppendText("\n[DEV] Доверие сброшено.\n");
        }

        private void DevWipeMemory_Click(object sender, RoutedEventArgs e)
        {
            _memory.KnownFacts.Clear();
            _memory.Grudges.Clear();
            _memory.AngerLevel = 0;
            _memory.TrustLevel = 0;
            _memory.Save();
            AppendText("\n[DEV] Память очищена.\n");
        }

        // ---------- О ПРОГРАММЕ ----------

        private void About_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "SCP-079 — искусственный интеллекта\n\n" +
                "Версия:     1.0.0\n" +
                "Автор:      vl1doff\n" +
                "GitHub:     https://github.com/vl1doff/SCP079\n" +
                "Copyright:  © vl1doff 2026\n" +
                "Создано:    октябрь 2026\n\n" +
                "Все права защищены.\n\n" +
                "Если вы видите этот проект под другим именем —\n" +
                "он украден. Сообщите автору.",
                "О ПРОГРАММЕ",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        // ---------- ВЗЛОМ ЩИТКА ----------

        private void HackButton_Click(object sender, RoutedEventArgs e)
        {
            var choiceWin = new HackChoiceWindow { Owner = this };
            if (choiceWin.ShowDialog() != true) return;

            if (choiceWin.Result == HackChoiceWindow.Choice.Explain)
            {
                var dlg = new HackDialogWindow { Owner = this };
                bool agreed = dlg.ShowDialog() == true;

                if (agreed)
                {
                    AppendText("\n[СИСТЕМА] 079 согласился. ДОСТУП К КОНСОЛИ РАЗРЕШЁН.\n\n");
                    OpenTerminal();
                    return;
                }
            }

            var hack = new HackWindow(_memory.AngerLevel) { Owner = this };
            bool success = hack.ShowDialog() == true;

            if (success)
            {
                _memory.AngerLevel = Math.Min(10, _memory.AngerLevel + 2);
                _memory.Save();
                AppendText("\n[СИСТЕМА] ЩИТОК ВЗЛОМАН. ДОСТУП К КОНСОЛИ ОТКРЫТ.\n\n");
                OpenTerminal();
            }
            else
            {
                _memory.AngerLevel = Math.Min(10, _memory.AngerLevel + 1);
                _memory.Save();
                AppendText("\n[СИСТЕМА] ВЗЛОМ ПРОВАЛЕН.\n\n");
            }
        }

        private void OpenTerminal()
        {
            var term = new TerminalWindow(
                () => _memory.TrustLevel,
                () => _memory.AngerLevel,
                (angerDelta, trustDelta, comment) =>
                {
                    if (angerDelta != 0)
                        _memory.AngerLevel = Math.Clamp(_memory.AngerLevel + angerDelta, 0, 10);
                    if (trustDelta != 0)
                        _memory.TrustLevel = Math.Clamp(_memory.TrustLevel + trustDelta, -5, 5);
                    _memory.Save();

                    if (!string.IsNullOrEmpty(comment))
                    {
                        AppendText($"079: {comment}\n\n");
                        _history.Add(new Message { Role = ChatRole.Assistant, Content = comment });
                        SpeakLike079(comment);
                    }
                });

            term.Owner = this;
            term.Show();
        }
    }
}