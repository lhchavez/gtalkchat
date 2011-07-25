using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using Microsoft.Phone.Shell;
using Coding4Fun.Phone.Controls;
using System.Windows.Media.Imaging;

namespace gtalkchat {
    public class GoogleTalkHelper {
        #region Public Events

        public delegate void MessageReceivedEventHandler(Message message);

        public event MessageReceivedEventHandler MessageReceived;

        public delegate void ConnectEventHandler();

        public event ConnectEventHandler Connect;

        public delegate void RosterUpdatedEventHandler();

        public event RosterUpdatedEventHandler RosterUpdated;

        #endregion

        #region Public Properties

        public bool Connected { get; private set; }

        #endregion

        #region Private Fields

        private readonly IsolatedStorageSettings settings;
        private readonly GoogleTalk gtalk;
        private readonly PushHelper pushHelper;
        private bool hasToken;
        private bool hasUri;
        private string registeredUri;

        #endregion

        #region Public Methods

        public GoogleTalkHelper() {
            settings = App.Current.Settings;
            gtalk = App.Current.GtalkClient;
            pushHelper = App.Current.PushHelper;

            pushHelper.UriUpdated += UriUpdated;
            pushHelper.RawNotificationReceived += RawNotificationReceived;
            Connected = false;
            Connect += LoadRoster;
        }

        public void LoginIfNeeded() {
            if (!App.Current.Settings.Contains("auth")) {
                App.Current.RootFrame.Dispatcher.BeginInvoke(
                    () => App.Current.RootFrame.Navigate(new Uri("/LoginPage.xaml", UriKind.Relative))
                );

                return;
            }

            if (gtalk.LoggedIn) {
                return;
            }

            Connected = false;

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
                    },
                    error => {
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

        public void Logout() {
            Connected = false;

            settings.Remove("token");
            settings.Remove("auth");

            hasToken = false;
            hasUri = false;
            registeredUri = null;

            if (gtalk.LoggedIn) {
                gtalk.Logout(data => { }, error => { });
            }

            App.Current.RootFrame.Dispatcher.BeginInvoke(
                () => App.Current.RootFrame.Navigate(new Uri("/LoginPage.xaml", UriKind.Relative))
            );
        }

        public void ShowToast(Message m) {
            if (m.Body != null && m.Body != string.Empty) {
                App.Current.RootFrame.Dispatcher.BeginInvoke(() => {
                    ToastPrompt t = new ToastPrompt();
                    Contact c = App.Current.Roster[m.From];
                    t.Title = c != null ? c.NameOrEmail : m.From;
                    t.Message = m.Body;
                    t.ImageSource = new BitmapImage(new Uri("/ApplicationIcon.png", UriKind.RelativeOrAbsolute));
                    t.Show();

                    t.Completed += (s, ev) => {
                        if (ev.PopUpResult == PopUpResult.Ok) {
                            App.Current.RootFrame.Navigate(new Uri("/ChatPage.xaml?from=" + m.From, UriKind.Relative));
                        }
                    };
                });
            }
        }

        public void UriUpdated(string uri) {
            hasUri = true;

            lock (this) {
                if (hasUri && hasToken) {
                    if (!uri.Equals(registeredUri)) {
                        registeredUri = uri;

                        Register(registeredUri);
                    }
                }
            }
        }

        public void TokenUpdated() {
            hasToken = true;

            lock (this) {
                if (hasUri && hasToken) {
                    if (!pushHelper.PushChannelUri.Equals(registeredUri)) {
                        registeredUri = pushHelper.PushChannelUri;

                        Register(registeredUri);
                    }
                }
            }
        }

        public void RawNotificationReceived(string data) {
            if (data.StartsWith("msg:")) {
                gtalk.ParseMessage(
                    data.Substring(4),
                    NotifyMessageReceived,
                    error => App.Current.RootFrame.Dispatcher.BeginInvoke(() => MessageBox.Show(error))
                );
            }
        }

        public List<Message> ChatLog(string username) {
            if(username.Contains("/")) {
                username = username.Substring(0, username.IndexOf('/'));
            }

            var chatLog = settings["chatlog"] as Dictionary<string, List<Message>>;

            lock (chatLog) {
                if (!chatLog.ContainsKey(username)) {
                    chatLog.Add(username, new List<Message>());
                }

                return chatLog[username];
            }
        }

        #endregion

        #region Private Methods

        private void Register(string uri) {
            if (!settings.Contains("clientkey")) {
                gtalk.GetKey(
                    clientKey => {
                        settings["clientkey"] = ProtectedData.Protect(Encoding.UTF8.GetBytes(clientKey), null);

                        Register(uri, true);
                    },
                    error => {
                        App.Current.RootFrame.Dispatcher.BeginInvoke(() => MessageBox.Show(error));

                        if (error.StartsWith("403")) {
                            settings.Remove("token");

                            LoginIfNeeded();
                        }
                    }
                );
            } else {
                Register(uri, false);
            }
        }

        private void Register(string uri, bool keySet) {
            if (!keySet) {
                var clientKeyBytes = ProtectedData.Unprotect(settings["clientkey"] as byte[], null);
                gtalk.SetKey(Encoding.UTF8.GetString(clientKeyBytes, 0, clientKeyBytes.Length));
            }

            gtalk.Register(
                uri,
                data => {
                    GetOfflineMessages();

                    Connected = true;
                    if (Connect != null) {
                        Connect();
                    }
                },
                error => {
                    App.Current.RootFrame.Dispatcher.BeginInvoke(() => MessageBox.Show(error));

                    if (error.StartsWith("403")) {
                        settings.Remove("token");

                        LoginIfNeeded();
                    }
                }
            );
        }

        private void GetOfflineMessages() {
            gtalk.MessageQueue(
                NotifyMessageReceived,
                error => {
                    App.Current.RootFrame.Dispatcher.BeginInvoke(() => MessageBox.Show(error));

                    if (error.StartsWith("403")) {
                        settings.Remove("token");

                        LoginIfNeeded();
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

        public void LoadRoster() {
            gtalk.GetRoster(
                roster => App.Current.RootFrame.Dispatcher.BeginInvoke(
                    () => {
                        foreach (var contact in roster) {
                            if (App.Current.Roster.Contains(contact.Email)) {
                                var original =
                                    App.Current.Roster[contact.Email];

                                original.JID = contact.JID;
                                original.Name = contact.Name ??
                                                original.Name;
                                original.Online = contact.Online;
                                original.Photo = contact.Photo ??
                                                 original.Photo;
                                original.Show = contact.Show ??
                                                original.Show;
                            } else {
                                App.Current.Roster.Add(contact);
                            }
                        }

                        if (RosterUpdated != null) {
                            RosterUpdated();
                        }
                    }
                ),
                error => {
                    App.Current.RootFrame.Dispatcher.BeginInvoke(() => MessageBox.Show(error));

                    if (error.StartsWith("403")) {
                        settings.Remove("token");

                        LoginIfNeeded();
                    }
                }
            );
        }

        private void NotifyMessageReceived(Message message) {
            if (message.Body != null) {
                List<Message> chatLog = ChatLog(message.From);

                lock (chatLog) {
                    if (chatLog.Count >= 10) {
                        chatLog.RemoveAt(0);
                    }
                    chatLog.Add(message);
                }

                if (App.Current.CurrentChat == null || message.From.IndexOf(App.Current.CurrentChat) != 0) {
                    var unread = settings["unread"] as Dictionary<string, int>;

                    var email = message.From;

                    if (email.Contains("/")) {
                        email = email.Substring(0, email.IndexOf('/'));
                    }

                    int unreadCount = 1;

                    lock (unread) {
                        if (!unread.ContainsKey(email)) {
                            unread.Add(email, 1);
                        } else {
                            unreadCount = ++unread[email];
                        }
                    }

                    var contact = App.Current.Roster[email];

                    if (contact != null) {
                        App.Current.RootFrame.Dispatcher.BeginInvoke(() => contact.UnreadCount = unreadCount);
                    }
                }
            }

            if (MessageReceived != null) {
                MessageReceived(message);
            }
        }

        #endregion
    }
}