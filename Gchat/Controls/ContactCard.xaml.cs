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

namespace Gchat.Controls {
    public partial class ContactCard : UserControl {
        public static DependencyProperty ContactProperty =
            DependencyProperty.Register("Contact", typeof(Contact), typeof(ContactCard), 
            new PropertyMetadata(null, (s, e) => {
                var a = e.NewValue;
            }));

        public Contact Contact {
            get { return (Contact)GetValue(ContactProperty); }
            set { 
                SetValue(ContactProperty, value); }
        }

        public ContactCard() {
            InitializeComponent();

            //LayoutRoot.DataContext = Contact;
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

        private void PinContact_Click(object sender, RoutedEventArgs e) {
            MenuItem item = sender as MenuItem;
            Contact c = item.DataContext as Contact;

            App.Current.GtalkHelper.PinContact(c.Email);
        }
    }
}
