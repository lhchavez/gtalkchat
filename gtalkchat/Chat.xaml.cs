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

        public Chat() {
            /// Holds the push channel that is created or found.
            HttpNotificationChannel pushChannel;

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

            if (settings.Contains("token")) {
                var tokenBytes = ProtectedData.Unprotect(settings["token"] as byte[], null);
                gtalk = new GoogleTalk(Encoding.UTF8.GetString(tokenBytes, 0, tokenBytes.Length));

                if (pushChannel.ChannelUri != null) {
                    gtalk.Register(pushChannel.ChannelUri.ToString(), data => { }, error => {
                        Dispatcher.BeginInvoke(() => {
                            settings.Remove("token");
                            settings.Save();

                            MessageBox.Show(error);
                            NavigationService.GoBack();
                        });
                    });
                }
            } else {
                var authBytes = ProtectedData.Unprotect(settings["auth"] as byte[], null);
                gtalk = new GoogleTalk(settings["username"] as string, Encoding.UTF8.GetString(authBytes, 0, authBytes.Length), token => {
                    settings["token"] = ProtectedData.Protect(Encoding.UTF8.GetBytes(token), null);
                    settings.Save();

                    if (pushChannel.ChannelUri != null) {
                        gtalk.Register(pushChannel.ChannelUri.ToString(), data => { }, error => Dispatcher.BeginInvoke(() => MessageBox.Show(error)));
                    }
                }, error => {
                    Dispatcher.BeginInvoke(() => {
                        MessageBox.Show(error);
                        NavigationService.GoBack();
                    });
                });
            }
        }

        private void send_Click(object sender, RoutedEventArgs e) {
            gtalk.Message(to.Text, body.Text, data => Dispatcher.BeginInvoke(() => body.Text = "" ), error => Dispatcher.BeginInvoke(() => {
                MessageBox.Show(error);
                settings.Remove("token");
                settings.Save();
            }));
        }

        void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e) {
            gtalk.Register(e.ChannelUri.ToString(), data => { }, error => Dispatcher.BeginInvoke(() => {
                MessageBox.Show(error);
                settings.Remove("token");
                settings.Save();

                NavigationService.GoBack();
            }));
        }

        void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e) {
            // Error handling logic for your particular application would be here.
            Dispatcher.BeginInvoke(() =>
                MessageBox.Show(String.Format("A push notification {0} error occurred.  {1} ({2}) {3}",
                    e.ErrorType, e.Message, e.ErrorCode, e.ErrorAdditionalData))
            );
        }

        void PushChannel_HttpNotificationReceived(object sender, HttpNotificationEventArgs e) {
            string message;

            using (System.IO.StreamReader reader = new System.IO.StreamReader(e.Notification.Body)) {
                message = reader.ReadToEnd();
            }

            Dispatcher.BeginInvoke(() => chatLog.Text += message + "\n");
        }
    }
}