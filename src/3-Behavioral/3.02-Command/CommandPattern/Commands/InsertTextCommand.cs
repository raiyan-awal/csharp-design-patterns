namespace CommandPattern;

public sealed class InsertTextCommand : ICommand
{
    private readonly Document _document;
    private readonly int      _position;
    private readonly string   _text;

    public InsertTextCommand(Document document, int position, string text)
    {
        _document = document;
        _position = position;
        _text     = text;
    }

    public void Execute() => _document.Insert(_position, _text);
    public void Undo()    => _document.Delete(_position, _text.Length);
}
