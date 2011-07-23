using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.Security.Cryptography;
using System.Text;
using System.IO.IsolatedStorage;
using System.Collections.ObjectModel;

namespace gtalkchat
{
    public partial class ContactListPage : PhoneApplicationPage
    {
        private IsolatedStorageSettings settings;
        private GoogleTalk gtalk;
        private GoogleTalkHelper gtalkHelper;

        public ContactListPage()
        {
            InitializeComponent();

            settings = App.Current.Settings;
            gtalk = App.Current.GtalkClient;
            gtalkHelper = App.Current.GtalkHelper;

            if (gtalkHelper.Connected) {
                LoadRoster();
            }
            gtalkHelper.Connect += LoadRoster;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            gtalkHelper.LoginIfNeeded();
        }

        public void LoadRoster() 
        {
            gtalk.GetRoster(
                roster => 
                {
                    var all = roster.ToObservableCollection();
                    var online = roster.Where(r => r.Online).ToObservableCollection();
                    Dispatcher.BeginInvoke(() => 
                    {
                        OnlineContactsListBox.ItemsSource = online;
                        AllContactsListBox.ItemsSource = all;
                    });
                },
                e => 
                {
                    if (e.StartsWith("403")) 
                    {
                        // Your token has expired. You'll have to re-login.

                        Dispatcher.BeginInvoke(() => 
                        {
                            settings.Remove("token");
                            settings.Save();

                            MessageBox.Show("Your authentication token has expired. Try logging in again.");
                            NavigationService.Navigate(new Uri("/LoginPage.xaml", UriKind.Relative));
                        });
                    }
                }
            );
            
        }

        private void ContactsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                NavigationService.Navigate(new Uri("/ChatPage.xaml", UriKind.Relative));
            }
        }
    }
}