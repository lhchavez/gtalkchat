using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;

namespace gtalkchat {
    public class ChatBubble : UserControl {

        public static DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(ChatBubble), new PropertyMetadata(""));

        public string Text {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static DependencyProperty TimeStampProperty = DependencyProperty.Register("TimeStamp", typeof(DateTime), typeof(ChatBubble), new PropertyMetadata(DateTime.Now));

        public DateTime TimeStamp {
            get { return (DateTime)GetValue(TimeStampProperty); }
            set { SetValue(TimeStampProperty, value); }
        }

        public ChatBubble() {
            // Create context menu
            ContextMenu menu = new ContextMenu();
            menu.IsZoomEnabled = false;

            MenuItem copy = new MenuItem();
            copy.Header = "copy";
            copy.Click += (s, e) => {
                System.Windows.Clipboard.SetText(Text);
            };

            menu.Items.Add(copy);
            ContextMenuService.SetContextMenu(this, menu);
        }
    }
}
