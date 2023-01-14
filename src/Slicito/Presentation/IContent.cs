namespace Slicito.Presentation;

public interface IContent
{
    void WriteHtmlTo(TextWriter writer);

    void WriteMarkdownTo(TextWriter writer);
}
