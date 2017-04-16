﻿namespace DHT.NodesProtocol
{
    using System.Data.Linq;
    using Dhtproto;
    using Grpc.Core;
    using Nodes;

    public class LocalNodeServerClient : DhtProtoService.DhtProtoServiceClient
    {
        private readonly NodeStore nodeStore;

        public LocalNodeServerClient()
        {
            this.nodeStore = new NodeStore();
        }

        public override KeyValueMessage GetValue(KeyMessage request, CallOptions options)
        {
            if (!this.nodeStore.ContainsKey(request.Key))
            {
                ThrowRpcException(StatusCode.NotFound, "Key not found");
            }

            var value = this.nodeStore.GetValue(request.Key);
            var response = new KeyValueMessage()
            {
                Key = request.Key,
                Value = value
            };

            return response;
        }

        public override KeyValueMessage RemoveValue(KeyMessage request, CallOptions options)
        {
            var removed = this.nodeStore.RemoveValue(request.Key);

            if (!removed)
            {
                ThrowRpcException(StatusCode.NotFound, "Key not found, can't remove.");
            }

            var response = new KeyValueMessage()
            {
                Key = request.Key,
                Value = removed ? "removed" : "not removed"
            };

            return response;
        }

        public override KeyValueMessage StoreValue(KeyValueMessage request, CallOptions options)
        {
            try
            {
                var added = this.nodeStore.AddValue(request.Key, request.Value);

                if (!added)
                {
                    ThrowRpcException(StatusCode.Internal, "Couldn't store value.");
                }
            }
            catch (DuplicateKeyException)
            {
                ThrowRpcException(StatusCode.AlreadyExists, "Duplicate key found.");
            }

            return request;
        }

        private void ThrowRpcException(StatusCode statusCode, string message)
        {
            var status = new Status(statusCode, message);
            throw new RpcException(status);
        }
    }
}