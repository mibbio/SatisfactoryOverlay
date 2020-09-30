﻿namespace SatisfactoryOverlay.Obs
{
    using Newtonsoft.Json.Linq;

    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Net.WebSockets;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public abstract class ObsWebsocketClient : IObsClient
    {
        private const int ReceiveChunkSize = 1024;
        private const int SendChunkSize = 1024;

        private readonly ClientWebSocket _cws;
        private readonly TimeSpan _connectionTimeout = TimeSpan.FromSeconds(30);

        protected readonly Dictionary<string, TaskCompletionSource<JObject>> _pendingRequests;

        protected Uri _uri;

        public bool IsConnected => _cws.State == WebSocketState.Open;

        public ObsWebsocketClient(IPAddress address, ushort port)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (address.AddressFamily == AddressFamily.InterNetwork)
            {
                _uri = new Uri($"ws://{address}:{port}");
            }
            else if (address.AddressFamily == AddressFamily.InterNetworkV6)
            {
                _uri = new Uri($"ws://[{address}]:{port}");
            }
            else
            {
                throw new ArgumentException("Invalid address family", nameof(address));
            }

            _cws = new ClientWebSocket();
            _cws.Options.KeepAliveInterval = TimeSpan.FromSeconds(30);
            _pendingRequests = new Dictionary<string, TaskCompletionSource<JObject>>();
        }

        public virtual async Task ConnectAsync()
        {
            await _cws.ConnectAsync(_uri, new CancellationTokenSource(_connectionTimeout).Token);
            if (_cws.State == WebSocketState.Open)
            {
                var listenerTask = Task.Run(StartListening);
                OnConnected?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task DisconnectAsync()
        {
            try
            {
                if (_cws.State == WebSocketState.Open)
                {
                    await _cws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
                }
            }
            catch (Exception)
            {
                Helper.DebugBreak();
            }
            finally
            {
                foreach (var request in _pendingRequests)
                {
                    request.Value.TrySetCanceled();
                }
                OnDisconnected?.Invoke(this, EventArgs.Empty);
            }
        }

        public async Task UpdateDisplayAsync(string elementName, string text)
        {
            throw new NotImplementedException();
        }

        public abstract Task<Version> GetVersionAsync();

        protected async Task SendMessageAsync(string message)
        {
            if (_cws?.State != WebSocketState.Open)
            {
                throw new WebSocketException(WebSocketError.InvalidState);
            }

            var messageBuffer = Encoding.UTF8.GetBytes(message);
            var messageCount = (int)Math.Ceiling((double)messageBuffer.Length / SendChunkSize);

            for (int i = 0; i < messageCount; i++)
            {
                var offset = (SendChunkSize * i);
                var count = SendChunkSize;
                var isLastMessage = ((i + 1) == messageCount);

                if ((count * (i + 1)) > messageBuffer.Length)
                {
                    count = messageBuffer.Length - offset;
                }

                await _cws.SendAsync(new ArraySegment<byte>(messageBuffer, offset, count), WebSocketMessageType.Text, isLastMessage, new CancellationTokenSource(_connectionTimeout).Token);
            }
        }

        protected abstract Task<JObject> SendRequestAsync(string request, JObject requestFields);

        protected abstract string FindMessageId(JObject data);

        protected static string GenerateMessageId(int length = 16)
        {
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var random = new Random();

            string result = "";
            for (int i = 0; i < length; i++)
            {
                int index = random.Next(0, pool.Length - 1);
                result += pool[index];
            }

            return result;
        }

        private void StartListening()
        {
            var buffer = new byte[ReceiveChunkSize];

            while (_cws.State == WebSocketState.Open)
            {
                try
                {
                    var stringResult = new StringBuilder();

                    WebSocketReceiveResult result;
                    do
                    {
                        result = _cws.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).Result;

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            DisconnectAsync().Wait();
                        }
                        else
                        {
                            var str = Encoding.UTF8.GetString(buffer, 0, result.Count);
                            stringResult.Append(str);
                        }
                    } while (!result.EndOfMessage);

                    var body = JObject.Parse(stringResult.ToString());
                    var messageId = FindMessageId(body);
                    var handler = _pendingRequests[messageId];

                    if (handler != null)
                    {
                        handler.TrySetResult(body);
                        lock (_pendingRequests)
                        {
                            _pendingRequests.Remove(messageId);
                        }
                    }
                }
                catch (Exception)
                {
                    if (_cws.State != WebSocketState.Open)
                    {

                    }
                    //TODO handle different exceptions
                    // WebSocketException, AggregateException (TaskCancelled), JsonReaderException
                    Helper.DebugBreak();
                }
            }
        }

        public event EventHandler OnConnected;

        public event EventHandler OnDisconnected;

        public event EventHandler<Exception> OnClientError;
    }
}
