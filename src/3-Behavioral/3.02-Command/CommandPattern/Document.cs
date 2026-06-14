namespace CommandPattern;

public sealed class Document
{
    private string _content;

    public string Content => _content;

    public Document(string initialContent = "") => _content = initialContent;

    public void Insert(int position, string text)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        if (position > _content.Length)
            throw new ArgumentOutOfRangeException(nameof(position));

        _content = _content[..position] + text + _content[position..];
    }

    public void Delete(int position, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        if (position + length > _content.Length)
            throw new ArgumentOutOfRangeException(nameof(position));

        _content = _content[..position] + _content[(position + length)..];
    }
}
