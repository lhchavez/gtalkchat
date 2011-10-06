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

namespace Gchat.Controls {

    /// <summary>
    /// A specialized highlighting text block control.
    /// From http://www.jeff.wilcox.name/2009/08/sl3-highlighting-text-block/
    /// </summary>
    public partial class HighlightedTextBlock : UserControl {
        /// <summary>
        /// Gets or sets the inlines list.
        /// </summary>
        private List<Inline> Inlines { get; set; }

        #region public string Text
        /// <summary>
        /// Gets or sets the contents of the TextBox.
        /// </summary>
        public string Text {
            get { return GetValue(TextProperty) as string; }
            set { 
                SetValue(TextProperty, value); 
            }
        }

        /// <summary>
        /// Identifies the Text dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                "Text",
                typeof(string),
                typeof(HighlightedTextBlock),
                new PropertyMetadata(string.Empty, OnTextPropertyChanged));

        /// <summary>
        /// TextProperty property changed handler.
        /// </summary>
        /// <param name="d">AutoCompleteBox that changed its Text.</param>
        /// <param name="e">Event arguments.</param>
        private static void OnTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            HighlightedTextBlock source = d as HighlightedTextBlock;
            source.ApplyText(e.NewValue as string);
        }

        private void ApplyText(string value) {
            if (TextBlock != null) {
                while (TextBlock.Inlines.Count > 0) {
                    TextBlock.Inlines.RemoveAt(0);
                }
                Inlines = new List<Inline>();
                if (value != null) {
                    for (int i = 0; i < value.Length; i++) {
                        Inline run = new Run { Text = value[i].ToString() };
                        TextBlock.Inlines.Add(run);
                        Inlines.Add(run);
                    }

                    ApplyHighlighting();
                }
            }
        }

        #endregion public string Text

        #region public string HighlightText
        /// <summary>
        /// Gets or sets the highlighted text.
        /// </summary>
        public string HighlightText {
            get { return GetValue(HighlightTextProperty) as string; }
            set { SetValue(HighlightTextProperty, value); }
        }

        /// <summary>
        /// Identifies the HighlightText dependency property.
        /// </summary>
        public static readonly DependencyProperty HighlightTextProperty =
            DependencyProperty.Register(
                "HighlightText",
                typeof(string),
                typeof(HighlightedTextBlock),
                new PropertyMetadata(OnHighlightTextPropertyChanged));

        /// <summary>
        /// HighlightText property changed handler.
        /// </summary>
        /// <param name="d">AutoCompleteBox that changed its HighlightText.</param>
        /// <param name="e">Event arguments.</param>
        private static void OnHighlightTextPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            HighlightedTextBlock source = d as HighlightedTextBlock;
            source.ApplyHighlighting();
        }

        #endregion public string HighlightText

        #region public Brush HighlightBrush
        /// <summary>
        /// Gets or sets the highlight brush.
        /// </summary>
        public Brush HighlightBrush {
            get { return GetValue(HighlightBrushProperty) as Brush; }
            set { SetValue(HighlightBrushProperty, value); }
        }

        /// <summary>
        /// Identifies the HighlightBrush dependency property.
        /// </summary>
        public static readonly DependencyProperty HighlightBrushProperty =
            DependencyProperty.Register(
                "HighlightBrush",
                typeof(Brush),
                typeof(HighlightedTextBlock),
                new PropertyMetadata(null, OnHighlightBrushPropertyChanged));

        /// <summary>
        /// HighlightBrushProperty property changed handler.
        /// </summary>
        /// <param name="d">HighlightingTextBlock that changed its HighlightBrush.</param>
        /// <param name="e">Event arguments.</param>
        private static void OnHighlightBrushPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
            HighlightedTextBlock source = d as HighlightedTextBlock;
            source.ApplyHighlighting();
        }
        #endregion public Brush HighlightBrush

        /// <summary>
        /// Initializes a new HighlightingTextBlock class.
        /// </summary>
        public HighlightedTextBlock() {
            InitializeComponent();
            ApplyText(Text);
        }

        /// <summary>
        /// Apply the visual highlighting.
        /// </summary>
        private void ApplyHighlighting() {
            if (Inlines == null) {
                return;
            }

            string text = Text ?? string.Empty;
            string highlight = HighlightText ?? string.Empty;
            StringComparison compare = StringComparison.OrdinalIgnoreCase;

            int cur = 0;
            while (cur < text.Length) {
                int i = highlight.Length == 0 ? -1 : text.IndexOf(highlight, cur, compare);
                i = i < 0 ? text.Length : i;

                // Clear
                while (cur < i && cur < text.Length) {
                    Inlines[cur].Foreground = Foreground;
                    cur++;
                }

                // Highlight
                int start = cur;
                while (cur < start + highlight.Length && cur < text.Length) {
                    Inlines[cur].Foreground = HighlightBrush;
                    cur++;
                }
            }
        }
    }
}
