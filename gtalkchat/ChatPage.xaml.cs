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
        private GoogleTalk gtalk;
        private GoogleTalkHelper gtalkHelper;
        private IsolatedStorageSettings settings;
        private List<Message> chatLog;

        private string to;
        private string email;

        public ChatPage() {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            gtalk = App.Current.GtalkClient;
            gtalkHelper = App.Current.GtalkHelper;
            settings = App.Current.Settings;

            if (NavigationContext.QueryString.ContainsKey("from")) {
                to = NavigationContext.QueryString["from"];
                email = to;

                if (email.Contains("/")) {
                    email = email.Substring(0, email.IndexOf('/'));
                }

                App.Current.CurrentChat = email;

                // TODO: make this optional
                to = email;
            }

            gtalkHelper.MessageReceived += DisplayMessage;

            if (gtalkHelper.RosterLoaded) {
                Initialize();
                gtalkHelper.GetOfflineMessages();
            } else {
                gtalkHelper.RosterUpdated += Initialize;
            }

            gtalkHelper.LoginIfNeeded();

            ScrollToBottom();
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
                    ScrollToBottom();
                } else {
                    TypingStatus.Visibility = Visibility.Collapsed;

                    to = message.From;

                    if (message.Body != null) {
                        var bubble = new ReceivedChatBubble();
                        bubble.Text = message.Body;
                        bubble.TimeStamp = message.Time.ToString("t");

                        MessageList.Children.Add(bubble);

                        ScrollToBottom();
                    }
                }
            });
        }

        private void SendButton_Click(object sender, EventArgs e) {
            if (MessageText.Text.Length == 0) return;

            (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = false;

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

                MessageText.Text = "";
                (ApplicationBar.Buttons[0] as ApplicationBarIconButton).IsEnabled = true;

                MessageList.UpdateLayout();
                Scroller.UpdateLayout();
                Scroller.ScrollToVerticalOffset(Scroller.ExtentHeight);

                
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
            ScrollToBottom();
            if (e.Key == System.Windows.Input.Key.Enter) {
                SendButton_Click(sender, e);
            }
        }

        private void Initialize() {
            gtalkHelper.RosterUpdated -= Initialize;

            Dispatcher.BeginInvoke(() => {
                string displayName = email;
                if (App.Current.Roster.Contains(to)) {
                    displayName = App.Current.Roster[to].NameOrEmail;
                }

                PageTitle.Text = displayName.ToUpper();
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

                Uri url = GetPinUri();
                ShellTile existing = ShellTile.ActiveTiles.FirstOrDefault(x => x.NavigationUri == url);

                if (existing != null) {
                    existing.Update(
                        new StandardTileData {
                            Count = 0
                        }
                    );
                }

                var contact = App.Current.Roster[email];

                if (contact != null) {
                    contact.UnreadCount = 0;
                }
            });
        }

        private void ScrollToBottom() {
            MessageList.UpdateLayout();
            Scroller.UpdateLayout();
            Scroller.ScrollToVerticalOffset(Scroller.ExtentHeight);
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

                    if (contact.Photo != null) {
                        gtalkHelper.DownloadImage(contact, () => {
                            tile.BackgroundImage =
                                new Uri("isostore:/Shared/ShellContent/" + contact.Photo + ".jpg");

                            ShellTile.Create(GetPinUri(), tile);
                        });
                    } else {
                        ShellTile.Create(GetPinUri(), tile);
                    }
                } else {
                    tile = new StandardTileData {
                        Title = email
                    };

                    ShellTile.Create(GetPinUri(), tile);
                }
            }
        }
    }
}
