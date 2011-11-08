using System;
using System.IO.IsolatedStorage;
using System.Security.Cryptography;
using System.Text;
using Gchat.Utilities;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows;
using System.Linq;
using System.Collections.Generic;
using FlurryWP7SDK.Models;

namespace Gchat.Pages {
    public partial class Login : PhoneApplicationPage {
        private readonly IsolatedStorageSettings settings;

        // Constructor
        public Login() {
            InitializeComponent();
            CreateAppbar();

            settings = IsolatedStorageSettings.ApplicationSettings;

            if (settings.Contains("username")) {
                Username.Text = settings["username"] as string;
            }

            if (settings.Contains("password")) {
                var passBytes = ProtectedData.Unprotect(settings["password"] as byte[], null);
                Password.Password = Encoding.UTF8.GetString(passBytes, 0, passBytes.Length);
            }
        }

        private void CreateAppbar() {
            ApplicationBar = new ApplicationBar();

            var login = new ApplicationBarIconButton();
            login.Text = AppResources.Login_AppbarLogin;
            login.IconUri = new Uri("/icons/appbar.next.rest.png", UriKind.Relative);
            login.Click += Login_Click;
            ApplicationBar.Buttons.Add(login);
        }

        private void ShowProgressBar(string text) {
            SystemTray.SetProgressIndicator(this, new ProgressIndicator {
                IsIndeterminate = true,
                IsVisible = true,
                Text = text
            });
        }

        private void HideProgressBar() {
            SystemTray.SetProgressIndicator(this, new ProgressIndicator {
                IsVisible = false,
            });
        }

        private void Login_Click(object sender, EventArgs e) {
            if (settings.Contains("username") && ((string) settings["username"]) == Username.Text &&
                (settings.Contains("auth") || (settings.Contains("token") && settings.Contains("rootUrl")))) {
                NavigationService.GoBack();
                return;
            }

            // track login attempt
            FlurryWP7SDK.Api.LogEvent("Login started", true);

            ShowProgressBar("Logging in...");
            Username.IsEnabled = false;
            Password.IsEnabled = false;
            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false;

            settings["username"] = Username.Text;
            settings["password"] = ProtectedData.Protect(Encoding.UTF8.GetBytes(Password.Password), null);
            settings.Save();

            GoogleTalkHelper.GoogleLogin(
                Username.Text,
                Password.Password,
                token => Dispatcher.BeginInvoke(() => {
                    settings["auth"] =
                        ProtectedData.Protect(
                            Encoding.UTF8.GetBytes(token), null
                        );
                    settings.Save();

                    var par = new List<Parameter>();
                    par.Add(new Parameter("Result", "success"));
                    FlurryWP7SDK.Api.EndTimedEvent("Login started", par);

                    NavigationService.GoBack();
                }),
                error => Dispatcher.BeginInvoke(() => {
                    MessageBox.Show(error, AppResources.Error_AuthErrorTitle, MessageBoxButton.OK);

                    // track unsuccessful login
                    var par = new List<Parameter>();
                    par.Add(new Parameter("Result", "auth error"));
                    FlurryWP7SDK.Api.EndTimedEvent("Login started", par);

                    HideProgressBar();
                    Username.IsEnabled = true;
                    Password.IsEnabled = true;
                    (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true;
                })
            );
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e) {
            base.OnBackKeyPress(e);

            if (App.Current.RootFrame.BackStack.Count() > 0) {
                App.Current.RootFrame.RemoveBackEntry();
            }
        }

        private void Password_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == System.Windows.Input.Key.Enter) {
                Focus();
                Login_Click(sender, e);
            }
        }
    }
}