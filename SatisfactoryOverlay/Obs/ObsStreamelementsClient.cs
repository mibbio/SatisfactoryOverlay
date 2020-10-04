namespace SatisfactoryOverlay.Obs
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using System;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    public class ObsStreamelementsClient : ObsWebsocketClient
    {
        private const string jsonrpcVersion = "2.0";

        private readonly string _apiToken;

        public override bool NeedsAuthentication { get; } = true;

        public ObsStreamelementsClient(IPAddress address, ushort port, string apiToken) : base(address, port)
        {
            _apiToken = apiToken;
            _uri = new Uri(_uri, "/api/websocket");
        }

        public override async Task ConnectAsync()
        {
            await base.ConnectAsync();
            var authResult = await AuthenticateAsync();

            if (authResult.ContainsKey("error"))
            {
                InvokeErrorEvent(ObsClientErrorType.InvalidPassword, (string)authResult["error"]["message"]);
            }
            else
            {
                InvokeAuthenticatedEvent();
            }
        }

        public override async Task UpdateDisplayAsync(string elementName, string text)
        {
            var fields = new JObject
            {
                { "resource", "SourcesService" },
                { "args", new JArray("satisInfo") }
            };

            var result = await SendRequestAsync("getSourcesByName", fields);

            if (result["result"] is JArray sources && sources.Count > 0)
            {
                var resourceId = (string)sources[0]["resourceId"];

                result = await SendRequestAsync("getSettings", new JObject { { "resource", resourceId } });

                if (result.ContainsKey("result"))
                {
                    var args = new JObject { { "text", text } };
                    fields = new JObject
                    {
                        { "resource", resourceId },
                        { "args",  new JArray(args) }
                    };
                    await SendRequestAsync("updateSettings", fields);
                }
            }
            else
            {
                InvokeErrorEvent(ObsClientErrorType.ElementNotFound, string.Empty);
            }
        }

        public override async Task<Version> GetVersionAsync() => await Task.FromException<Version>(new NotSupportedException());

        protected override async Task<JObject> SendRequestAsync(string requestName, JObject requestBody = null)
        {
            string messageId;
            do
            {
                messageId = GenerateMessageId();
            } while (_pendingRequests.ContainsKey(messageId));

            var data = new JObject
            {
                { "jsonrpc", jsonrpcVersion },
                { "id", messageId },
                { "method", requestName },
                { "params", requestBody ??= new JObject() }
            };

            var tcs = new TaskCompletionSource<JObject>();
            var cts = new CancellationTokenSource(_connectionTimeout);

            if (_pendingRequests.TryAdd(messageId, tcs))
            {
                cts.Token.Register(() => tcs.TrySetCanceled(), false);
                await SendMessageAsync(data.ToString(Formatting.None)).ConfigureAwait(false);

                try
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
                catch (TaskCanceledException tcEx)
                {
                    _pendingRequests.TryRemove(messageId, out _);
                    InvokeErrorEvent(ObsClientErrorType.RequestTimeout, tcEx.Message);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        protected override string FindMessageId(JObject data) => (string)data["id"];

        private async Task<JObject> AuthenticateAsync()
        {
            var parameters = new JObject
            {
                { "resource", "TcpServerService" },
                { "args", new JArray(_apiToken) }
            };

            return await SendRequestAsync("auth", parameters);
        }

        protected override void InvokeConnectedEvent() => base.InvokeConnectedEvent();

        protected override void InvokeAuthenticatedEvent() => base.InvokeAuthenticatedEvent();

        protected override void InvokeDisconnectedEvent() => base.InvokeDisconnectedEvent();

        protected override void InvokeErrorEvent(ObsClientErrorType errorType, string message) => base.InvokeErrorEvent(errorType, message);
    }
}
