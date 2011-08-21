using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Phone.Controls;
using System.Threading;

namespace gtalkchat.Pages {
    public partial class ContactList : PhoneApplicationPage {
        private GoogleTalkHelper gtalkHelper;
        private bool reloadedRoster;

        public ContactList() {
            InitializeComponent();

            AllContactsListBox.ItemsSource = App.Current.Roster;
            OnlineContactsListBox.ItemsSource = App.Current.Roster.GetOnlineContacts();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            if (gtalkHelper != App.Current.GtalkHelper) {
                gtalkHelper = App.Current.GtalkHelper;

                Dispatcher.BeginInvoke(() => AllContactsListBox.ItemsSource = App.Current.Roster);
            }

            gtalkHelper.RosterUpdated += RosterLoaded;

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

            if(gtalkHelper.RosterLoaded) {
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

                    if(!reloadedRoster && onlineContacts.Count == 0) {
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

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e) {
            base.OnNavigatedFrom(e);

            gtalkHelper.MessageReceived -= gtalkHelper.ShowToast;
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