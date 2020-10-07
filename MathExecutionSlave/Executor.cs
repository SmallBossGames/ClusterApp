using ClusterWorkerServices;
using System;
using System.Collections.Generic;
using System.Text;

namespace MathExecutionSlave
{
    public class Executor
    {
        public Dictionary<string, string> Execute(int index, int nodeCount, Dictionary<string, string> variables)
        {
            byte[] sourceData = Encoding.UTF8.GetBytes(variables["source"]);
            int hardLevel = int.Parse(variables["hard"]);

            var step = int.MaxValue / nodeCount;

            var (nonce, hash) = index == nodeCount - 1
                ? MathHelpers.FindExpectedHash(sourceData, hardLevel, step * index, int.MaxValue)
                : MathHelpers.FindExpectedHash(sourceData, hardLevel, step * index, step * (index + 1));

            var result = new Dictionary<string, string>
            {
                ["nonce"] = nonce.ToString(),
                ["hash"] = hash,
            };

            return result;
        }
    }
}
