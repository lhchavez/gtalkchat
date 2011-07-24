using System;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;

namespace gtalkchat {
    public partial class ContactListPage : PhoneApplicationPage {
        private readonly GoogleTalkHelper gtalkHelper;

        public ContactListPage() {
            InitializeComponent();

            gtalkHelper = App.Current.GtalkHelper;

            AllContactsListBox.ItemsSource = App.Current.Roster;
            OnlineContactsListBox.ItemsSource = App.Current.Roster.GetOnlineContacts();

            gtalkHelper.RosterUpdated += () =>
                Dispatcher.BeginInvoke(
                    () => OnlineContactsListBox.ItemsSource = App.Current.Roster.GetOnlineContacts()
                );
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            gtalkHelper.LoginIfNeeded();
        }

        private void ContactsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count > 0) {
                var to = (e.AddedItems[0] as Contact).JID;
                (sender as ListBox).SelectedIndex = -1;
                NavigationService.Navigate(new Uri("/ChatPage.xaml?from=" + to, UriKind.Relative));
            }
        }
    }
}