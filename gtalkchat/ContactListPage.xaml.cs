using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Microsoft.Phone.Controls;

namespace gtalkchat {
    public partial class ContactListPage : PhoneApplicationPage {
        private GoogleTalkHelper gtalkHelper;

        public ContactListPage() {
            InitializeComponent();

            AllContactsListBox.ItemsSource = App.Current.Roster;
            OnlineContactsListBox.ItemsSource = App.Current.Roster.GetOnlineContacts();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            if (gtalkHelper != App.Current.GtalkHelper) {
                gtalkHelper = App.Current.GtalkHelper;

                Dispatcher.BeginInvoke(() => AllContactsListBox.ItemsSource = App.Current.Roster);

                gtalkHelper.RosterUpdated += () =>
                    Dispatcher.BeginInvoke(
                        () => {
                            ProgressBar.Visibility = Visibility.Collapsed;
                            ProgressBar.IsIndeterminate = false;
                            OnlineContactsListBox.ItemsSource =
                                App.Current.Roster.GetOnlineContacts();
                        }
                    );
            }

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
                gtalkHelper.GetOfflineMessages();
            } else {
                Dispatcher.BeginInvoke(() => OnlineContactsListBox.ItemsSource = App.Current.Roster.GetOnlineContacts());
            }

            gtalkHelper.LoginIfNeeded();
            gtalkHelper.MessageReceived += gtalkHelper.ShowToast;
        }

        protected override void OnNavigatedFrom(System.Windows.Navigation.NavigationEventArgs e) {
            base.OnNavigatedFrom(e);

            gtalkHelper.MessageReceived -= gtalkHelper.ShowToast;
        }

        private void ContactsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count > 0) {
                var to = (e.AddedItems[0] as Contact).JID;
                (sender as ListBox).SelectedIndex = -1;
                NavigationService.Navigate(new Uri("/ChatPage.xaml?from=" + to, UriKind.Relative));
            }
        }

        private void SettingsButton_Click(object sender, EventArgs e) {
            NavigationService.Navigate(new Uri("/SettingsPage.xaml", UriKind.Relative));
        }

        private void RefreshButton_Click(object sender, EventArgs e) {
            ProgressBar.Visibility = Visibility.Visible;
            gtalkHelper.LoadRoster();
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
}