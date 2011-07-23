using System;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Threading;
using Microsoft.Phone.Shell;

namespace gtalkchat {
    public class GoogleTalkHelper {
        #region Public Events

        public delegate void MessageReceivedEventHandler(Message message);
        public event MessageReceivedEventHandler MessageReceived;

        public delegate void ConnectEventHandler();
        public event ConnectEventHandler Connect;

        #endregion

        #region Public Properties

        public bool Connected { get; private set; }

        #endregion

        private IsolatedStorageSettings settings;
        private GoogleTalk gtalk;
        private PushHelper pushHelper;
        private bool hasToken = false;
        private bool hasUri = false;
        private string registeredUri = null;

        public GoogleTalkHelper() {
            this.settings = App.Current.Settings;
            this.gtalk = App.Current.GtalkClient;
            this.pushHelper = App.Current.PushHelper;

            this.pushHelper.UriUpdated += UriUpdated;
            this.pushHelper.RawNotificationReceived += RawNotificationReceived;
            this.Connected = false;
        }

        public void LoginIfNeeded() {
            if(!App.Current.Settings.Contains("auth")) {
                App.Current.RootFrame.Dispatcher.BeginInvoke(
                    () => App.Current.RootFrame.Navigate(new Uri("/LoginPage.xaml", UriKind.Relative)));

                return;
            }

            if (gtalk.LoggedIn) {
                return;
            }

            this.Connected = false;

            if (settings.Contains("token")) {
                var tokenBytes = ProtectedData.Unprotect(settings["token"] as byte[], null);
                App.Current.GtalkClient.SetToken(Encoding.UTF8.GetString(tokenBytes, 0, tokenBytes.Length));

                TokenUpdated();
            } else {
                var authBytes = ProtectedData.Unprotect(settings["auth"] as byte[], null);
                App.Current.GtalkClient.Login(
                    settings["username"] as string,
                    Encoding.UTF8.GetString(authBytes, 0, authBytes.Length),
                    token => {
                        settings["token"] = ProtectedData.Protect(Encoding.UTF8.GetBytes(token), null);

                        TokenUpdated();
                    }, error => {
                        if (error.StartsWith("401")) {
                            // stale auth token. get a new one and we should be all happy again.
                            settings.Remove("auth");

                            App.Current.RootFrame.Dispatcher.BeginInvoke(() => {
                                MessageBox.Show("Your authentication token has expired. Try logging in again.");
                                App.Current.RootFrame.Navigate(new Uri("/LoginPage.xaml", UriKind.Relative));
                            });
                        } else {
                            App.Current.RootFrame.Dispatcher.BeginInvoke(() => MessageBox.Show(error));
                        }
                    }
                );
            }
        }

        public void UriUpdated(string uri)
        {
            this.hasUri = true;

            lock (this) {
                if (this.hasUri && this.hasToken) {
                    if (!uri.Equals(registeredUri)) {
                        registeredUri = uri;

                        Register(registeredUri);
                    }
                }
            }
        }

        public void TokenUpdated()
        {
            this.hasToken = true;

            lock (this) {
                if (this.hasUri && this.hasToken) {
                    if(!pushHelper.PushChannelUri.Equals(registeredUri)){
                        registeredUri = pushHelper.PushChannelUri;

                        Register(registeredUri);
                    }
                }
            }
        }

        private void Register(string uri) {
            if (!settings.Contains("clientkey")) {
                gtalk.GetKey(clientKey => {
                    settings["clientkey"] = ProtectedData.Protect(Encoding.UTF8.GetBytes(clientKey), null);

                    this.Register(uri, true);
                }, error => {
                    App.Current.RootFrame.Dispatcher.BeginInvoke(() => MessageBox.Show(error));

                    if (error.StartsWith("403")) {
                        settings.Remove("token");

                        this.LoginIfNeeded();
                    }
                });
            } else {
                this.Register(uri, false);
            }
        }

        private void Register(string uri, bool keySet) {
            if (!keySet) {
                var clientKeyBytes = ProtectedData.Unprotect(settings["clientkey"] as byte[], null);
                gtalk.SetKey(Encoding.UTF8.GetString(clientKeyBytes, 0, clientKeyBytes.Length));
            }

            gtalk.Register(uri, data =>
                {
                    this.GetOfflineMessages();

                    this.Connected = true;
                    Connect.Invoke();
                }, error => {
                App.Current.RootFrame.Dispatcher.BeginInvoke(() => MessageBox.Show(error));

                if (error.StartsWith("403")) {
                    settings.Remove("token");

                    this.LoginIfNeeded();
                }
            });
        }

        public void RawNotificationReceived(string data) {
            if (data.StartsWith("msg:")) {
                gtalk.ParseMessage(
                    data.Substring(4),
                    message => MessageReceived.Invoke(message),
                    error => App.Current.RootFrame.Dispatcher.BeginInvoke(() => MessageBox.Show(error))
                );
            }
        }

        private void GetOfflineMessages() {
            gtalk.MessageQueue(
                message => MessageReceived.Invoke(message),
                error => {
                    App.Current.RootFrame.Dispatcher.BeginInvoke(() => MessageBox.Show(error));

                    if (error.StartsWith("403")) {
                        settings.Remove("token");

                        this.LoginIfNeeded();
                    }
                },
                () => { }
            );

            App.Current.RootFrame.Dispatcher.BeginInvoke(() => {
                var tileToFind = ShellTile.ActiveTiles.First();
                var newTileData = new StandardTileData {
                    Count = 0
                };
                tileToFind.Update(newTileData);
            });
        }
    }
}
