using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO.IsolatedStorage;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Notification;

namespace gtalkchat {
    public partial class Chat : PhoneApplicationPage {
        private IsolatedStorageSettings settings;
        private GoogleTalk gtalk;

        /// Holds the push channel that is created or found.
        HttpNotificationChannel pushChannel;

        public Chat() {
            // The name of our push channel.
            string channelName = "GtalkChatChannel";

            InitializeComponent();

            // Try to find the push channel.
            pushChannel = HttpNotificationChannel.Find(channelName);

            // If the channel was not found, then create a new connection to the push service.
            if (pushChannel == null) {
                pushChannel = new HttpNotificationChannel(channelName);

                // Register for all the events before attempting to open the channel.
                pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);
                pushChannel.HttpNotificationReceived += new EventHandler<HttpNotificationEventArgs>(PushChannel_HttpNotificationReceived);

                pushChannel.Open();
                pushChannel.BindToShellToast();
            } else {
                // The channel was already open, so just register for all the events.
                pushChannel.ChannelUriUpdated += new EventHandler<NotificationChannelUriEventArgs>(PushChannel_ChannelUriUpdated);
                pushChannel.ErrorOccurred += new EventHandler<NotificationChannelErrorEventArgs>(PushChannel_ErrorOccurred);
                pushChannel.HttpNotificationReceived += new EventHandler<HttpNotificationEventArgs>(PushChannel_HttpNotificationReceived);
            }

            settings = IsolatedStorageSettings.ApplicationSettings;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            if (NavigationContext.QueryString.ContainsKey("from")) {
                to.Text = NavigationContext.QueryString["from"];
            }

            if (settings.Contains("token")) {
                var tokenBytes = ProtectedData.Unprotect(settings["token"] as byte[], null);
                gtalk = new GoogleTalk(Encoding.UTF8.GetString(tokenBytes, 0, tokenBytes.Length));

                if (pushChannel.ChannelUri != null) {
                    Register(pushChannel.ChannelUri.ToString());
                }
            } else {
                var authBytes = ProtectedData.Unprotect(settings["auth"] as byte[], null);
                gtalk = new GoogleTalk(settings["username"] as string, Encoding.UTF8.GetString(authBytes, 0, authBytes.Length), token => {
                    settings["token"] = ProtectedData.Protect(Encoding.UTF8.GetBytes(token), null);
                    settings.Save();

                    if (pushChannel.ChannelUri != null) {
                        Register(pushChannel.ChannelUri.ToString());
                    }
                }, error => {
                    Dispatcher.BeginInvoke(() => {
                        if (error.StartsWith("401")) {
                            // stale auth token. get a new one and we should be all happy again.
                            settings.Remove("auth");
                            settings.Save();
                        }

                        MessageBox.Show(error);
                        NavigationService.GoBack();
                    });
                });
            }
        }

        private void Register(string uri) {
            if (!settings.Contains("clientkey")) {
                gtalk.GetKey(clientKey => {
                    Dispatcher.BeginInvoke(() => {
                        settings["clientkey"] = ProtectedData.Protect(Encoding.UTF8.GetBytes(clientKey), null);
                        settings.Save();

                        this.Register(uri, true);
                    });
                }, error => {
                    MessageBox.Show(error);
                    NavigationService.GoBack();
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

            gtalk.Register(uri, data => {
                this.GetOfflineMessages();
            }, error => {
                Dispatcher.BeginInvoke(() => {
                    if (error.StartsWith("404")) {
                        settings.Remove("token");
                        settings.Save();
                    }

                    MessageBox.Show(error);
                    NavigationService.GoBack();
                });
            });
        }

        private void DisplayMessage(Message message) {
            Dispatcher.BeginInvoke(() => {
                chatLog.Text += String.Format("{0} on {1}: {2}\n", message.From, message.Time, message.Body);
            });
        }

        private void DisplayContact(Contact contact) {
            Dispatcher.BeginInvoke(() => {
                chatLog.Text += String.Format("{0} on {1}: {2}\n", contact.JID, contact.Online, contact.Show);
            });
        }

        private void GetOfflineMessages() {
            gtalk.MessageQueue(DisplayMessage, error => Dispatcher.BeginInvoke(() => MessageBox.Show(error)), () => { });
        }

        private void send_Click(object sender, RoutedEventArgs e) {
            send.IsEnabled = false;
            to.IsEnabled = false;
            body.IsEnabled = false;

            gtalk.SendMessage(to.Text, body.Text, data => Dispatcher.BeginInvoke(() => {
                body.Text = "";
                send.IsEnabled = true;
                to.IsEnabled = true;
                body.IsEnabled = true;
            }), error => Dispatcher.BeginInvoke(() => {
                if (error.StartsWith("404")) {
                    settings.Remove("token");
                    settings.Save();
                }

                MessageBox.Show(error);
                send.IsEnabled = true;
                to.IsEnabled = true;
                body.IsEnabled = true;
            }));
        }

        void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e) {
            Register(e.ChannelUri.ToString());
        }

        void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e) {
            // Error handling logic for your particular application would be here.
            Dispatcher.BeginInvoke(() =>
                MessageBox.Show(String.Format("A push notification {0} error occurred.  {1} ({2}) {3}",
                    e.ErrorType, e.Message, e.ErrorCode, e.ErrorAdditionalData))
            );
        }

        void PushChannel_HttpNotificationReceived(object sender, HttpNotificationEventArgs e) {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(e.Notification.Body)) {
                string line;

                while ((line = reader.ReadLine()) != null) {
                    if (line.StartsWith("msg:")) {
                        gtalk.ParseMessage(line.Substring(4), DisplayMessage, error => Dispatcher.BeginInvoke(() => MessageBox.Show(error)));
                    } else if (line.StartsWith("pre:")) {
                        gtalk.ParseContact(line.Substring(4), true, DisplayContact, error => Dispatcher.BeginInvoke(() => MessageBox.Show(error)));
                    }
                }
            }
        }
    }
}