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
        }

        private void RagesCheckbox_Checked(object sender, RoutedEventArgs e) {
            App.Current.Settings["rages"] = RagesCheckbox.IsChecked;
        }

        private void LandscapeCheckbox_Checked(object sender, RoutedEventArgs e) {
            App.Current.Settings["rotate"] = LandscapeCheckbox.IsChecked;
        }
    }
}