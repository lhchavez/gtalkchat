using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using FlurryWP7SDK.Models;
using Gchat.Data;
using Microsoft.Phone.Controls;

namespace Gchat.Pages {
    public partial class Search : PhoneApplicationPage {
        private string oldSearch;

        public Search() {
            InitializeComponent();
            SearchResults.ItemsSource = App.Current.Roster;

            oldSearch = string.Empty;

            DispatcherTimer t = new DispatcherTimer();
            t.Tick += (s, e) => FilterListForSearch();
            t.Interval = new TimeSpan(1000);
            t.Start();
        }

        private void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e) {
            SearchBox.Focus();
        }

        protected override void OnNavigatedTo(System.Windows.Navigation.NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            FlurryWP7SDK.Api.LogEvent("Search - Search started", true);
        }

        protected override void OnNavigatingFrom(System.Windows.Navigation.NavigatingCancelEventArgs e) {
            base.OnNavigatingFrom(e);
            FlurryWP7SDK.Api.EndTimedEvent("Search - Search started");
        }

        private void SearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (e.AddedItems.Count > 0) {

                FlurryWP7SDK.Api.LogEvent("Initiated chat", new List<Parameter>() {
                    new Parameter("Source", "Search")
                });

                var to = (e.AddedItems[0] as Contact).Email;
                (sender as ListBox).SelectedIndex = -1;
                NavigationService.Navigate(new Uri("/Pages/Chat.xaml?from=" + to, UriKind.Relative));
            }
        }

        private void FilterListForSearch() {
            Dispatcher.BeginInvoke(() => {
                var search = SearchBox.Text.ToLower();
                if (oldSearch != search) {
                    oldSearch = search;

                    BackgroundWorker w = new BackgroundWorker();
                    w.DoWork += (s, e) => {
                        var results = new List<Contact>();
                        foreach (var contact in App.Current.Roster) {
                            if (contact.Matches(search)) {
                                results.Add(contact);
                            }
                        }
                        Dispatcher.BeginInvoke(() => SearchResults.ItemsSource = results);
                    };
                    w.RunWorkerAsync();
                }
            });
        }
    }
}