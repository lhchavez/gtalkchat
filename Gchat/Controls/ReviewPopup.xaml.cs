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
        private IsolatedStorageSettings settings;

        public ReviewPopup() {
            InitializeComponent();
            LayoutRoot.Hide();

            settings = App.Current.Settings;
        }

        private void PageLoaded(object sender, RoutedEventArgs e) {
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

        public void Show() {
            LayoutRoot.Show();

            var f = App.Current.RootFrame.Content as PhoneApplicationPage;
            f.ApplicationBar.IsVisible = false;
        }

        public void Hide() {
            LayoutRoot.Hide();
            
            var f = App.Current.RootFrame.Content as PhoneApplicationPage;
            f.ApplicationBar.IsVisible = true;
        }

        public bool IsShown() {
            return LayoutRoot.Visibility == System.Windows.Visibility.Visible;
        }

        private void Rate_Click(object sender, RoutedEventArgs e) {
            Hide();
            settings["ReviewPopup-Completed"] = true;
            FlurryWP7SDK.Api.LogEvent("ReviewPopup", new List<FlurryWP7SDK.Models.Parameter>() {
                new FlurryWP7SDK.Models.Parameter("Button", "Rate")
            });
            var t = new Microsoft.Phone.Tasks.MarketplaceReviewTask();
            t.Show();
        }

        public void Remind_Click(object sender, RoutedEventArgs e) {
            Hide();
            settings["ReviewPopup-InstallDate"] = DateTime.Now; // reset install date
            settings["ReviewPopup-Completed"] = false;
            FlurryWP7SDK.Api.LogEvent("ReviewPopup", new List<FlurryWP7SDK.Models.Parameter>() {
                new FlurryWP7SDK.Models.Parameter("Button", "Remind")
            });
        }

        private void No_Click(object sender, RoutedEventArgs e) {
            Hide();
            settings["ReviewPopup-Completed"] = true;
            FlurryWP7SDK.Api.LogEvent("ReviewPopup", new List<FlurryWP7SDK.Models.Parameter>() {
                new FlurryWP7SDK.Models.Parameter("Button", "No")
            });
        }
    }
}
