using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Waimea.Channel;
using Waimea.Channel.Messages.Common;
using Waimea.Channel.Messages.ToBackend;
using Waimea.Channel.Messages.ToFrontend;

namespace WBStreamer
{
    internal class Program
    {
        private const string WB_ORIGIN = "https://alphasandbox.dev.waimeabae.com";
        //private const string WB_ORIGIN = "https://alpha.waimeabae.com";
        private const uint AUDIO_FEED_VIEW_ID = 1;
        private FrontendChannel channel = null;
        private ILoggerFactory loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        private FeedId roomFeedId = new FeedId() { Name = "room" };

        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                var program = new Program();
                await program.Connect("1800-go-away");
            });

            Console.WriteLine("Background task started!");

            Console.ReadLine();
        }

        public async Task Connect(string roomId)
        {
            var backendConnected = new TaskCompletionSource();

            channel = new FrontendChannel(Str0mBackendFactory.Create, (message, attachment) => {
                switch (message.MessageCase)
                {
                    case ToFrontendMessage.MessageOneofCase.ConnectionMetadata: {
                            using (var client = new HttpClient())
                            {
                                var metadata = message.ConnectionMetadata;
                                using (var content = JsonContent.Create(new {
                                        metadata.EndpointId,
                                        metadata.SecretToken,
                                        profile = "videoconf",
                                        roomId,
                                        AppEntityId = "*"
                                    }))
                                {
                                    var result = client.PostAsync(new Uri(WB_ORIGIN + "/samples/app_server/activate"), content).Result;
                                    Console.WriteLine($"Activation completed: {result.StatusCode}");
                                }
                            }

                            backendConnected.SetResult();

                            channel?.StartPumpingOpusFrames();

                            break;
                        }
                    case ToFrontendMessage.MessageOneofCase.ConnectionStateChange: {
                            var stateChanged = message.ConnectionStateChange;
                            switch (stateChanged.State)
                            {
                                case ConnectionState.Connected:
                                    Console.WriteLine("Connected to WaimeaBay.");
                                    break;
                                case ConnectionState.Disconnected:
                                    Console.WriteLine("Disconnected from WaimeaBay.");
                                    break;
                                default:
                                    break;
                            }

                            break;
                        }
                    default:
                        break;
                }
            }, loggerFactory.CreateLogger<FrontendChannel>());

            channel.Send(new()
            {
                AddFeedView = new()
                {
                    FeedViewId = AUDIO_FEED_VIEW_ID,
                    FeedId = roomFeedId,
                    AudioConfig = new()
                    {
                        MaxStreams = 1,
                        ReflectionMode = ReflectionMode.None,
                    },
                }
            });

            channel.Send(new() {
                AddOutboundMedia = new() {
                    SourceId = 1,
                    AppEntityId = new AppEntityId("ACSCalling").ToByteString(),
                    MediaType = new() { Audio = new() },
                    AttachmentFormat = AttachmentFormat.Encoded,
                    FeedIds = { roomFeedId }
                }
            });

            channel.Send(new() {
                Connect = new() {
                    FrontdoorOrigin = WB_ORIGIN
                }
            });

            Console.WriteLine("Waiting for Connect.");
        }
    }
}
