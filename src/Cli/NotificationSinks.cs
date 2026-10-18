using Core.Abstractions;
using Core.Events;

namespace Cli;

public sealed class ConsoleSink : INotificationSink
{
    public void Notify(CatalogChangedEventArgs args)
    {
        Console.WriteLine($"[ConsoleSink] {args.At:HH:mm:ss} {args.Kind,-8} {args.Id} {args.Delta,4:+0;-0} -> залишок {args.QuantityAfter}");
    }
}

public sealed class FileSink : INotificationSink
{
    private readonly string _filePath;

    public FileSink(string filePath)
    {
        _filePath = filePath;
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
    }

    public void Notify(CatalogChangedEventArgs args)
    {
        var line = $"[FileSink] {args.At:HH:mm:ss} {args.Kind,-8} {args.Id} {args.Delta,4:+0;-0} -> залишок {args.QuantityAfter}";
        File.AppendAllText(_filePath, line + Environment.NewLine);
    }
}

public sealed class CompositeNotificationSink : INotificationSink
{
    private readonly IEnumerable<INotificationSink> _sinks;

    public CompositeNotificationSink(params INotificationSink[] sinks)
    {
        _sinks = sinks;
    }

    public void Notify(CatalogChangedEventArgs args)
    {
        foreach (var sink in _sinks)
        {
            sink.Notify(args);
        }
    }
}