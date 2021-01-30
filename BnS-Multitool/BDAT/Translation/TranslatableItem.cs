using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BnsDatTool.lib
{
    public class TranslatableItem
    {
        public enum Fields { AutoId, Alias, Original, Translate, Untranslation }

        public enum TranslateState { Translated, PartiallyTranslated, NotTranslated }

        public int AutoId { get; private set; }

        public string Alias { get; private set; }

        public int Priority { get; private set; }

        public string Original { get; private set; }

        public string Translate { get; private set; }

        public string Type { get; private set; }

        public TranslateState State { get; private set; }

        public TranslatableItem(int autoId, string alias, string original, string translate, string type)
        {
            AutoId = autoId;
            Alias = alias;
            Original = original;
            Translate = translate;
            Type = type;
            //UpdateTranslateState();
        }
        public void UpdateTranslateState()
        {
            if (string.Equals(Original, Translate, StringComparison.CurrentCulture))
                State = TranslateState.NotTranslated;
            else if (Translate.Any(CharExtension.IsChineseChar) ||
                Translate.Any(CharExtension.IsJapanChar) ||
                Translate.Any(CharExtension.IsKoreaChar) ||
                Translate.Any(CharExtension.IsThaiChar))
                State = TranslateState.PartiallyTranslated;
            else
                State = TranslateState.Translated;
        }
        internal void UpdateTranslate(string newValue, bool useGtran)
        {
            Translate = newValue;
            if (useGtran)
                Type = "gg";
        }
        internal string ResetTranslate()
        {
            Translate = Original;
            Type = "nc";
            return Translate;
        }
    }
}
