using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Deskout.ViewModels;

namespace Deskout.Views
{
    public partial class ReminderWindow : Window
    {
        private bool _isTrulyClosing;

        public ReminderWindow(ReminderViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose = () => {
                this.Hide();
            };
        }

        public void SetTrulyClosing()
        {
            _isTrulyClosing = true;
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!_isTrulyClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            base.OnClosing(e);
        }

        private void SnoozeBtn_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var times = new[] { 5, 15, 30, 60, 120 };
            var vm = (ReminderViewModel)DataContext;
            
            foreach (var t in times)
            {
                var item = new MenuItem { Header = $"{t} Minutes" };
                item.Command = vm.SnoozeCommand;
                item.CommandParameter = t.ToString();
                menu.Items.Add(item);
            }
            
            menu.PlacementTarget = sender as UIElement;
            menu.IsOpen = true;
        }

        private void ShutdownOptionBtn_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu();
            var vm = (ReminderViewModel)DataContext;

            var restartItem = new MenuItem { Header = "Restart Anyway" };
            restartItem.Command = vm.RestartAnywayCommand;

            var logoffItem = new MenuItem { Header = "Sign Out Anyway" };
            logoffItem.Command = vm.LogoffAnywayCommand;

            menu.Items.Add(restartItem);
            menu.Items.Add(logoffItem);

            menu.PlacementTarget = sender as UIElement;
            menu.IsOpen = true;
        }
    }
}
