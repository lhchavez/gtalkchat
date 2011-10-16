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
using System.Threading;
using System.Windows.Navigation;

namespace Gchat.Pages {
    public partial class Search : PhoneApplicationPage {
        private Thread searchThread;
        private Searcher searcher;

        public Search() {
            InitializeComponent();

            SearchResults.ItemsSource = App.Current.Roster;
            searcher = new Searcher(results => Dispatcher.BeginInvoke(() => SearchResults.ItemsSource = results));
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e) {
            SearchBox.Focus();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e) {
            searchThread = new Thread(searcher.Run);
            searchThread.Start();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            searcher.Stop();
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

        private void SearchBox_KeyUp(object sender, KeyEventArgs e) {
            searcher.Search(SearchBox.Text.ToLower());
        }
    }

    class Searcher {
        public delegate void SearcherDelegate(List<Contact> results);
        private SearcherDelegate callback;
        private ManualResetEvent mutex = new ManualResetEvent(false);
        private string search;

        public bool IsRunning { get; set; }

        public Searcher(SearcherDelegate callback) {
            this.callback = callback;
        }

        public void Stop() {
            IsRunning = false;
            mutex.Set();
        }

        public void Run() {
            IsRunning = true;

            while (IsRunning) {
                mutex.WaitOne();
                mutex.Reset();

                if (!IsRunning) break;

                var currentSearch = search;

                var results = new List<Contact>();

                foreach (var contact in App.Current.Roster) {
                    if (contact.Matches(currentSearch)) {
                        results.Add(contact);
                    }
                }

                callback(results);

                // sleepyhead.
                Thread.Sleep(1000);
            }
        }

        public void Search(string search) {
            this.search = search;
            mutex.Set();
        }
    }
}