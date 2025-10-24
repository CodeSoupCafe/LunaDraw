using ReactiveUI;
using System.Collections.Generic;

namespace LunaDraw.Logic.Commands
{
    /// <summary>
    /// Manages the history of executed commands for undo/redo functionality.
    /// </summary>
    public class CommandHistory : ReactiveObject
    {
        private readonly Stack<IDrawCommand> _undoStack = new Stack<IDrawCommand>();
        private readonly Stack<IDrawCommand> _redoStack = new Stack<IDrawCommand>();

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        public void Execute(IDrawCommand command)
        {
            command.Execute();
            _undoStack.Push(command);
            _redoStack.Clear();
            UpdateCanExecute();
        }

        public void Undo()
        {
            if (CanUndo)
            {
                var command = _undoStack.Pop();
                command.Undo();
                _redoStack.Push(command);
                UpdateCanExecute();
            }
        }

        public void Redo()
        {
            if (CanRedo)
            {
                var command = _redoStack.Pop();
                command.Execute();
                _undoStack.Push(command);
                UpdateCanExecute();
            }
        }

        private void UpdateCanExecute()
        {
            this.RaisePropertyChanged(nameof(CanUndo));
            this.RaisePropertyChanged(nameof(CanRedo));
        }
    }
}
