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
    public partial class ContactList : PhoneApplicationPage
    {
        private IsolatedStorageSettings settings;
        private GoogleTalk gtalk;

        public ContactList()
        {
            InitializeComponent();

            settings = IsolatedStorageSettings.ApplicationSettings;
        }

        protected override void  OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (settings.Contains("token"))
            {
                var tokenBytes = ProtectedData.Unprotect(settings["token"] as byte[], null);
                gtalk = new GoogleTalk(Encoding.UTF8.GetString(tokenBytes, 0, tokenBytes.Length));
                LoadRoster();
            }
            else
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
                            NavigationService.GoBack();
                        });
                    }
                );
            }
        }

        public void LoadRoster() 
        {
            var contacts = gtalk.GetRoster(c => { }, e => { });
            ContactsListBox.ItemsSource = contacts;
        }
    }
}