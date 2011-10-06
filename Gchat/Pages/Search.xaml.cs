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
using Gchat.Data;

namespace Gchat.Pages {
    public partial class Search : PhoneApplicationPage {
        public Search() {
            InitializeComponent();

            SearchResults.ItemsSource = App.Current.Roster;
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e) {
            SearchBox.Focus();
        }

        private void PinContact_Click(object sender, RoutedEventArgs e) {
            MenuItem item = sender as MenuItem;
            Contact c = item.DataContext as Contact;

            App.Current.GtalkHelper.PinContact(c.Email);
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e) {
            ContextMenu menu = sender as ContextMenu;
            Contact c = menu.DataContext as Contact;
            MenuItem item = menu.Items[0] as MenuItem;

            if (App.Current.GtalkHelper.IsContactPinned(c.Email)) {
                item.IsEnabled = false;
            } else {
                item.IsEnabled = true;
            }
        }

        private void SearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count > 0) {
                var to = (e.AddedItems[0] as Contact).Email;
                (sender as ListBox).SelectedIndex = -1;
                NavigationService.Navigate(new Uri("/Pages/Chat.xaml?from=" + to, UriKind.Relative));
            }
        }
    }
}