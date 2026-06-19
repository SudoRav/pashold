using pashold.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace pashold
{
    public partial class MainWindow : Window
    {
        private const double DeleteBlockHoldSeconds = 3;
        private const double DeletePasswordHoldSeconds = 1.5;
        private readonly Dictionary<Button, HoldDeleteState> _holdDeleteStates = new Dictionary<Button, HoldDeleteState>();
        private bool isshowpas = false;
        public MainWindow()
        {
            InitializeComponent();

            btn_ShowPassword.Content = "Скрывать пароль";
        }

        private void PasswordBox_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is PasswordItem passwordItem)
            {
                passwordItem.IsContentVisible = true;
                Clipboard.SetText(passwordItem.Content);

                if (!isshowpas)
                    passwordItem.IsContentVisible = false;

                tb.Focus();
                tb.CaretIndex = tb.Text.Length;
                tb.SelectAll();
            }
        }

        private void PasswordBox_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is PasswordItem passwordItem)
            {
                passwordItem.IsContentVisible = false; // скрываем пароль
                //tb.Focus();
                tb.CaretIndex = tb.Text.Length;
            }
        }

        private void btn_ShowPassword_Click(object sender, RoutedEventArgs e)
        {
            isshowpas = !isshowpas;

            if (isshowpas)
                btn_ShowPassword.Content = "Показывать пароль";
            else
                btn_ShowPassword.Content = "Скрывать пароль";
        }

        private void DeleteBlockButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartHoldDelete(sender, TimeSpan.FromSeconds(DeleteBlockHoldSeconds), DeleteTarget.Block);
            e.Handled = true;
        }

        private void DeletePasswordButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartHoldDelete(sender, TimeSpan.FromSeconds(DeletePasswordHoldSeconds), DeleteTarget.Password);
            e.Handled = true;
        }

        private void DeleteButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            CancelHoldDelete(sender);
            e.Handled = true;
        }

        private void DeleteButton_MouseLeave(object sender, MouseEventArgs e)
        {
            CancelHoldDelete(sender);
        }

        private void StartHoldDelete(object sender, TimeSpan holdDuration, DeleteTarget target)
        {
            if (!(sender is Button button))
                return;

            CancelHoldDelete(button);

            var progressBar = FindDeleteProgressBar(button);
            if (progressBar == null)
                return;

            var state = new HoldDeleteState
            {
                ProgressBar = progressBar,
                HoldDuration = holdDuration,
                Target = target
            };

            progressBar.Value = 0;
            state.Stopwatch.Start();
            state.Timer.Tick += (s, e) => UpdateHoldDelete(button);
            _holdDeleteStates[button] = state;
            button.CaptureMouse();
            state.Timer.Start();
        }

        private void UpdateHoldDelete(Button button)
        {
            if (!_holdDeleteStates.TryGetValue(button, out var state))
                return;

            var progress = state.Stopwatch.Elapsed.TotalMilliseconds / state.HoldDuration.TotalMilliseconds * 100;
            state.ProgressBar.Value = Math.Min(100, progress);

            if (progress < 100)
                return;

            StopHoldDelete(button, resetProgress: false);
            ExecuteDelete(button, state.Target);
        }

        private void CancelHoldDelete(object sender)
        {
            if (sender is Button button)
                StopHoldDelete(button, resetProgress: true);
        }

        private void StopHoldDelete(Button button, bool resetProgress)
        {
            if (!_holdDeleteStates.TryGetValue(button, out var state))
                return;

            state.Timer.Stop();
            state.Stopwatch.Stop();
            _holdDeleteStates.Remove(button);

            if (button.IsMouseCaptured)
                button.ReleaseMouseCapture();

            if (resetProgress)
                state.ProgressBar.Value = 0;
        }

        private void ExecuteDelete(Button button, DeleteTarget target)
        {
            var command = button.Command;
            var commandParameter = button.CommandParameter;

            if (command?.CanExecute(commandParameter) == true)
                command.Execute(commandParameter);
        }

        private ProgressBar FindDeleteProgressBar(Button button)
        {
            if (!(button.Parent is Panel panel))
                return null;

            var buttonColumn = Grid.GetColumn(button);
            foreach (UIElement child in panel.Children)
            {
                if (child is ProgressBar progressBar && Grid.GetColumn(progressBar) == buttonColumn)
                    return progressBar;
            }

            return FindVisualChild<ProgressBar>(panel);
        }

        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
                return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var descendant = FindVisualChild<T>(child);
                if (descendant != null)
                    return descendant;
            }

            return null;
        }

        private enum DeleteTarget
        {
            Block,
            Password
        }

        private class HoldDeleteState
        {
            public ProgressBar ProgressBar { get; set; }
            public TimeSpan HoldDuration { get; set; }
            public DeleteTarget Target { get; set; }
            public Stopwatch Stopwatch { get; } = new Stopwatch();
            public DispatcherTimer Timer { get; } = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        }
    }
}