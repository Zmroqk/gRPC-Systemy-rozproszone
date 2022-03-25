using Grpc.Net.Client;
using Services.AtmosphericData;
using Services.CloudService;

namespace ClientgRPC
{
    public class gRpcProvider
    {
        public AtmosphericDataHandler.AtmosphericDataHandlerClient AtmosphericDataHandler { get; set; }
        public CloudService.CloudServiceClient CloudService { get; set; }

        public gRpcProvider()
        {
            GrpcChannel channel = GrpcChannel.ForAddress("https://localhost:7204");
            AtmosphericDataHandler = new AtmosphericDataHandler.AtmosphericDataHandlerClient(channel);
            CloudService = new CloudService.CloudServiceClient(channel);
        }     
    }
}