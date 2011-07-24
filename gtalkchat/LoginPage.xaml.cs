using System;
using System.IO;
using System.IO.IsolatedStorage;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Microsoft.Phone.Controls;

namespace gtalkchat {
    public partial class LoginPage : PhoneApplicationPage {
        public delegate void LoginCallback(string token);

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

            GoogleLogin(
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

        public void GoogleLogin(string username, string password, LoginCallback callback) {
            var data = Encoding.UTF8.GetBytes(
                "accountType=HOSTED_OR_GOOGLE" +
                "&Email=" + HttpUtility.UrlEncode(username) +
                "&Passwd=" + HttpUtility.UrlEncode(password) +
                "&service=mail" +
                "&source=lhchavez.com-gtalkchat-1.0"
            );

            var req = WebRequest.CreateHttp("https://www.google.com/accounts/ClientLogin");

            req.ContentType = "application/x-www-form-urlencoded";
            req.Method = "POST";
            req.AllowReadStreamBuffering = true;
            req.Headers["Content-Length"] = data.Length.ToString();

            req.BeginGetRequestStream(
                ar => {
                    using (var requestStream = req.EndGetRequestStream(ar)) {
                        requestStream.Write(data, 0, data.Length);
                    }

                    req.BeginGetResponse(
                        a => {
                            try {
                                var response = req.EndGetResponse(a) as HttpWebResponse;

                                var responseStream = response.GetResponseStream();
                                using (var sr = new StreamReader(responseStream)) {
                                    string line;

                                    while ((line = sr.ReadLine()) != null && !line.StartsWith("Auth=")) {
                                    }

                                    callback(line.Split(new[] {'='})[1]);
                                }
                            } catch (WebException e) {
                                var response = e.Response as HttpWebResponse;

                                try {
                                    using (var responseStream = response.GetResponseStream()) {
                                        using (var sr = new StreamReader(responseStream)) {
                                            string message = "Authentication error:\n" + sr.ReadToEnd();
                                            Dispatcher.BeginInvoke(() => MessageBox.Show(message));
                                        }
                                    }
                                } catch (Exception ex) {
                                    // What is wrong with this platform?!
                                    Dispatcher.BeginInvoke(
                                        () => MessageBox.Show("Authentication error:\n" + ex.Message + "\n" + e.Message)
                                    );
                                }
                            }
                        },
                        null
                    );
                },
                null
            );
        }
    }
}