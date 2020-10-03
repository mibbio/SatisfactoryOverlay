namespace SatisfactoryOverlay.Obs
{
    using Newtonsoft.Json.Linq;

    using System;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class ObsStudioClient : ObsWebsocketClient
    {
        private readonly string _password;

        public override bool NeedsAuthentication { get; } = true;

        public ObsStudioClient(IPAddress address, ushort port, string password = "") : base(address, port)
        {
            _password = password;
        }

        public override async Task ConnectAsync()
        {
            await base.ConnectAsync();

            var authData = await SendRequestAsync("GetAuthRequired");
            if ((bool)authData["authRequired"])
            {
                var challenge = (string)authData["challenge"];
                var salt = (string)authData["salt"];
                var requestFields = GenerateAuthRequestFields(challenge, salt);
                var authResult = await SendRequestAsync("Authenticate", requestFields);

                if ((string)authResult?["status"] != "ok")
                {
                    InvokeErrorEvent(ObsClientErrorType.InvalidPassword);
                }
                else
                {
                    InvokeAuthenticatedEvent();
                }
            }
        }

        public override async Task<Version> GetVersionAsync()
        {
            var result = await SendRequestAsync("GetVersion");
            Version.TryParse((string)result?["obs-studio-version"], out Version version);
            return version;
        }

        protected override async Task<JObject> SendRequestAsync(string requestName, JObject requestBody = null)
        {
            string messageId;
            do
            {
                messageId = GenerateMessageId();
            } while (_pendingRequests.ContainsKey(messageId));

            var data = new JObject
            {
                {"request-type", requestName },
                {"message-id", messageId }
            };

            if (requestBody != null)
            {
                var mergeSettings = new JsonMergeSettings
                {
                    MergeArrayHandling = MergeArrayHandling.Union,
                    MergeNullValueHandling = MergeNullValueHandling.Merge,
                    PropertyNameComparison = StringComparison.OrdinalIgnoreCase
                };
                data.Merge(requestBody, mergeSettings);
            }

            var tcs = new TaskCompletionSource<JObject>();
            var cts = new CancellationTokenSource(_connectionTimeout);

            if (_pendingRequests.TryAdd(messageId, tcs))
            {
                cts.Token.Register(() => tcs.TrySetCanceled(), false);

                await SendMessageAsync(data.ToString()).ConfigureAwait(false);

                try
                {
                    return await tcs.Task.ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    _pendingRequests.TryRemove(messageId, out _);
                    InvokeErrorEvent(ObsClientErrorType.RequestTimeout);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        protected override string FindMessageId(JObject data) => (string)data["message-id"];

        protected override void InvokeConnectedEvent() => base.InvokeConnectedEvent();

        protected override void InvokeAuthenticatedEvent() => base.InvokeAuthenticatedEvent();

        protected override void InvokeDisconnectedEvent() => base.InvokeDisconnectedEvent();

        protected override void InvokeErrorEvent(ObsClientErrorType errorType) => base.InvokeErrorEvent(errorType);

        private JObject GenerateAuthRequestFields(string challenge, string salt)
        {
            var sha256 = new SHA256Managed();

            byte[] secretBytes = Encoding.ASCII.GetBytes(_password + salt);
            byte[] secretHash = sha256.ComputeHash(secretBytes);
            string secret = Convert.ToBase64String(secretHash);

            byte[] responseBytes = Encoding.ASCII.GetBytes(secret + challenge);
            byte[] responseHash = sha256.ComputeHash(responseBytes);
            string response = Convert.ToBase64String(responseHash);

            return new JObject
            {
                { "auth", response }
            };
        }
    }
}
