using System;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace gtalkchat {
    public partial class ChatPage : PhoneApplicationPage {
        private readonly GoogleTalk gtalk;
        private readonly GoogleTalkHelper gtalkHelper;
        private readonly IsolatedStorageSettings settings;

        private string to;

        public ChatPage() {
            InitializeComponent();
            gtalk = App.Current.GtalkClient;
            gtalkHelper = App.Current.GtalkHelper;
            settings = App.Current.Settings;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            if (NavigationContext.QueryString.ContainsKey("from")) {
                to = NavigationContext.QueryString["from"];
                PageTitle.Text = App.Current.Roster[to].Name;
            }

            gtalkHelper.LoginIfNeeded();
            gtalkHelper.MessageReceived += DisplayMessage;

            MessageList.UpdateLayout();
            Scroller.ScrollToVerticalOffset(Scroller.ExtentHeight);
        }

        private void DisplayMessage(Message message) {
            Dispatcher.BeginInvoke(() => {
                var bubble = new ReceivedChatBubble();
                bubble.Text = message.Body;
                bubble.TimeStamp = message.Time.ToString("t");

                MessageList.Children.Add(bubble);

                MessageList.UpdateLayout();
                Scroller.UpdateLayout();
                Scroller.ScrollToVerticalOffset(Scroller.ExtentHeight);
            });
        }

        private void SendButton_Click(object sender, EventArgs e) {
            
            gtalk.SendMessage(to, MessageText.Text, data => Dispatcher.BeginInvoke(() => {
                var bubble = new SentChatBubble();
                bubble.Text = MessageText.Text;
                bubble.TimeStamp = DateTime.Now.ToString("t");

                MessageList.Children.Add(bubble);

                MessageList.UpdateLayout();
                Scroller.UpdateLayout();
                Scroller.ScrollToVerticalOffset(Scroller.ExtentHeight);

                MessageText.Text = "";
            }), error => {
                if (error.StartsWith("403")) {
                    settings.Remove("token");
                    gtalkHelper.LoginIfNeeded();
                }

                Dispatcher.BeginInvoke(() => {
                    MessageBox.Show(error);
                });
            });
        }

        private void MessageText_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) {
            if (e.Key == System.Windows.Input.Key.Enter) {
                SendButton_Click(sender, e);
            }
        }
    }
}