using System;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;

namespace gtalkchat {
    public partial class ChatPage : PhoneApplicationPage {
        private readonly GoogleTalk gtalk;
        private readonly GoogleTalkHelper gtalkHelper;
        private readonly IsolatedStorageSettings settings;

        public ChatPage() {
            InitializeComponent();
            gtalk = App.Current.GtalkClient;
            gtalkHelper = App.Current.GtalkHelper;
            settings = App.Current.Settings;

            gtalkHelper.MessageReceived += DisplayMessage;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            if (NavigationContext.QueryString.ContainsKey("from")) {
                //to.Text = NavigationContext.QueryString["from"];
            }

            gtalkHelper.LoginIfNeeded();
        }

        private void DisplayMessage(Message message) {
            Dispatcher.BeginInvoke(() => {
                var bubble = new ReceivedChatBubble();
                bubble.Text = message.Body;
                bubble.TimeStamp = message.Time.ToString("t");

                MessageList.Children.Add(bubble);
            });
        }

        // ReSharper disable InconsistentNaming
        private void send_Click(object sender, RoutedEventArgs e) {
            // ReSharper restore InconsistentNaming
            //send.IsEnabled = false;
            //to.IsEnabled = false;
            //body.IsEnabled = false;

            gtalk.SendMessage(/*to.Text, body.Text*/ null, null, data => Dispatcher.BeginInvoke(() => {
                //body.Text = "";
                //send.IsEnabled = true;
                //to.IsEnabled = true;
                //body.IsEnabled = true;
            }), error => {
                if (error.StartsWith("403")) {
                    settings.Remove("token");
                    gtalkHelper.LoginIfNeeded();
                }

                Dispatcher.BeginInvoke(() => {
                    MessageBox.Show(error);
                    //send.IsEnabled = true;
                    //to.IsEnabled = true;
                    //body.IsEnabled = true;
                });
            });
        }
    }
}