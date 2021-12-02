using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elsa.Services;

namespace Elsa.Activities.Mqtt.Bookmarks
{
    public class MessageReceivedBookmark : IBookmark
    {
        public MessageReceivedBookmark()
        {
        }

        public MessageReceivedBookmark(string topic)
        {
            Topic = topic;
        }

        public string Topic { get; set; } = default!;
    }

    public class MessageReceivedBookmarkProvider : BookmarkProvider<MessageReceivedBookmark, MqttMessageReceived>
    {
        public override async ValueTask<IEnumerable<BookmarkResult>> GetBookmarksAsync(BookmarkProviderContext<MqttMessageReceived> context, CancellationToken cancellationToken) =>
            new[]
            {
                Result(new MessageReceivedBookmark
                {
                    Topic = (await context.ReadActivityPropertyAsync(x => x.Topic, cancellationToken))!,
                })
            };
    }
}