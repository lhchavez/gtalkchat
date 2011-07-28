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
using System.Net;
using System.Windows.Documents;
using System.Text.RegularExpressions;
using System.IO;

namespace gtalkchat {
    public class GoogleTalkHelper {
        public const int MaximumChatLogSize = 500;

        #region Public Events

        public delegate void LoginCallback(string token);

        public delegate void MessageReceivedEventHandler(Message message);

        public event MessageReceivedEventHandler MessageReceived;

        public delegate void ConnectEventHandler();

        public event ConnectEventHandler Connect;

        public delegate void RosterUpdatedEventHandler();

        public event RosterUpdatedEventHandler RosterUpdated;

        #endregion

        #region Public Properties

        public bool Connected { get; private set; }

        public bool RosterLoaded { get; private set; }

        private bool offlineMessagesDownloaded;

        #endregion

        #region Private Fields

        private readonly IsolatedStorageSettings settings;
        private readonly GoogleTalk gtalk;
        private readonly PushHelper pushHelper;
        private bool hasToken;
        private bool hasUri;
        private string registeredUri;
        private static readonly Regex linkRegex = new Regex("(https?://)?(([0-9]{1-3}\\.[0-9]{1-3}\\.[0-9]{1-3}\\.[0-9]{1-3})|([a-z0-9.-]+\\.[a-z]{2,4}))(/[-a-z0-9+&@#\\/%?=~_|!:,.;]*[-a-z0-9+&@#\\/%=~_|])?", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        #endregion

        #region Public Methods

        public GoogleTalkHelper() {
            settings = App.Current.Settings;
            gtalk = App.Current.GtalkClient;
            pushHelper = App.Current.PushHelper;

            pushHelper.UriUpdated += UriUpdated;
            pushHelper.RawNotificationReceived += RawNotificationReceived;
            Connected = false;
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
                        if (error.Equals("")) {
                            App.Current.RootFrame.Dispatcher.BeginInvoke(
                                () => {
                                    MessageBox.Show(
                                        "Unable to contact server. Please retry later.");

                                    throw new QuitException();
                                }
                            );
                        } else if (error.StartsWith("401")) {
                            // stale auth token. get a new one and we should be all happy again.
                            settings.Remove("auth");

                            App.Current.RootFrame.Dispatcher.BeginInvoke(
                                () => {
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
            RosterLoaded = false;
            offlineMessagesDownloaded = false;

            settings.Clear();
            settings["chatlog"] = new Dictionary<string, List<Message>>();
            settings["unread"] = new Dictionary<string, int>();

            App.Current.Roster.Clear();

            hasToken = false;
            hasUri = false;
            registeredUri = null;

            App.Current.PushHelper.CloseChannel();
            App.Current.PushHelper.RegisterPushNotifications();

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

        public void DownloadImage(Contact contact, Action finished) {
            var fileName = "Shared/ShellContent/" + contact.Photo + ".jpg";

            using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForApplication()) {
                if (isf.FileExists(fileName)) {
                    finished();
                    return;
                }

                var fileStream = isf.CreateFile(fileName);

                var req = WebRequest.CreateHttp("https://gtalkjsonproxy.lhchavez.com/images/" + contact.Photo);

                req.BeginGetResponse(a => {
                    var response = (HttpWebResponse) req.EndGetResponse(a);

                    using (var responseStream = response.GetResponseStream()) {
                        var data = new byte[response.ContentLength];

                        responseStream.BeginRead(
                            data,
                            0,
                            (int) response.ContentLength,
                            result =>
                                fileStream.BeginWrite(
                                    data,
                                    0,
                                    data.Length,
                                    async => {
                                        finished();
                                        fileStream.Close();
                                    },
                                    null
                                )
                            ,
                            null
                        );
                    }
                }, null);
            }
        }

        public static Paragraph Linkify(string message) {
            var paragraph = new Paragraph();

            int last = 0;

            foreach (Match m in linkRegex.Matches(message)) {
                if (m.Index > last) {
                    paragraph.Inlines.Add(
                        new Run {
                            Text = message.Substring(last, m.Index)
                        }
                    );
                }

                string uri = m.Groups[0].Value;
                if (!uri.StartsWith("http://") && !uri.StartsWith("https://")) {
                    uri = uri.Insert(0, "http://");
                }

                var link = new Hyperlink {
                    NavigateUri = new Uri(uri),
                    TargetName = "_blank"
                };
                link.Inlines.Add(m.Groups[0].Value);

                paragraph.Inlines.Add(link);

                last = m.Index + m.Length;
            }

            if (last != message.Length) {
                paragraph.Inlines.Add(
                    new Run {
                        Text = message.Substring(last)
                    }
                );
            }

            return paragraph;
        }

        public static void GoogleLogin(string username, string password, LoginCallback callback) {
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

                                    callback(line.Split(new[] { '=' })[1]);
                                }
                            } catch (WebException e) {
                                var response = e.Response as HttpWebResponse;

                                try {
                                    using (var responseStream = response.GetResponseStream()) {
                                        using (var sr = new StreamReader(responseStream)) {
                                            string message = "Authentication error:\n" + sr.ReadToEnd();
                                            App.Current.RootFrame.Dispatcher.BeginInvoke(() => MessageBox.Show(message));
                                        }
                                    }
                                } catch (Exception ex) {
                                    // What is wrong with this platform?!
                                    App.Current.RootFrame.Dispatcher.BeginInvoke(
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

        public void GetOfflineMessages() {
            gtalk.MessageQueue(
                NotifyMessageReceived,
                error => {
                    if(error.Equals("")) {
                        App.Current.RootFrame.Dispatcher.BeginInvoke(() => MessageBox.Show("Unable to contact server. Please retry later."));
                    } else if (error.StartsWith("403")) {
                        GracefulReLogin();
                    } else {
                        App.Current.RootFrame.Dispatcher.BeginInvoke(() => MessageBox.Show(error));
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
                        var unread = settings["unread"] as Dictionary<string, int>;

                        App.Current.Roster.Notify = false;

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
                                if (unread.ContainsKey(contact.Email)) {
                                    contact.UnreadCount = unread[contact.Email];
                                }

                                App.Current.Roster.Add(contact);
                            }
                        }
                        App.Current.Roster.Notify = true;

                        RosterLoaded = true;
                        if (RosterUpdated != null) {
                            RosterUpdated();
                        }

                        if (!offlineMessagesDownloaded) {
                            offlineMessagesDownloaded = true;
                            GetOfflineMessages();
                        }
                    }
                ),
                error => {
                    if (error.Equals("")) {
                        App.Current.RootFrame.Dispatcher.BeginInvoke(() => MessageBox.Show("Unable to contact server. Please retry later."));
                    } else if (error.StartsWith("403")) {
                        GracefulReLogin();
                    } else {
                        App.Current.RootFrame.Dispatcher.BeginInvoke(() => MessageBox.Show(error));
                    }
                }
            );
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
                        if (error.Equals("")) {
                            App.Current.RootFrame.Dispatcher.BeginInvoke(
                                () => {
                                    MessageBox.Show(
                                        "Unable to contact server. Please retry later.");

                                    throw new QuitException();
                                }
                            );
                        } else if (error.StartsWith("403")) {
                            GracefulReLogin();
                        } else {
                            App.Current.RootFrame.Dispatcher.BeginInvoke(() => MessageBox.Show(error));
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
                    LoadRoster();

                    Connected = true;
                    if (Connect != null) {
                        Connect();
                    }
                },
                error => {
                        if (error.Equals("")) {
                            App.Current.RootFrame.Dispatcher.BeginInvoke(
                                () => {
                                    MessageBox.Show(
                                        "Unable to contact server. Please retry later.");

                                    throw new QuitException();
                                }
                            );
                        } else if (error.StartsWith("403")) {
                        GracefulReLogin();
                    } else {
                        App.Current.RootFrame.Dispatcher.BeginInvoke(() => MessageBox.Show(error));
                    }
                }
            );
        }

        private void NotifyMessageReceived(Message message) {
            if (message.Body != null) {
                List<Message> chatLog = ChatLog(message.From);

                lock (chatLog) {
                    if (chatLog.Count >= MaximumChatLogSize) {
                        chatLog.RemoveAt(0);
                    }
                    chatLog.Add(message);
                }

                var email = message.From;

                if (email.Contains("/")) {
                    email = email.Substring(0, email.IndexOf('/'));
                }

                var contact = App.Current.Roster[email];

                if(contact == null) {
                    // TODO: only for sanity-of-mind-purposes. MUST remove eventually
                    return;
                }

                if(contact.JID != message.From) {
                    App.Current.RootFrame.Dispatcher.BeginInvoke(() => contact.JID = message.From);
                }

                if (App.Current.CurrentChat == null || message.From.IndexOf(App.Current.CurrentChat) != 0) {
                    var unread = settings["unread"] as Dictionary<string, int>;

                    int unreadCount = 1;

                    lock (unread) {
                        if (!unread.ContainsKey(email)) {
                            unread.Add(email, 1);
                        } else {
                            unreadCount = ++unread[email];
                        }
                    }

                    if (contact != null) {
                        App.Current.RootFrame.Dispatcher.BeginInvoke(() => contact.UnreadCount = unreadCount);
                    }
                }
            }

            if (MessageReceived != null) {
                MessageReceived(message);
            }
        }

        private void GracefulReLogin() {
            settings.Remove("token");
            gtalk.SetToken(null);
            hasToken = false;

            LoginIfNeeded();
        }

        #endregion
    }
}