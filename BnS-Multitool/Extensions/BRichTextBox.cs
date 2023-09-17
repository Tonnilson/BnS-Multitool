using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace BnS_Multitool.Extensions
{
    public sealed class BRichTextBox : RichTextBox
    {
        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source",
                typeof(Uri), typeof(BRichTextBox),
                new PropertyMetadata(OnSourceChanged));

        public Uri Source
        {
            get => GetValue(SourceProperty) as Uri;
            set => SetValue(SourceProperty, value);
        }

        private static void OnSourceChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is BRichTextBox rtf && rtf.Source != null)
            {
                var stream = Application.GetResourceStream(rtf.Source);
                if (stream != null)
                {
                    var range = new TextRange(rtf.Document.ContentStart, rtf.Document.ContentEnd);
                    range.Load(stream.Stream, DataFormats.Rtf);
                }
            }
        }
    }
}
