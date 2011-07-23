using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using System.IO;
using System.Windows.Threading;
using System.Security.Cryptography;
using System.IO.IsolatedStorage;

namespace gtalkchat
{
    public partial class LoginPage : PhoneApplicationPage
    {
        public delegate void LoginCallback(string token);
        private IsolatedStorageSettings settings;

        // Constructor
        public LoginPage()
        {
            InitializeComponent();

            settings = IsolatedStorageSettings.ApplicationSettings;

            if (settings.Contains("username")) username.Text = settings["username"] as string;
            if (settings.Contains("password"))
            {
                var passBytes = ProtectedData.Unprotect(settings["password"] as byte[], null);
                password.Password = Encoding.UTF8.GetString(passBytes, 0, passBytes.Length);
            }
        }

        private void login_Click(object sender, RoutedEventArgs e)
        {
            if (settings.Contains("username") && ((string)settings["username"]) == username.Text && (settings.Contains("auth") || settings.Contains("token")))
            {
                NavigationService.GoBack();
                return;
            }

            settings["username"] = username.Text;
            settings["password"] = ProtectedData.Protect(Encoding.UTF8.GetBytes(password.Password), null);
            settings.Save();

            GoogleLogin(username.Text, password.Password, token =>
            {
                Dispatcher.BeginInvoke(() =>
                {
                    settings["auth"] = ProtectedData.Protect(Encoding.UTF8.GetBytes(token), null);
                    settings.Save();

                    NavigationService.GoBack();
                });
            });
        }

        public void GoogleLogin(string username, string password, LoginCallback callback)
        {
            var data = Encoding.UTF8.GetBytes(
                "accountType=HOSTED_OR_GOOGLE" +
                "&Email=" + HttpUtility.UrlEncode(username) +
                "&Passwd=" + HttpUtility.UrlEncode(password) +
                "&service=mail" +
                "&source=lhchavez.com-gtalkchat-1.0"
            );

            var req = HttpWebRequest.CreateHttp("https://www.google.com/accounts/ClientLogin");

            req.ContentType = "application/x-www-form-urlencoded";
            req.Method = "POST";
            req.AllowReadStreamBuffering = true;
            req.Headers["Content-Length"] = data.Length.ToString();

            req.BeginGetRequestStream(ar =>
            {
                using (var requestStream = req.EndGetRequestStream(ar))
                {
                    requestStream.Write(data, 0, data.Length);
                }

                req.BeginGetResponse(a =>
                {
                    try
                    {
                        var response = req.EndGetResponse(a) as HttpWebResponse;

                        var responseStream = response.GetResponseStream();
                        using (var sr = new StreamReader(responseStream))
                        {
                            string line;

                            while (!(line = sr.ReadLine()).StartsWith("Auth=")) ;

                            callback(line.Split(new char[] { '=' })[1]);
                        }
                    }
                    catch (WebException e)
                    {
                        var response = e.Response as HttpWebResponse;

                        try
                        {
                            using (var responseStream = response.GetResponseStream())
                            {
                                using (var sr = new StreamReader(responseStream))
                                {
                                    string message = "Authentication error:\n" + sr.ReadToEnd();
                                    Dispatcher.BeginInvoke(() => MessageBox.Show(message));
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // What is wrong with this platform?!
                            Dispatcher.BeginInvoke(() => MessageBox.Show("Authentication error:\n" + ex.Message + "\n" + e.Message));
                        }
                    }
                }, null);
            }, null);
        }
    }
}
