using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BnsDatTool.lib.UndoRedo
{
    internal class EditCommand
    {
        private readonly IEnumerable<EditableElement> _elements;

        public EditCommand(EditableElement element)
        {
            _elements = new List<EditableElement> { element };
        }

        public EditCommand(IEnumerable<EditableElement> elements)
        {
            _elements = new List<EditableElement>(elements);
        }

        public void Execute()
        {
            foreach (EditableElement element in _elements)
                element.Element.UpdateTranslate(element.NewTranslate, false);
        }

        public void UnExecute()
        {
            foreach (EditableElement element in _elements)
                element.Element.UpdateTranslate(element.OldTranslate, false);
        }
    }
}
