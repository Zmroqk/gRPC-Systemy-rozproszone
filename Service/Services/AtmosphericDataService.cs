using Grpc.Core;
using Services.AtmosphericData;

namespace Service.Services
{
    public class AtmosphericDataService : AtmosphericDataHandler.AtmosphericDataHandlerBase
    {
        private readonly ILogger<AtmosphericDataService> _logger;
        private List<AtmosphericData> CollectedData { get; }

        public AtmosphericDataService(ILogger<AtmosphericDataService> logger)
        {
            _logger = logger;
            CollectedData = new List<AtmosphericData>();
        }

        public override Task<Bool> SaveData(AtmosphericData request, ServerCallContext context)
        {
            CollectedData.Add(request);
            return Task.FromResult(new Bool() { Success = true });
        }

        public override Task<AtmosphericDataResponse> GetAllData(Empty request, ServerCallContext context)
        {
            var response = new AtmosphericDataResponse();
            response.Data.AddRange(CollectedData);
            return Task.FromResult(response);
        }

        public override Task<AtmosphericDataResponse> GetData(AtmosphericDataRequest request, ServerCallContext context)
        {
            var response = new AtmosphericDataResponse();
            response.Data.AddRange(CollectedData.Where(data => DateTime.Parse(data.Date).Date.Equals(DateTime.Parse(request.Date).Date)));
            return Task.FromResult(response);
        }       
    }
}