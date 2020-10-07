using ClusterWorkerNode;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using static ClusterWorkerNode.MathExecuter;

namespace MathExecutorMaster
{
    public class Executor
    {
        public async Task<Dictionary<string, string>> ExecuteAsync(Dictionary<string, string> variables, MathExecuterClient[] nodes)
        {
            if(nodes.Length == 0)
            {
                return new Dictionary<string, string>();
            }

            var pool = new Task<DataResponse>[nodes.Length];

            using var tokenSource = new CancellationTokenSource();

            for (int i = 0; i < pool.Length; i++)
            {
                var request = new DataRequest()
                {
                    NodeIndex = i,
                    NodeCount = pool.Length,
                };

                request.Variables.Add(variables);

                pool[i] = nodes[i].ExecuteProgramAsync(request, null, null, tokenSource.Token).ResponseAsync;
            }

            var resultDictionary = await Task.WhenAny(pool).ConfigureAwait(false);

            tokenSource.Cancel();

            return resultDictionary.Result.Variables.ToDictionary(x => x.Key, x => x.Value);
        }
    }
}
