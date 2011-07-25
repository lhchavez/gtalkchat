using System;
using System.IO.IsolatedStorage;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Net;
using System.Linq;

namespace gtalkchat {
    public partial class ChatPage : PhoneApplicationPage {
        private readonly GoogleTalk gtalk;
        private readonly GoogleTalkHelper gtalkHelper;
        private readonly IsolatedStorageSettings settings;
        private List<Message> chatLog;

        private string to;
        private string email;

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
                email = to;

                if (email.Contains("/")) {
                    email = email.Substring(0, email.IndexOf('/'));
                }

                App.Current.CurrentChat = email;

                Dispatcher.BeginInvoke(() => {

                    string displayName = email;
                    if (App.Current.Roster.Contains(to)) {
                        displayName = App.Current.Roster[to].NameOrEmail;
                    }

                    PageTitle.Text = displayName;
                    TypingStatus.Text = displayName + " is typing...";

                    if (IsPinned()) {
                        (ApplicationBar.Buttons[1] as ApplicationBarIconButton).IsEnabled = false;
                    }

                    chatLog = gtalkHelper.ChatLog(to);

                    MessageList.Children.Clear();

                    lock (chatLog) {
                        foreach (var message in chatLog) {
                            UserControl bubble;

                            if (message.Outbound) {
                                bubble = new SentChatBubble();

                                (bubble as SentChatBubble).Text = message.Body;
                                (bubble as SentChatBubble).TimeStamp = message.Time.ToString("t");
                            } else {
                                bubble = new ReceivedChatBubble();

                                (bubble as ReceivedChatBubble).Text = message.Body;
                                (bubble as ReceivedChatBubble).TimeStamp = message.Time.ToString("t");
                            }

                            MessageList.Children.Add(bubble);
                        }
                    }

                    MessageList.UpdateLayout();
                    Scroller.UpdateLayout();
                    Scroller.ScrollToVerticalOffset(Scroller.ExtentHeight);

                    var unread = settings["unread"] as Dictionary<string, int>;
                    lock (unread) {
                        unread[email] = 0;
                    }

                    var contact = App.Current.Roster[email];

                    if (contact != null) {
                        contact.UnreadCount = 0;
                    }
                });
            }

            gtalkHelper.LoginIfNeeded();
            gtalkHelper.MessageReceived += DisplayMessage;

            MessageList.UpdateLayout();
            Scroller.ScrollToVerticalOffset(Scroller.ExtentHeight);
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
            base.OnNavigatingFrom(e);

            gtalkHelper.MessageReceived -= DisplayMessage;

            App.Current.CurrentChat = null;
        }

        private void DisplayMessage(Message message) {
            Dispatcher.BeginInvoke(() => {
                if (message.From.IndexOf(email) != 0) {
                    gtalkHelper.ShowToast(message);
                } else if (message.Typing) {
                    TypingStatus.Visibility = Visibility.Visible;
                } else {
                    TypingStatus.Visibility = Visibility.Collapsed;

                    if (message.Body != null) {
                        var bubble = new ReceivedChatBubble();
                        bubble.Text = message.Body;
                        bubble.TimeStamp = message.Time.ToString("t");

                        MessageList.Children.Add(bubble);

                        MessageList.UpdateLayout();
                        Scroller.UpdateLayout();
                        Scroller.ScrollToVerticalOffset(Scroller.ExtentHeight);
                    }
                }
            });
        }

        private void SendButton_Click(object sender, EventArgs e) {
            if (MessageText.Text.Length == 0) return;

            lock (chatLog) {
                if (chatLog.Count >= 10) {
                    chatLog.RemoveAt(0);
                }
                chatLog.Add(new Message {
                    Body = MessageText.Text,
                    Outbound = true,
                    Time = DateTime.Now
                });
            }

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

        private Uri GetPinUri() {
            return new Uri("/ChatPage.xaml?from=" + HttpUtility.UrlEncode(email), UriKind.Relative);
        }

        private bool IsPinned() {
            Uri url = GetPinUri();
            ShellTile existing = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri == url);

            return existing != null;
        }

        private void PinButton_Click(object sender, EventArgs e) {
            if (!IsPinned()) {
                Contact contact = App.Current.Roster[email];
                StandardTileData tile;

                if (contact != null) {
                    tile = new StandardTileData {
                        Title = contact.NameOrEmail
                    };
                } else {
                    tile = new StandardTileData {
                        Title = email
                    };
                }

                ShellTile.Create(GetPinUri(), tile);
            }
        }
    }
}
