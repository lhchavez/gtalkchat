using System.Windows;
using Microsoft.Phone.Controls;
using System.Collections.Generic;
using FlurryWP7SDK.Models;

namespace Gchat.Pages {
    public partial class Settings : PhoneApplicationPage {
        private bool fireEvents;

        public Settings() {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            FlurryWP7SDK.Api.LogEvent("Settings - Settings started", true);

            App.Current.GtalkHelper.SetCorrectOrientation(this);

            App.Current.LastPage = e.Uri.OriginalString;

            fireEvents = false;

            RagesCheckbox.IsChecked = App.Current.Settings.Contains("rages") && (bool)App.Current.Settings["rages"];
            LandscapeCheckbox.IsChecked = App.Current.Settings.Contains("rotate") && (bool)App.Current.Settings["rotate"];
            ToastCheckbox.IsChecked = !App.Current.Settings.Contains("toastNotification") || (bool)App.Current.Settings["toastNotification"];
            TileCheckbox.IsChecked = !App.Current.Settings.Contains("tileNotification") || (bool)App.Current.Settings["tileNotification"];
            SecondaryTileCheckbox.IsChecked = !App.Current.Settings.Contains("secondaryTileNotification") || (bool)App.Current.Settings["secondaryTileNotification"];

            fireEvents = true;

            SetLicenseNotice();
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e) {
            base.OnNavigatingFrom(e);
            FlurryWP7SDK.Api.EndTimedEvent("Settings - Settings started");
        }

        private void RagesCheckbox_Checked(object sender, RoutedEventArgs e) {
            if (!fireEvents) return;

            FlurryWP7SDK.Api.LogEvent("Settings - Rages toggled", new List<Parameter>() { new Parameter("enabled", RagesCheckbox.IsChecked.ToString()) });
            App.Current.Settings["rages"] = RagesCheckbox.IsChecked;
        }

        private void LandscapeCheckbox_Checked(object sender, RoutedEventArgs e) {
            if (!fireEvents) return;

            FlurryWP7SDK.Api.LogEvent("Settings - Rotate toggled", new List<Parameter>() { new Parameter("enabled", LandscapeCheckbox.IsChecked.ToString()) });
            App.Current.Settings["rotate"] = LandscapeCheckbox.IsChecked;
        }

        private void Notification_Checked(object sender, RoutedEventArgs e) {
            if (!fireEvents) return;

            if (App.Current.GtalkClient.LoggedIn) {
                FlurryWP7SDK.Api.LogEvent("Settings - Notifications toggled", new List<Parameter>() { 
                    new Parameter("toast", ToastCheckbox.IsChecked.ToString()),
                    new Parameter("tile", TileCheckbox.IsChecked.ToString()),
                    new Parameter("secondary", SecondaryTileCheckbox.IsChecked.ToString()) 
                });

                App.Current.GtalkClient.Notifications(
                    ToastCheckbox.IsChecked.GetValueOrDefault(true),
                    TileCheckbox.IsChecked.GetValueOrDefault(true),
                    SecondaryTileCheckbox.IsChecked.GetValueOrDefault(true),
                    data => Dispatcher.BeginInvoke(() => {
                        App.Current.Settings["toastNotification"] = ToastCheckbox.IsChecked;
                        App.Current.Settings["tileNotification"] = TileCheckbox.IsChecked;
                        App.Current.Settings["secondaryTileNotification"] = SecondaryTileCheckbox.IsChecked;
                    }), error => {
                    }
                );
            }
        }

        private void Review_Click(object sender, RoutedEventArgs e) {
            FlurryWP7SDK.Api.LogEvent("About - Review clicked");
            
            var t = new Microsoft.Phone.Tasks.MarketplaceReviewTask();
            t.Show();
        }

        private void Buy_Click(object sender, RoutedEventArgs e) {
            FlurryWP7SDK.Api.LogEvent("About - Buy clicked");

            var t = new Microsoft.Phone.Tasks.MarketplaceDetailTask();
            t.ContentIdentifier = "1f377d53-5fbf-4549-928f-2d246891a735";
            t.Show();
        }

        private void SetLicenseNotice() {
            if (App.Current.GtalkHelper.IsPaid()) {
                PaidVersionNotice.FontSize = (double)App.Current.Resources["PhoneFontSizeMedium"];
                FreeVersionNotice.FontSize = 0.1;
            } else {
                FreeVersionNotice.FontSize = (double)App.Current.Resources["PhoneFontSizeMedium"];
                PaidVersionNotice.FontSize = 0.1;
            }
        }
    }
}