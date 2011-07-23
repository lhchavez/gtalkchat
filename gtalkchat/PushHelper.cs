using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Notification;

namespace gtalkchat
{
    public class PushHelper
    {
        #region Public Events

        public delegate void UriUpdatedEventHandler(string uri);
        public event UriUpdatedEventHandler UriUpdated;

        public delegate void ErrorEventHandler(NotificationChannelErrorEventArgs e);
        public event ErrorEventHandler Error;

        public delegate void RawNotificationReceivedEventHandler(string data);
        public event RawNotificationReceivedEventHandler RawNotificationReceived;

        #endregion

        #region Public Methods

        public void RegisterPushNotifications()
        {
            // If the channel was not found, then create a new connection to the push service.
            if (pushChannel == null)
            {
                pushChannel = new HttpNotificationChannel(channelName);

                // Register for all the events before attempting to open the channel.
                pushChannel.ChannelUriUpdated += PushChannel_ChannelUriUpdated;
                pushChannel.ErrorOccurred += PushChannel_ErrorOccurred;
                pushChannel.HttpNotificationReceived += PushChannel_HttpNotificationReceived;

                pushChannel.Open();
                pushChannel.BindToShellToast();
            }
            else
            {
                // The channel was already open, so just register for all the events.
                pushChannel.ChannelUriUpdated += PushChannel_ChannelUriUpdated;
                pushChannel.ErrorOccurred += PushChannel_ErrorOccurred;
                pushChannel.HttpNotificationReceived += PushChannel_HttpNotificationReceived;
            }

            if (!pushChannel.IsShellTileBound)
            {
                pushChannel.BindToShellTile();
            }
        }

        #endregion

        #region Private Fields

        /// Holds the push channel that is created or found.
        private static HttpNotificationChannel pushChannel;

        private static string channelName = "GtalkChatChannel";

        #endregion

        #region Private Methods

        private void PushChannel_ChannelUriUpdated(object sender, NotificationChannelUriEventArgs e)
        {
            if (UriUpdated != null)
            {
                UriUpdated(e.ChannelUri.ToString());
            }
        }

        private void PushChannel_ErrorOccurred(object sender, NotificationChannelErrorEventArgs e)
        {
            // Error handling logic for your particular application would be here.
            if (Error != null)
            {
                Error(e);
            }
        }

        private void PushChannel_HttpNotificationReceived(object sender, HttpNotificationEventArgs e)
        {
            using (System.IO.StreamReader reader = new System.IO.StreamReader(e.Notification.Body))
            {
                string data = reader.ReadToEnd();

                if (RawNotificationReceived != null)
                {
                    RawNotificationReceived(data);
                }
            }
        }

        #endregion
    }
}
