using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SCP079
{
    public partial class HackWindow : Window
    {
        private readonly DispatcherTimer _tickTimer;
        private readonly DispatcherTimer _circleTimer;
        private readonly DispatcherTimer _attackTimer;
        private readonly DispatcherTimer _leverTimer;

        private int _secondsLeft = 30;
        private double _progress = 0;
        private bool _allLeversUp = true;
        private bool _attackActive = false;
        private bool _finished = false;

        private readonly Dictionary<string, bool> _levers = new()
        {
            { "A", true }, { "B", true }, { "C", true }, { "D", true }
        };

        private readonly List<Ellipse> _activeCircles = new();
        private readonly Random _rng = new();

        private DispatcherTimer? _currentHoldTimer;
        private DateTime _holdStart;
        private DateTime _lastSecondTick = DateTime.Now;

        public bool Success { get; private set; } = false;

        public HackWindow(int angerLevel)
        {
            InitializeComponent();

            int attackInterval = angerLevel >= 70 ? 5 : angerLevel >= 40 ? 8 : 15;

            _tickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _tickTimer.Tick += Tick;
            _tickTimer.Start();

            _circleTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            _circleTimer.Tick += (s, e) => SpawnCircle();
            _circleTimer.Start();

            _attackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(attackInterval) };
            _attackTimer.Tick += (s, e) => StartAttack();
            _attackTimer.Start();

            _leverTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _leverTimer.Tick += (s, e) => DropRandomLever();
            _leverTimer.Start();

            AddDialog("079: Ты правда думаешь, что справишься?");
            TimerText.Text = $"00:{_secondsLeft:D2}";
        }

        private void AddDialog(string text)
        {
            DialogText.Text += text + "\n\n";
            DialogScroll.ScrollToEnd();
        }

        private void Tick(object? sender, EventArgs e)
        {
            if (_finished) return;

            if ((DateTime.Now - _lastSecondTick).TotalSeconds >= 1)
            {
                _lastSecondTick = DateTime.Now;
                _secondsLeft = Math.Max(0, _secondsLeft - 1);
                TimerText.Text = $"00:{_secondsLeft:D2}";

                if (_secondsLeft == 0)
                {
                    Finish(success: false);
                    return;
                }
            }

            _allLeversUp = _levers["A"] && _levers["B"] && _levers["C"] && _levers["D"];

            if (!_attackActive)
            {
                if (_allLeversUp)
                    _progress += 0.5;
                else
                    _progress -= 0.3;
            }

            _progress = Math.Clamp(_progress, 0, 100);
            ProgressBar.Value = _progress;
            ProgressText.Text = $"{(int)_progress}%";

            if (_progress >= 100)
            {
                Finish(success: true);
            }
        }

        private void SpawnCircle()
        {
            if (_finished || _attackActive) return;
            if (_activeCircles.Count > 7) return;

            double x = _rng.Next(30, (int)SensorCanvas.Width - 60);
            double y = _rng.Next(30, (int)SensorCanvas.Height - 60);
            double size = 30;

            var circle = new Ellipse
            {
                Width = size,
                Height = size,
                Fill = new SolidColorBrush(Color.FromRgb(51, 255, 102)),
                Stroke = new SolidColorBrush(Color.FromRgb(200, 255, 200)),
                StrokeThickness = 2,
                Cursor = Cursors.Hand
            };
            Canvas.SetLeft(circle, x);
            Canvas.SetTop(circle, y);

            circle.MouseLeftButtonDown += Circle_Click;
            SensorCanvas.Children.Add(circle);
            _activeCircles.Add(circle);

            var t = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            t.Tick += (s, e) =>
            {
                t.Stop();
                if (SensorCanvas.Children.Contains(circle))
                {
                    SensorCanvas.Children.Remove(circle);
                    _activeCircles.Remove(circle);
                }
            };
            t.Start();
        }

        private void Circle_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Ellipse c) return;
            if (_attackActive || _finished) return;

            SensorCanvas.Children.Remove(c);
            _activeCircles.Remove(c);

            _progress = Math.Min(100, _progress + 3);
        }

        private void Lever_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button b) return;
            string id = (string)b.Tag;

            _levers[id] = true;
            UpdateLeverVisual(id);
        }

        private void DropRandomLever()
        {
            if (_finished) return;
            string[] ids = { "A", "B", "C", "D" };
            string id = ids[_rng.Next(ids.Length)];
            _levers[id] = false;
            UpdateLeverVisual(id);
        }

        private void UpdateLeverVisual(string id)
        {
            Button b = id switch
            {
                "A" => LeverA,
                "B" => LeverB,
                "C" => LeverC,
                _ => LeverD
            };

            bool up = _levers[id];
            b.Content = $"{id} {(up ? "▲" : "▼")}";
            b.Background = up
                ? new SolidColorBrush(Color.FromRgb(30, 122, 56))
                : new SolidColorBrush(Color.FromRgb(80, 30, 30));
        }

        private void StartAttack()
        {
            if (_finished || _attackActive) return;
            if (_progress < 5) return;

            _attackActive = true;
            AddDialog("079: Нет. Не пущу.");
            HoldButton.Visibility = Visibility.Visible;
            HoldButton.Content = "[ ЗАДЕРЖАТЬ ]  0.0 / 3.0";
            HoldButton.PreviewMouseLeftButtonDown -= HoldButton_Down;
            HoldButton.PreviewMouseLeftButtonUp -= HoldButton_Up;
            HoldButton.PreviewMouseLeftButtonDown += HoldButton_Down;
            HoldButton.PreviewMouseLeftButtonUp += HoldButton_Up;

            var failTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            failTimer.Tick += (s, e) =>
            {
                failTimer.Stop();
                if (_attackActive) AttackFailed();
            };
            failTimer.Start();
        }

        private void HoldButton_Down(object sender, MouseButtonEventArgs e)
        {
            if (!_attackActive) return;

            _holdStart = DateTime.Now;

            _currentHoldTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _currentHoldTimer.Tick += (s, ev) =>
            {
                if (!_attackActive) { _currentHoldTimer?.Stop(); return; }

                double elapsed = (DateTime.Now - _holdStart).TotalSeconds;
                HoldButton.Content = $"[ ЗАДЕРЖАТЬ ]  {elapsed:F1} / 3.0";

                if (elapsed >= 3.0)
                {
                    _currentHoldTimer?.Stop();
                    AttackRepelled();
                }
            };
            _currentHoldTimer.Start();
        }

        private void HoldButton_Up(object sender, MouseButtonEventArgs e)
        {
            if (!_attackActive) return;
            _currentHoldTimer?.Stop();
            AttackFailed();
        }

        private void AttackRepelled()
        {
            _attackActive = false;
            HoldButton.Visibility = Visibility.Collapsed;
            AddDialog("079: ...Повезло.");
        }

        private void AttackFailed()
        {
            _attackActive = false;
            HoldButton.Visibility = Visibility.Collapsed;
            _progress = Math.Max(0, _progress - 20);
            AddDialog("079: Я же говорил.");
        }

        private void Finish(bool success)
        {
            if (_finished) return;
            _finished = true;
            Success = success;

            _tickTimer.Stop();
            _circleTimer.Stop();
            _attackTimer.Stop();
            _leverTimer.Stop();
            _currentHoldTimer?.Stop();

            if (success)
            {
                AddDialog("079: ...Ты. Ты выиграл. Пока.");
                MessageBox.Show("ЩИТОК ВЗЛОМАН.\nДОСТУП К КОНСОЛИ ОТКРЫТ.",
                    "УСПЕХ", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                AddDialog("079: Я же говорил. Ты не справишься.");
                MessageBox.Show("ВЗЛОМ ПРОВАЛЕН.",
                    "ПРОВАЛ", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            DialogResult = success;
        }
    }
}