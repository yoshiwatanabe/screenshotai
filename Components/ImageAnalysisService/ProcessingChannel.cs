
using System.Threading.Channels;

namespace ImageAnalysisService
{
    public class ProcessingChannel
    {
        private readonly Channel<string> _channel = Channel.CreateUnbounded<string>();

        public ChannelReader<string> Reader => _channel.Reader;
        public ChannelWriter<string> Writer => _channel.Writer;
    }
}
