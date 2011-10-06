using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Gchat.Data;
using Gchat.Utilities;
using Microsoft.Phone.Controls;
using System.Linq;
using System.Collections.Generic;
using Gchat.Protocol;

namespace Gchat.Pages {
    public partial class ContactList : PhoneApplicationPage {
        private GoogleTalkHelper gtalkHelper;
        private bool reloadedRoster;

        private Dictionary<UserStatus, string> status = new Dictionary<UserStatus,string> { 
            {UserStatus.Available, "available"}, 
            {UserStatus.Dnd, "busy"}, 
            {UserStatus.Away, "away"}
        };

        public ContactList() {
            InitializeComponent();

            AllContactsListBox.ItemsSource = App.Current.Roster;
            OnlineContactsListBox.ItemsSource = App.Current.Roster.GetOnlineContacts();

            StatusPicker.ItemsSource = status;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            if (App.Current.LastPage != null && App.Current.LastPage.StartsWith("/Pages/Chat.xaml?from=") && App.Current.RootFrame.BackStack.Count() > 0) {
                App.Current.RootFrame.RemoveBackEntry();
            }
            App.Current.LastPage = e.Uri.OriginalString;

            if (gtalkHelper != App.Current.GtalkHelper) {
                gtalkHelper = App.Current.GtalkHelper;

                Dispatcher.BeginInvoke(() => AllContactsListBox.ItemsSource = App.Current.Roster);
            }

            gtalkHelper.RosterUpdated += RosterLoaded;
            gtalkHelper.ConnectFailed += ConnectFailed;

            gtalkHelper.SetCorrectOrientation(this);

            Dispatcher.BeginInvoke(
                () => {
                    if (gtalkHelper.RosterLoaded) {
                        ProgressBar.Visibility = Visibility.Collapsed;
                        ProgressBar.IsIndeterminate = false;
                    } else {
                        ProgressBar.IsIndeterminate = true;
                        ProgressBar.Visibility = Visibility.Visible;
                    }
                });

            if (gtalkHelper.RosterLoaded && App.Current.GtalkClient.LoggedIn) {
                if (e.IsNavigationInitiator) {
                    gtalkHelper.GetOfflineMessages();
                }
            } else {
                Dispatcher.BeginInvoke(() => OnlineContactsListBox.ItemsSource = App.Current.Roster.GetOnlineContacts());
            }

            gtalkHelper.LoginIfNeeded();
            gtalkHelper.MessageReceived += gtalkHelper.ShowToast;
        }

        private void RosterLoaded() {
            Dispatcher.BeginInvoke(
                () => {
                    var onlineContacts = App.Current.Roster.GetOnlineContacts();

                    if (!reloadedRoster && onlineContacts.Count == 0) {
                        reloadedRoster = true;
                        var timer = new Timer(state => {
                            (state as Timer).Dispose();
                            gtalkHelper.LoadRoster();
                        });
                        timer.Change(1000, Timeout.Infinite);
                    } else {
                        ProgressBar.Visibility = Visibility.Collapsed;
                        ProgressBar.IsIndeterminate = false;
                        OnlineContactsListBox.ItemsSource = onlineContacts;
                    }
                }
            );
        }

        private void ConnectFailed(string message, string title) {
            Dispatcher.BeginInvoke(
                () => {
                    ProgressBar.Visibility = Visibility.Collapsed;
                    ProgressBar.IsIndeterminate = false;
                    gtalkHelper.ShowToast(message, title);
                });
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e) {
            base.OnNavigatedFrom(e);

            gtalkHelper.MessageReceived -= gtalkHelper.ShowToast;
            gtalkHelper.ConnectFailed -= ConnectFailed;
        }

        private void ContactsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count > 0) {
                var to = (e.AddedItems[0] as Contact).Email;
                (sender as ListBox).SelectedIndex = -1;
                NavigationService.Navigate(new Uri("/Pages/Chat.xaml?from=" + to, UriKind.Relative));
            }
        }

        private void SettingsButton_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/Pages/Settings.xaml", UriKind.Relative));
        }

        private void RefreshButton_Click(object sender, EventArgs e) {
            Dispatcher.BeginInvoke(
                () => {
                    ProgressBar.IsIndeterminate = true;
                    ProgressBar.Visibility = Visibility.Visible;
                });

            if (gtalkHelper.Connected) {
                gtalkHelper.LoadRoster();
            } else {
                gtalkHelper.LoginIfNeeded();
            }
        }

        private void Logout_Click(object sender, EventArgs e) {
            var gtalkHelper = App.Current.GtalkHelper;
            gtalkHelper.Logout();
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e) {
            ContextMenu menu = sender as ContextMenu;
            Contact c = menu.DataContext as Contact;
            MenuItem item = menu.Items[0] as MenuItem;

            if (gtalkHelper.IsContactPinned(c.Email)) {
                item.IsEnabled = false;
            } else {
                item.IsEnabled = true;
            }
        }

        private void PinContact_Click(object sender, RoutedEventArgs e) {
            MenuItem item = sender as MenuItem;
            Contact c = item.DataContext as Contact;

            gtalkHelper.PinContact(c.Email);
        }

        private void SearchButton_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/Pages/Search.xaml", UriKind.Relative));
        }

        private void StatusButton_Click(object sender, EventArgs e) {
            StatusPicker.Open();
        }

        private void StatusPicker_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count > 0) {
                // TODO: complete this
                // note: this event is fired on first page load, take that into account
                var status = ((KeyValuePair<UserStatus, string>)e.AddedItems[0]).Key;
                App.Current.GtalkClient.SetStatus(status, (t) => { }, (t) => { });
            }
        }
    }

    public class NumberToVisibilityConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            int val = (int)value;

            if (val == 0) {
                return Visibility.Collapsed;
            } else {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }

    public class StatusToColorConverter : IValueConverter {

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            string status = (string)value;

            if (status == "available") {
                return App.Current.Resources["PhoneAccentBrush"];
            } else {
                return App.Current.Resources["PhoneSubtleBrush"];
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
            throw new NotImplementedException();
        }
    }
}