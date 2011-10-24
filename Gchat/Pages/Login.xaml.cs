using System;
using System.IO.IsolatedStorage;
using System.Security.Cryptography;
using System.Text;
using Gchat.Utilities;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows;
using System.Linq;

namespace Gchat.Pages {
    public partial class Login : PhoneApplicationPage {
        private readonly IsolatedStorageSettings settings;

        // Constructor
        public Login() {
            InitializeComponent();

            settings = IsolatedStorageSettings.ApplicationSettings;

            if (settings.Contains("username")) {
                Username.Text = settings["username"] as string;
            }

            if (settings.Contains("password")) {
                var passBytes = ProtectedData.Unprotect(settings["password"] as byte[], null);
                Password.Password = Encoding.UTF8.GetString(passBytes, 0, passBytes.Length);
            }
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
                token =>
                Dispatcher.BeginInvoke(() => {
                    settings["auth"] =
                        ProtectedData.Protect(
                            Encoding.UTF8.GetBytes(token), null
                        );
                    settings.Save();

                    NavigationService.GoBack();
                }),
                error =>
                Dispatcher.BeginInvoke(() => {
                    MessageBox.Show("Authentication error: " + error);

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