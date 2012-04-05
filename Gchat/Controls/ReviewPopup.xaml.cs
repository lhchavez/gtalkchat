using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.IO.IsolatedStorage;
using Microsoft.Phone.Controls;

namespace Gchat.Controls {
    public partial class ReviewPopup : UserControl {
        private IsolatedStorageSettings settings = IsolatedStorageSettings.ApplicationSettings;

        public ReviewPopup() {
            InitializeComponent();
            LayoutRoot.Hide();
        }

        private void Show() {
            LayoutRoot.Show();

            var f = App.Current.RootFrame.Content as PhoneApplicationPage;
            f.ApplicationBar.IsVisible = false;
        }

        private void Hide() {
            LayoutRoot.Hide();

            var f = App.Current.RootFrame.Content as PhoneApplicationPage;
            f.ApplicationBar.IsVisible = true;
        }

        private void Rate_Click(object sender, RoutedEventArgs e) {
            Hide();
            settings["ReviewPopup-Completed"] = true;
            FlurryWP7SDK.Api.LogEvent("About - Review clicked");
            var t = new Microsoft.Phone.Tasks.MarketplaceReviewTask();
            t.Show();
        }

        private void Remind_Click(object sender, RoutedEventArgs e) {
            Hide();
            settings["ReviewPopup-InstallDate"] = DateTime.Now; // reset install date
            settings["ReviewPopup-Completed"] = false;
        }

        private void No_Click(object sender, RoutedEventArgs e) {
            Hide();
            settings["ReviewPopup-Completed"] = true;
        }

        private void Loaded(object sender, RoutedEventArgs e) {
            // Already reviewed?
            bool reviewed;
            if (settings.TryGetValue("ReviewPopup-Completed", out reviewed) && reviewed) {
                return;
            }

            // Check install date
            DateTime install;
            if (!settings.TryGetValue("ReviewPopup-InstallDate", out install)) {
                // No install date saved, save today
                install = DateTime.Now;
                settings["ReviewPopup-InstallDate"] = install;
            }

            TimeSpan diff = DateTime.Now - install;
            if (diff.Days >= 3) {
                // Three days have passed, show popup
                Show();
            }
        }
    }
}
