namespace CommandPattern;

public sealed class DeleteTextCommand : ICommand
{
    private readonly Document _document;
    private readonly int      _position;
    private readonly int      _length;
    private string?           _deletedText;  // captured at Execute time for Undo

    public DeleteTextCommand(Document document, int position, int length)
    {
        _document = document;
        _position = position;
        _length   = length;
    }

    public void Execute()
    {
        _deletedText = _document.Content.Substring(_position, _length);
        _document.Delete(_position, _length);
    }

    public void Undo()
    {
        if (_deletedText is null)
            throw new InvalidOperationException("Execute must be called before Undo.");
        _document.Insert(_position, _deletedText);
    }
}
