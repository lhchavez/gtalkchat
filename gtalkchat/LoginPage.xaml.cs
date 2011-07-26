using System;
using System.IO.IsolatedStorage;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Phone.Controls;

namespace gtalkchat {
    public partial class LoginPage : PhoneApplicationPage {
        private readonly IsolatedStorageSettings settings;

        // Constructor
        public LoginPage() {
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

        private void Login_Click(object sender, EventArgs e) {
            if (settings.Contains("username") && ((string) settings["username"]) == Username.Text &&
                (settings.Contains("auth") || settings.Contains("token"))) {
                NavigationService.GoBack();
                return;
            }

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
                })
            );
        }

        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e) {
            if (!settings.Contains("username") || !settings.Contains("password")) {
                throw new QuitException();
            }

            base.OnBackKeyPress(e);
        }
    }
}