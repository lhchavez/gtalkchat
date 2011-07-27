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

namespace gtalkchat {
    public partial class LinkifiedTextBox : UserControl {
        public static DependencyProperty TextProperty = 
            DependencyProperty.Register("Text", typeof(string), typeof(LinkifiedTextBox), 
            new PropertyMetadata("", ChangedText));

        public string Text {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); } 
        }

        private static void ChangedText(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            ((LinkifiedTextBox)d).ChangedText(e);
        }

        private void ChangedText(DependencyPropertyChangedEventArgs e) {
            if (e.OldValue != e.NewValue) {
                Paragraph richtext = GoogleTalkHelper.Linkify((string) e.NewValue);
                RichText.Blocks.Add(richtext);
            }
        }

        public LinkifiedTextBox() {
            InitializeComponent();
        }
    }
}
