using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Data;

namespace gtalkchat {
    public partial class SentChatBubble : ChatBubble {
        public SentChatBubble() {
            // Required to initialize variables
            InitializeComponent();

            // Set colors
            Color c = (Color) App.Current.Resources["PhoneAccentColor"];
            c.R = (byte)(c.R * 0.7);
            c.G = (byte)(c.G * 0.7);
            c.B = (byte)(c.B * 0.7);

            BubbleBg.Fill = new SolidColorBrush(c);
            BubblePoint.Fill = new SolidColorBrush(c);
        }
    }
}