namespace LunaDraw.Logic.Commands
{
    /// <summary>
    /// Interface for all commands that can be executed and undone.
    /// </summary>
    public interface IDrawCommand
    {
        /// <summary>
        /// Executes the command.
        /// </summary>
        void Execute();

        /// <summary>
        /// Undoes the command.
        /// </summary>
        void Undo();
    }
}
