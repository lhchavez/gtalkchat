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
using Microsoft.Phone.Controls;
using Microsoft.Phone.Tasks;

namespace gtalkchat {
    public partial class SettingsPage : PhoneApplicationPage {
        public SettingsPage() {
            InitializeComponent();
        }

        private void LaunchWebPage(string url) {
            WebBrowserTask t = new WebBrowserTask();
            t.Uri = new Uri(url);
            t.Show();
        }

        private void Luis_Click(object sender, RoutedEventArgs e) {
            LaunchWebPage("http://twitter.com/lhchavez");
        }

        private void Juliana_Click(object sender, RoutedEventArgs e) {
            LaunchWebPage("http://julianapena.com");
        }

        private void Github_Click(object sender, RoutedEventArgs e) {
            LaunchWebPage("https://github.com/lhchavez/gtalkchat");
        }

        private void Logout_Click(object sender, RoutedEventArgs e) {
            var gtalkHelper = App.Current.GtalkHelper;
            gtalkHelper.Logout();
        }
    }
}