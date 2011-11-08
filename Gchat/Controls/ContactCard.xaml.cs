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
using FlurryWP7SDK.Models;

namespace Gchat.Controls {
    public partial class ContactCard : UserControl {
        public ContactCard() {
            InitializeComponent();
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

            MenuItem hidden = menu.Items[1] as MenuItem;
            hidden.Header = c.Hidden ? AppResources.Contact_ContextMenuUnhide : AppResources.Contact_ContextMenuHide;
        }

        private void PinContact_Click(object sender, RoutedEventArgs e) {
            MenuItem item = sender as MenuItem;
            Contact c = item.DataContext as Contact;

            App.Current.GtalkHelper.PinContact(c.Email);
        }

        private void HideContact_Click(object sender, RoutedEventArgs e) {
            MenuItem item = sender as MenuItem;
            Contact c = item.DataContext as Contact;

            c.Hidden = !c.Hidden;

            FlurryWP7SDK.Api.LogEvent("Contact hidden toggled", new List<Parameter>() {
                new Parameter("Hidden", c.Hidden.ToString())
            });

            App.Current.Roster.Update(c);
            App.Current.ContactList.UpdateRoster();
        }
    }
}
