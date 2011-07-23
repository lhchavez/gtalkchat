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

        public ContactListPage()
        {
            InitializeComponent();

            settings = App.Current.Settings;
            gtalk = App.Current.GtalkClient;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (settings.Contains("token"))
            {
                var tokenBytes = ProtectedData.Unprotect(settings["token"] as byte[], null);
                gtalk = new GoogleTalk(Encoding.UTF8.GetString(tokenBytes, 0, tokenBytes.Length));
                LoadRoster();
            }
            else if (settings.Contains("auth"))
            {
                var authBytes = ProtectedData.Unprotect(settings["auth"] as byte[], null);
                gtalk = new GoogleTalk(
                    settings["username"] as string,
                    Encoding.UTF8.GetString(authBytes, 0, authBytes.Length),
                    token =>
                    {
                        settings["token"] = ProtectedData.Protect(Encoding.UTF8.GetBytes(token), null);
                        settings.Save();
                        LoadRoster();
                    },
                    error =>
                    {
                        Dispatcher.BeginInvoke(() =>
                        {
                            if (error.StartsWith("401"))
                            {
                                // stale auth token. get a new one and we should be all happy again.
                                settings.Remove("auth");
                                settings.Save();
                            }

                            MessageBox.Show(error);
                            NavigationService.Navigate(new Uri("/LoginPage.xaml", UriKind.Relative));
                        });
                    }
                );
            }
            else
            {
                // No token or auth, load login page
                NavigationService.Navigate(new Uri("/LoginPage.xaml", UriKind.Relative));
            }
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
                    if (e.StartsWith("404")) 
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