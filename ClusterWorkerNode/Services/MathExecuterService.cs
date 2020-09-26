using ClusterWorkerServices;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace ClusterWorkerNode.Services
{
    public class MathExecuterService:MathExecuter.MathExecuterBase
    {
        private readonly ILogger<MathExecuterService> _logger;

        public MathExecuterService(ILogger<MathExecuterService> logger)
        {
            _logger = logger;
        }

        public override Task<MathDataResponse> Execute(MathDataRequest request, ServerCallContext context)
        {
            var bytes = Encoding.UTF8.GetBytes(request.Text);

            var index = (int)request.Index;

            var size = (int)request.Size;

            var step = int.MaxValue / size;

            var (nonce, hash) = request.Index == request.Size - 1
                ? MathHelpers.FindExpectedHash(bytes, request.HardLevel, step * index, int.MaxValue)
                : MathHelpers.FindExpectedHash(bytes, request.HardLevel, step * index, step * (index + 1));

            var response = new MathDataResponse()
            {
                Hash = hash,
                Nonce = nonce
            };

            return Task.FromResult(response);
        }
    }
}
