using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BnsDatTool.lib.UndoRedo
{
    public class EditableElement
    {
        public TranslatableItem Element { get; private set; }

        public string OldTranslate { get; private set; }

        public string NewTranslate { get; private set; }

        public EditableElement(TranslatableItem element, string newTranslate)
        {
            Element = element;
            OldTranslate = element.Translate;
            NewTranslate = newTranslate;
        }
    }
}
