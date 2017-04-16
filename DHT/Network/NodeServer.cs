﻿namespace DHT.Network
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using ArgumentValidator;
    using Dhtproto;
    using Grpc.Core;
    using Nodes;
    using Routing;
    using grpc = global::Grpc.Core;

    public class NodeServer : DhtProtoService.DhtProtoServiceBase
    {
        private readonly NodeInfo nodeInfo;

        private readonly NodeStore nodeStore;

        private readonly IRoutingTable routingTable;

        public NodeServer(NodeInfo nodeInfo, IRoutingTable routingTable)
        {
            Throw.IfNull(nodeInfo, nameof(nodeInfo));
            Throw.IfNull(routingTable, nameof(routingTable));

            this.nodeInfo = nodeInfo;
            this.routingTable = routingTable;
            this.nodeStore = new NodeStore();
        }

        public override Task<StringMessage> SayHello(StringMessage request, grpc::ServerCallContext context)
        {
            var stringMessage = new StringMessage()
            {
                Message = "Received " + request.Message
            };

            return Task.FromResult<StringMessage>(stringMessage);
        }

        public override Task<KeyValueMessage> GetValue(KeyMessage request, grpc.ServerCallContext context)
        {
            // Find the node which should store this key, value
            KeyValueMessage response = null;
            var key = request.Key;
            var node = this.routingTable.FindNode(key);

            // If it's us, we should get it from the local store
            if (node.NodeId == this.nodeInfo.NodeId)
            {
                if (this.nodeStore.ContainsKey(key))
                {
                    var value = this.nodeStore.GetValue(key);
                    response = new KeyValueMessage()
                    {
                        Key = key,
                        Value = value
                    };
                }
            }
            else
            {
                // If it's not us, we ask that node remotely
                response = this.GetValueRemote(node, key);
            }

            return Task.FromResult(response);
        }

        public override Task<KeyValueMessage> StoreValue(KeyValueMessage request, grpc.ServerCallContext context)
        {
            // Find the node which should store this key, value
            KeyValueMessage response = null;
            var key = request.Key;
            var value = request.Value;
            var node = this.routingTable.FindNode(key);

            // If it's us, we should store it in the local store
            if (node.NodeId == this.nodeInfo.NodeId)
            {
                this.nodeStore.AddValue(key, value);
            }
            else
            {
                // If it's not us, we store in that node remotely
                response = this.StoreValueRemote(node, key, value);
            }

            return Task.FromResult(response);
        }

        private KeyValueMessage GetValueRemote(NodeInfo node, string key)
        {
            var target = string.Format("{0}:{1}", node.HostName, node.Port);
            var channel = new Channel(target, ChannelCredentials.Insecure);
            var client = new DhtProtoService.DhtProtoServiceClient(channel);

            var request = new KeyMessage()
            {
                Key = key
            };

            var clientResponse = client.GetValue(request);

            return clientResponse;
        }

        private KeyValueMessage StoreValueRemote(NodeInfo node, string key, string value)
        {
            var target = string.Format("{0}:{1}", node.HostName, node.Port);
            var channel = new Channel(target, ChannelCredentials.Insecure);
            var client = new DhtProtoService.DhtProtoServiceClient(channel);

            var request = new KeyValueMessage()
            {
                Key = key,
                Value = value
            };

            var clientResponse = client.StoreValue(request);

            return clientResponse;
        }
    }
}
