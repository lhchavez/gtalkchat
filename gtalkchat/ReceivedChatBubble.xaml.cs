using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace gtalkchat {
    public partial class ReceivedChatBubble : ChatBubble {
        // TODO: Make a superclass for SentChatBubble and ReceivedChatBubble 
        // (when I understand how XAML inheritance works u__u)

        public ReceivedChatBubble() {
            // Required to initialize variables
            InitializeComponent();
        }
    }
}