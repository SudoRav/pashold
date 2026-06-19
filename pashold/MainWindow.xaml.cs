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
using System.Windows.Media.Animation;
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
        }

        private void TextBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb && tb.DataContext is PasswordItem passwordItem)
            {
                passwordItem.IsContentVisible = true;
                Clipboard.SetText(passwordItem.Content);
                passwordItem.IsContentVisible = false;
            }
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
            StartHoldDelete(sender, TimeSpan.FromSeconds(DeleteBlockHoldSeconds));
            e.Handled = true;
        }

        private void DeletePasswordButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            StartHoldDelete(sender, TimeSpan.FromSeconds(DeletePasswordHoldSeconds));
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

        private void StartHoldDelete(object sender, TimeSpan holdDuration)
        {
            if (!(sender is Button button))
                return;

            CancelHoldDelete(button);

            var progressBar = FindDeleteProgressBar(button);
            if (progressBar == null)
                return;

            var state = new HoldDeleteState
            {
                ProgressBar = progressBar
            };

            var animation = new DoubleAnimation
            {
                From = 0,
                To = 100,
                Duration = new Duration(holdDuration),
                FillBehavior = FillBehavior.HoldEnd
            };

            animation.Completed += (s, e) => CompleteHoldDelete(button, state);

            progressBar.Value = 0;
            _holdDeleteStates[button] = state;
            button.CaptureMouse();
            progressBar.BeginAnimation(ProgressBar.ValueProperty, animation);
        }

        private void CompleteHoldDelete(Button button, HoldDeleteState completedState)
        {
            if (!_holdDeleteStates.TryGetValue(button, out var currentState) || !ReferenceEquals(currentState, completedState))
                return;

            StopHoldDelete(button, resetProgress: false);
            ExecuteDelete(button);
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

            _holdDeleteStates.Remove(button);
            state.ProgressBar.BeginAnimation(ProgressBar.ValueProperty, null);

            if (button.IsMouseCaptured)
                button.ReleaseMouseCapture();

            state.ProgressBar.Value = resetProgress ? 0 : 100;
        }

        private void ExecuteDelete(Button button)
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

        private class HoldDeleteState
        {
            public ProgressBar ProgressBar { get; set; }
        }
    }
}