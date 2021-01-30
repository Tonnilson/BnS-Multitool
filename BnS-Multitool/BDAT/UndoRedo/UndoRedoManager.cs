using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BnsDatTool.lib.UndoRedo
{
    public class UndoRedoManager
    {
        private readonly Stack<EditCommand> _undoCommands = new Stack<EditCommand>();
        private readonly Stack<EditCommand> _redoCommands = new Stack<EditCommand>();

        public bool UndoListIsEmpty { get { return _undoCommands.Count == 0; } }

        public bool RedoListIsEmpty { get { return _redoCommands.Count == 0; } }

        public bool Undo()
        {
            if (UndoListIsEmpty)
                return false;

            EditCommand command = _undoCommands.Pop();
            command.UnExecute();
            _redoCommands.Push(command);
            return true;
        }

        public bool Redo()
        {
            if (RedoListIsEmpty)
                return false;

            EditCommand command = _redoCommands.Pop();
            command.Execute();
            _undoCommands.Push(command);
            return true;
        }

        private void Add(EditCommand command)
        {
            _undoCommands.Push(command);
            _redoCommands.Clear();
        }

        public void Add(IEnumerable<EditableElement> elements)
        {
            Add(new EditCommand(elements));
        }

        public void Add(EditableElement element)
        {
            Add(new EditCommand(element));
        }
    }

}
