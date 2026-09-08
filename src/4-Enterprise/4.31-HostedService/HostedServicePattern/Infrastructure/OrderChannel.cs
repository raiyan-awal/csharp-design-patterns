using System.Threading.Channels;
using HostedServicePattern.Domain;

namespace HostedServicePattern.Infrastructure;

public sealed class OrderChannel
{
    private readonly Channel<Order> _channel = Channel.CreateUnbounded<Order>(
        new UnboundedChannelOptions { SingleReader = true });

    public ChannelWriter<Order> Writer => _channel.Writer;
    public ChannelReader<Order> Reader => _channel.Reader;
}
