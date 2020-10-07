using ClusterWorkerServices;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;

namespace ClusterWorkerNode.Services
{
    public class MathExecuterService:MathExecuter.MathExecuterBase
    {
        private readonly ILogger<MathExecuterService> _logger;

        private static Assembly _assembly = null;

        public MathExecuterService(ILogger<MathExecuterService> logger)
        {
            _logger = logger;
        }

        public override Task<SendProgramResponse> SendProgram(SendProgramRequest request, ServerCallContext context)
        {
            var result = request.ProgramBytecode.ToByteArray();

            _assembly = Assembly.Load(result);

            return Task.FromResult(new SendProgramResponse { NodeIndex = request.NodeIndex, SuccessMessage = "Success" });
        }

        public override Task<DataResponse> ExecuteProgram(DataRequest request, ServerCallContext context)
        {
            var inputDictionary = request.Variables.ToDictionary(x => x.Key, x=> x.Value);

            var t = _assembly.GetType("MathExecutionSlave.Executor");

            dynamic instance = Activator.CreateInstance(t);

            var result = (Dictionary<string, string>) instance.Execute(request.NodeIndex, request.NodeCount, inputDictionary);

            var response = new DataResponse()
            {
                NodeIndex = request.NodeIndex,
            };

            response.Variables.Add(result);

            return Task.FromResult(response);
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
