using System.Windows;
using Microsoft.Phone.Controls;

namespace gtalkchat {
    public partial class SettingsPage : PhoneApplicationPage {
        public SettingsPage() {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            App.Current.GtalkHelper.SetCorrectOrientation(this);

            RagesCheckbox.IsChecked = App.Current.Settings.Contains("rages") && (bool)App.Current.Settings["rages"];
            LandscapeCheckbox.IsChecked = App.Current.Settings.Contains("rotate") && (bool)App.Current.Settings["rotate"];
            ToastCheckbox.IsChecked = !App.Current.Settings.Contains("toastNotification") || (bool)App.Current.Settings["toastNotification"];
            TileCheckbox.IsChecked = !App.Current.Settings.Contains("tileNotification") || (bool)App.Current.Settings["tileNotification"];
            SecondaryTileCheckbox.IsChecked = !App.Current.Settings.Contains("secondaryTileNotification") || (bool)App.Current.Settings["secondaryTileNotification"];
        }

        private void RagesCheckbox_Checked(object sender, RoutedEventArgs e) {
            App.Current.Settings["rages"] = RagesCheckbox.IsChecked;
        }

        private void LandscapeCheckbox_Checked(object sender, RoutedEventArgs e) {
            App.Current.Settings["rotate"] = LandscapeCheckbox.IsChecked;
        }

        private void Notification_Checked(object sender, RoutedEventArgs e) {
            App.Current.GtalkClient.Notifications(
                ToastCheckbox.IsChecked.Value,
                TileCheckbox.IsChecked.Value,
                SecondaryTileCheckbox.IsChecked.Value,
                data => Dispatcher.BeginInvoke(() => { 
                    App.Current.Settings["toastNotification"] = ToastCheckbox.IsChecked;
                    App.Current.Settings["tileNotification"] = TileCheckbox.IsChecked;
                    App.Current.Settings["secondaryTileNotification"] = SecondaryTileCheckbox.IsChecked;
                }), error => {
                }
            );
        }
    }
}