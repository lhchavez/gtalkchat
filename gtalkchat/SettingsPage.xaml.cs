using System.Windows;
using Microsoft.Phone.Controls;

namespace gtalkchat {
    public partial class SettingsPage : PhoneApplicationPage {
        public SettingsPage() {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            base.OnNavigatedTo(e);

            RagesCheckbox.IsChecked = App.Current.Settings.Contains("rages") && (bool) App.Current.Settings["rages"];
        }

        private void Logout_Click(object sender, RoutedEventArgs e) {
            var gtalkHelper = App.Current.GtalkHelper;
            gtalkHelper.Logout();
        }

        private void RagesCheckbox_Checked(object sender, RoutedEventArgs e) {
            App.Current.Settings["rages"] = RagesCheckbox.IsChecked;
        }
    }
}