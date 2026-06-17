using System.Collections.Generic;
using UnityEngine;

public interface ISolitaireCommand
{
    void Execute();
    void Undo();
}

public class SolitaireHistory
{
    private Stack<ISolitaireCommand> history = new Stack<ISolitaireCommand>();

    public int Count => history.Count;

    public void Clear() => history.Clear();

    public void PushAndExecute(ISolitaireCommand command)
    {
        command.Execute();
        history.Push(command);
    }

    public void Undo()
    {
        if (history.Count == 0) return;
        ISolitaireCommand command = history.Pop();
        command.Undo();
    }
}
