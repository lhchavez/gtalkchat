using System;
using System.IO.IsolatedStorage;
using System.Windows;
using Microsoft.Phone.Controls;

namespace gtalkchat
{
    public partial class ChatPage : PhoneApplicationPage {
        private IsolatedStorageSettings settings;
        private GoogleTalk gtalk;
        private GoogleTalkHelper gtalkHelper;

        public ChatPage() {
            InitializeComponent();
            gtalk = App.Current.GtalkClient;
            gtalkHelper = App.Current.GtalkHelper;
            settings = App.Current.Settings;

            gtalkHelper.MessageReceived += DisplayMessage;
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            if (NavigationContext.QueryString.ContainsKey("from")) {
                to.Text = NavigationContext.QueryString["from"];
            }

            gtalkHelper.LoginIfNeeded();
        }

        private void DisplayMessage(Message message) {
            Dispatcher.BeginInvoke(() => {
                chatLog.Text += String.Format("{0} on {1}: {2}\n", message.From, message.Time, message.Body);
            });
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
            }), error => {
                if (error.StartsWith("403")) {
                    settings.Remove("token");
                    gtalkHelper.LoginIfNeeded();
                }

                Dispatcher.BeginInvoke(() => {
                    MessageBox.Show(error);
                    send.IsEnabled = true;
                    to.IsEnabled = true;
                    body.IsEnabled = true;
                });
            });
        }
    }
}