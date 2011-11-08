using System;
using System.Collections.Generic;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Gchat.Controls;
using Gchat.Data;
using Gchat.Protocol;
using Gchat.Utilities;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System.Windows.Media.Imaging;

namespace Gchat.Pages {
    public partial class Chat : PhoneApplicationPage {
        private GoogleTalk gtalk;
        private GoogleTalkHelper gtalkHelper;
        private IsolatedStorageSettings settings;
        private List<Message> chatLog = new List<Message>();
        private PhotoChooserTask photoChooserTask = new PhotoChooserTask {
            ShowCamera = true
        };

        private string to;
        private string email;
        private Contact currentContact = null;

        private bool otr;
        ApplicationBarIconButton sendButton, otrButton, attachButton, pinButton;

        public Chat() {
            InitializeComponent();
            BuildAppbar();

            photoChooserTask.Completed += (s, r) => {
                if (r.TaskResult == TaskResult.OK) {
                    FlurryWP7SDK.Api.LogEvent("Chat - Image attched");

                    attachButton.IsEnabled = false;
                    BitmapImage bm = new BitmapImage();
                    bm.SetSource(r.ChosenPhoto);
                    ShowProgressBar(AppResources.Chat_ProgressUploadingPhoto);
                    Imgur.Upload(bm, (i, error) => {
                        if (i != null) {
                            Dispatcher.BeginInvoke(() => {
                                MessageText.Text += " " + i.Original.ToString() + " ";
                                ScrollToBottom();
                                HideProgressBar();
                                attachButton.IsEnabled = true;
                            });
                        } else {
                            Dispatcher.BeginInvoke(() => {
                                gtalkHelper.ShowToast(error);
                                HideProgressBar();
                                attachButton.IsEnabled = true;
                            });
                        }
                    });
                }
            };
        }

        public void BuildAppbar() {
            ApplicationBar = new ApplicationBar();

            sendButton = new ApplicationBarIconButton();
            sendButton.IconUri = new Uri("/icons/appbar.send.text.rest.png", UriKind.Relative);
            sendButton.Text = AppResources.Chat_AppbarSend;
            sendButton.Click += SendButton_Click;
            ApplicationBar.Buttons.Add(sendButton);

            attachButton = new ApplicationBarIconButton();
            attachButton.IconUri = new Uri("/icons/appbar.attach.rest.png", UriKind.Relative);
            attachButton.Text = AppResources.Chat_AppbarAttach;
            attachButton.Click += AttachButton_Click;
            ApplicationBar.Buttons.Add(attachButton);

            otrButton = new ApplicationBarIconButton();
            otrButton.IconUri = new Uri("/icons/appbar.lock.rest.png", UriKind.Relative);
            otrButton.Text = AppResources.Chat_AppbarGoOtr;
            otrButton.Click += OTRButton_Click;
            ApplicationBar.Buttons.Add(otrButton);

            pinButton = new ApplicationBarIconButton();
            pinButton.IconUri = new Uri("/icons/appbar.pin.rest.png", UriKind.Relative);
            pinButton.Text = AppResources.Chat_AppbarPin;
            pinButton.Click += PinButton_Click;
            ApplicationBar.Buttons.Add(pinButton);

            var delete = new ApplicationBarMenuItem();
            delete.Text = AppResources.Chat_AppbarDeleteThread;
            delete.Click += DeleteThread_Click;
            ApplicationBar.MenuItems.Add(delete);

            var list = new ApplicationBarMenuItem();
            list.Text = AppResources.Chat_AppbarViewContacts;
            list.Click += ViewContactList_Click;
            ApplicationBar.MenuItems.Add(list);
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            FlurryWP7SDK.Api.LogEvent("Chat - Chat started", true);

            App.Current.LastPage = e.Uri.OriginalString;

            gtalk = App.Current.GtalkClient;
            gtalkHelper = App.Current.GtalkHelper;
            settings = App.Current.Settings;

            App.Current.GtalkHelper.SetCorrectOrientation(this);

            currentContact = null;

            if (NavigationContext.QueryString.ContainsKey("from")) {
                to = NavigationContext.QueryString["from"];
                email = to;

                if (email.Contains("/")) {
                    email = email.Substring(0, email.IndexOf('/'));
                }

                App.Current.CurrentChat = email;

                to = email;

                if (App.Current.Roster.Contains(email)) {
                    currentContact = App.Current.Roster[email];
                }
            }

            gtalkHelper.MessageReceived += DisplayMessage;



            if (gtalkHelper.RosterLoaded && gtalk.LoggedIn && App.Current.Roster.Contains(email)) {
                Initialize();
            } else if (gtalkHelper.RosterLoaded && gtalk.LoggedIn) {
                if (e.IsNavigationInitiator) {
                    ShowProgressBar(AppResources.Chat_ProgressGettingMessages);
                    gtalkHelper.GetOfflineMessages(() => Dispatcher.BeginInvoke(() => HideProgressBar()));
                }
            } else {
                ShowProgressBar(AppResources.Chat_ProgressGettingMessages);
                gtalkHelper.RosterUpdated += () => HideProgressBar();
                gtalkHelper.RosterUpdated += Initialize;
            }
            
            object savedText;
            if (State.TryGetValue("message", out savedText)) {
                MessageText.Text = (string) savedText;
            }
            
            gtalkHelper.LoginIfNeeded();

            ScrollToBottom();
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e) {
            base.OnNavigatingFrom(e);

            FlurryWP7SDK.Api.EndTimedEvent("Chat - Chat started");

            gtalkHelper.MessageReceived -= DisplayMessage;

            App.Current.CurrentChat = null;

            State["message"] = MessageText.Text;
        }

        private void DisplayMessage(Message message) {
            FlurryWP7SDK.Api.LogEvent("Chat - Chat recieved");
            Dispatcher.BeginInvoke(() => {
                if (message.From.IndexOf(email) != 0) {
                    gtalkHelper.ShowToast(message);
                } else if (message.Typing) {
                    TypingStatus.Visibility = Visibility.Visible;
                    ScrollToBottom();
                } else {
                    if (message.OTR != otr) {
                        if (message.OTR) {
                            ShowStartOtr();
                        } else {
                            ShowEndOtr();
                        }

                        otr = message.OTR;

                        lock (chatLog) {
                            while (chatLog.Count >= GoogleTalkHelper.MaximumChatLogSize) {
                                chatLog.RemoveAt(0);
                            }
                            chatLog.Add(new Message {
                                OTR = otr,
                                Outbound = false,
                                Time = message.Time
                            });
                        }
                    }

                    TypingStatus.Visibility = Visibility.Collapsed;

                    to = message.From;

                    if (message.Body != null) {
                        var bubble = new ReceivedChatBubble();
                        bubble.Text = message.Body;
                        bubble.TimeStamp = message.Time;

                        MessageList.Children.Add(bubble);

                        ScrollToBottom();
                    }
                }
            });
        }

        private void LogChatEvent(string description) {
            TextBlock t = new TextBlock {
                Text = description
            };
            t.Margin = new Thickness(0, 0, 0, 6);
            t.Foreground = (Brush)App.Current.Resources["PhoneSubtleBrush"];
            MessageList.Children.Add(t);
            ScrollToBottom();
        }

        private void ShowStartOtr() {
            FlurryWP7SDK.Api.LogEvent("Chat - OTR enabled");

            otrButton.IconUri = new Uri("/icons/appbar.unlock.rest.png", UriKind.Relative);
            otrButton.Text = AppResources.Chat_AppbarEndOtr;
            LogChatEvent(AppResources.Chat_NoticeStartOtr);
        }

        private void ShowEndOtr() {
            FlurryWP7SDK.Api.LogEvent("Chat - OTR disabled");

            otrButton.IconUri = new Uri("/icons/appbar.lock.rest.png", UriKind.Relative);
            otrButton.Text = AppResources.Chat_AppbarGoOtr;
            LogChatEvent(AppResources.Chat_NoticeEndOtr);
        }

        private void SendButton_Click(object sender, EventArgs e) {
            FlurryWP7SDK.Api.LogEvent("Chat - Chat sent");

            if (MessageText.Text.Length == 0) return;

            ShowProgressBar(AppResources.Chat_ProgressSendingMessage);

            sendButton.IsEnabled = false;

            try {
                gtalk.SendMessage(to, MessageText.Text, data => Dispatcher.BeginInvoke(() => {
                    HideProgressBar();

                    var bubble = new SentChatBubble();
                    bubble.Text = MessageText.Text;
                    bubble.TimeStamp = DateTime.Now;

                    App.Current.GtalkHelper.AddRecentContact(currentContact);

                    MessageList.Children.Add(bubble);

                    sendButton.IsEnabled = true;

                    MessageList.UpdateLayout();
                    Scroller.UpdateLayout();
                    Scroller.ScrollToVerticalOffset(Scroller.ExtentHeight);

                    lock (chatLog) {
                        while (chatLog.Count >= GoogleTalkHelper.MaximumChatLogSize) {
                            chatLog.RemoveAt(0);
                        }
                        chatLog.Add(new Message {
                            Body = MessageText.Text,
                            Outbound = true,
                            Time = DateTime.Now,
                            OTR = otr
                        });
                    }

                    MessageText.Text = "";
                }), error => {
                    HideProgressBar();
                    if (error.StartsWith("403")) {
                        settings.Remove("token");
                        settings.Remove("rootUrl");
                        gtalkHelper.LoginIfNeeded();
                    } else {
                        Dispatcher.BeginInvoke(
                            () => {
                                sendButton.IsEnabled = true;
                                gtalkHelper.ShowToast(AppResources.Chat_ErrorMessageNotSent);
                            }
                        );
                    }
                });
            } catch (InvalidOperationException) {
                Dispatcher.BeginInvoke(
                    () => {
                        HideProgressBar();
                        MessageBox.Show(
                            AppResources.Chat_ErrorAuthExpiredBody,
                            AppResources.Chat_ErrorAuthExpiredTitle,
                            MessageBoxButton.OK
                        );
                        App.Current.RootFrame.Navigate(new Uri("/Pages/Login.xaml", UriKind.Relative));
                    }
                );
            }
        }

        private void Initialize() {
            gtalkHelper.RosterUpdated -= Initialize;

            Dispatcher.BeginInvoke(() => {
                string displayName = email;
                string status = string.Empty;
                if (App.Current.Roster.Contains(to)) {
                    Contact t = App.Current.Roster[to];
                    displayName = t.NameOrEmail;
                    status = t.Status;
                }

                PageTitle.Text = displayName.ToUpper();
                if (status != string.Empty) {
                    PageTitle.Text += ", " + char.ToUpper(status[0]) + status.Substring(1);
                }

                TypingStatus.Text = String.Format(AppResources.Chat_NoticeTyping, displayName);

                if (gtalkHelper.IsContactPinned(email)) {
                    pinButton.IsEnabled = false;
                }

                chatLog = gtalkHelper.ChatLog(to);

                MessageList.Visibility = System.Windows.Visibility.Collapsed;

                MessageList.Children.Clear();

                lock (chatLog) {
                    var otr = false;

                    foreach (var message in chatLog) {
                        UserControl bubble;

                        if (message.OTR != otr) {
                            if (message.OTR) {
                                ShowStartOtr();
                            } else {
                                ShowEndOtr();
                            }

                            otr = message.OTR;
                        }

                        if (message.Body == null) continue;

                        if (message.Outbound) {
                            bubble = new SentChatBubble();

                            (bubble as SentChatBubble).Text = message.Body;
                            (bubble as SentChatBubble).TimeStamp = message.Time;
                        } else {
                            bubble = new ReceivedChatBubble();

                            (bubble as ReceivedChatBubble).Text = message.Body;
                            (bubble as ReceivedChatBubble).TimeStamp = message.Time;
                        }

                        MessageList.Children.Add(bubble);
                    }
                }

                MessageList.Visibility = System.Windows.Visibility.Visible;
                MessageList.UpdateLayout();
                Scroller.UpdateLayout();
                Scroller.ScrollToVerticalOffset(Scroller.ExtentHeight);

                var unread = settings["unread"] as Dictionary<string, int>;
                lock (unread) {
                    unread[email] = 0;
                }
                if (App.Current.Roster.Contains(email)) {
                    App.Current.Roster[email].UnreadCount = 0;
                }

                Uri url = gtalkHelper.GetPinUri(email);
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

                // Sets to broadcast the first message in a conversation
                to = email;
            });
        }

        private void ScrollToBottom() {
            MessageList.UpdateLayout();
            Scroller.UpdateLayout();
            Scroller.ScrollToVerticalOffset(Scroller.ExtentHeight);
        }

        private void PinButton_Click(object sender, EventArgs e) {
            gtalkHelper.PinContact(email);
        }

        private void OTRButton_Click(object sender, EventArgs e) {
            FlurryWP7SDK.Api.LogEvent("Chat - OTRButton clicked");

            if (otr) {
                gtalk.OTR(email, false, s => Dispatcher.BeginInvoke(ShowEndOtr), s => { });
                otr = false;
            } else {
                gtalk.OTR(email, true, s => Dispatcher.BeginInvoke(ShowStartOtr), s => { });
                otr = true;
            }

            lock (chatLog) {
                while (chatLog.Count >= GoogleTalkHelper.MaximumChatLogSize) {
                    chatLog.RemoveAt(0);
                }
                chatLog.Add(new Message {
                    OTR = otr,
                    Outbound = true,
                    Time = DateTime.Now
                });
            }
        }

        private void DeleteThread_Click(object sender, EventArgs e)
        {
            FlurryWP7SDK.Api.LogEvent("Chat - DeleteThread clicked");

            MessageBoxResult delete = MessageBox.Show(
                AppResources.Chat_WarningDeleteThreadBody,
                AppResources.Chat_WarningDeleteThreadTitle,
                MessageBoxButton.OKCancel
            );

            if (delete == MessageBoxResult.OK)
            {
                MessageList.Children.Clear();

                lock (chatLog)
                {
                    chatLog.Clear();
                }
            }
        }

        private void ViewContactList_Click(object sender, EventArgs e) {
            FlurryWP7SDK.Api.LogEvent("Chat - ViewContactList clicked");

            Dispatcher.BeginInvoke(() => App.Current.RootFrame.Navigate(new Uri("/Pages/ContactList.xaml", UriKind.Relative)));
        }

        private void MessageText_KeyUp(object sender, System.Windows.Input.KeyEventArgs e) {
            ScrollToBottom();
        }

        private void ShowProgressBar(string text) {
            SystemTray.SetProgressIndicator(this, new ProgressIndicator {
                IsIndeterminate = true,
                IsVisible = true,
                Text = text
            });
        }

        private void HideProgressBar() {
            SystemTray.SetProgressIndicator(this, new ProgressIndicator {
                IsVisible = false,
            });
        }

        private void AttachButton_Click(object sender, EventArgs e) {
            FlurryWP7SDK.Api.LogEvent("Chat - AttachButton clicked");
            bool warned = false;
            IsolatedStorageSettings.ApplicationSettings.TryGetValue("imgurwarned", out warned);

            if (!warned) {
                MessageBoxResult r = MessageBox.Show(AppResources.Chat_WarningImagesBody, AppResources.Chat_WarningImagesTitle, MessageBoxButton.OKCancel);
                if (r == MessageBoxResult.OK) {
                    IsolatedStorageSettings.ApplicationSettings["imgurwarned"] = true;
                    warned = true;
                }
            }

            if (warned) {
                photoChooserTask.Show();
            }
        }
    }
}
