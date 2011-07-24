using System;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;
using Coding4Fun.Phone.Controls;
using System.Windows.Media.Imaging;

namespace gtalkchat {
    public partial class ContactListPage : PhoneApplicationPage {
        private readonly GoogleTalkHelper gtalkHelper;

        public ContactListPage() {
            InitializeComponent();

            gtalkHelper = App.Current.GtalkHelper;

            AllContactsListBox.ItemsSource = App.Current.Roster;
            OnlineContactsListBox.ItemsSource = App.Current.Roster.GetOnlineContacts();

            gtalkHelper.RosterUpdated += () =>
                Dispatcher.BeginInvoke(() => {
                    OnlineContactsListBox.ItemsSource = App.Current.Roster.GetOnlineContacts();
                });

            gtalkHelper.MessageReceived += gtalkHelper.ShowToast;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            gtalkHelper.LoginIfNeeded();
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
    }
}