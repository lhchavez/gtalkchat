using System;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Phone.Controls;

namespace gtalkchat {
    public partial class ContactListPage : PhoneApplicationPage {
        private readonly IsolatedStorageSettings settings;
        private readonly GoogleTalk gtalk;
        private readonly GoogleTalkHelper gtalkHelper;

        public ContactListPage() {
            InitializeComponent();

            settings = App.Current.Settings;
            gtalk = App.Current.GtalkClient;
            gtalkHelper = App.Current.GtalkHelper;

            if (gtalkHelper.Connected) {
                LoadRoster();
            }
            gtalkHelper.Connect += LoadRoster;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            gtalkHelper.LoginIfNeeded();
        }

        public void LoadRoster() {
            gtalk.GetRoster(
                roster => {
                    var all = roster.ToObservableCollection();
                    var online = roster.Where(r => r.Online).ToObservableCollection();
                    Dispatcher.BeginInvoke(() => {
                        OnlineContactsListBox.ItemsSource = online;
                        AllContactsListBox.ItemsSource = all;
                    });
                },
                e => {
                    if (e.StartsWith("403")) {
                        // Your token has expired. You'll have to re-login.

                        Dispatcher.BeginInvoke(() => {
                            settings.Remove("token");
                            settings.Save();

                            MessageBox.Show("Your authentication token has expired. Try logging in again.");
                            NavigationService.Navigate(new Uri("/LoginPage.xaml", UriKind.Relative));
                        });
                    }
                }
            );
        }

        private void ContactsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count > 0) {
                NavigationService.Navigate(new Uri("/ChatPage.xaml", UriKind.Relative));
            }
        }
    }
}