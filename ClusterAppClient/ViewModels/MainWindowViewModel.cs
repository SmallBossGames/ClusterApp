using ClusterWorkerNode;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace ClusterAppClient.ViewModels
{
    class MainWindowViewModel : INotifyPropertyChanged
    {
        private string outputProgressText;

        public ObservableCollection<ClusterNodePath> NodeUrls { get; } = new ObservableCollection<ClusterNodePath>();

        public ObservableCollection<VariablesKeyValuePair> Variables { get; } = new ObservableCollection<VariablesKeyValuePair>();

        public string SourceText { get; set; } = string.Empty;

        public string MasterDllPath { get; set; } = string.Empty;

        public string SlaveDllPath { get; set; } = string.Empty;

        public int SelectedNodeIndex { get; set; }

        public ClusterNodePath SelectedNode { get; set; }

        public int SelectedVariableIndex { get; set; }

        public VariablesKeyValuePair SelectedVariable { get; set; }

        private Assembly _masterAssembly = null;

        public string OutputProgressText 
        { 
            get => outputProgressText; 
            set 
            {
                outputProgressText = value;
                OnPropertyChanged();
            } 
        }

        public int HardLevel { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void AddNode()
        {
            NodeUrls.Add(new ClusterNodePath { Url = "https://127.0.0.1:5001" });
        }

        public void RemoveNode()
        {
            NodeUrls.Remove(SelectedNode);
        }

        public void AddVariable()
        {
            Variables.Add(new VariablesKeyValuePair());
        }

        public void RemoveVariable()
        {
            Variables.Remove(SelectedVariable);
        }

        public async Task ExecuteProgramAsync(Dispatcher dispatcher)
        {
            var pool = new Task<MathDataResponse>[NodeUrls.Count];

            var sb = new StringBuilder();

            await dispatcher.InvokeAsync(() =>
            {
                sb.AppendLine("Process started.");
                OutputProgressText = sb.ToString();
            });

            var variablesDictionary = Variables.ToDictionary(x => x.Key, x => x.Value);

            var watch = new Stopwatch();

            watch.Start();

            var result = await ExecuteProgramAsync(NodeUrls.Select(x => x.Url).ToArray(), variablesDictionary);

            watch.Stop();

            await dispatcher.InvokeAsync(() =>
            {
                sb.AppendLine("Result: ").AppendLine();

                foreach (var item in result)
                {
                    sb.AppendLine($"{item.Key}={item.Value}");
                }

                sb.AppendLine();

                sb.AppendLine($"Process completed in {watch.ElapsedMilliseconds} ms.");
                OutputProgressText = sb.ToString();
            });
        }

        public async Task<Dictionary<string, string>> ExecuteProgramAsync(string[] urls, Dictionary<string, string> variables)
        {
            var channels = new GrpcChannel[urls.Length];
            var clients = new MathExecuter.MathExecuterClient[urls.Length];

            var httpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            var options = new GrpcChannelOptions { HttpHandler = httpHandler };

            for (int i = 0; i < urls.Length; i++)
            {
                channels[i] = GrpcChannel.ForAddress(urls[i], options);
                clients[i] = new MathExecuter.MathExecuterClient(channels[i]);
            }

            var t = _masterAssembly.GetType("MathExecutorMaster.Executor");

            dynamic instance = Activator.CreateInstance(t);

            var result = await (Task<Dictionary<string, string>>)instance.ExecuteAsync(variables, clients);

            foreach (var item in channels)
            {
                item.Dispose();
            }

            return result;
        }

        public async Task UploadMasterDll(Dispatcher dispatcher)
        {
            var sb = new StringBuilder();

            await dispatcher.InvokeAsync(() =>
            {
                sb.AppendLine("Master program upload started.");
                OutputProgressText = sb.ToString();
            });

            _masterAssembly = Assembly.LoadFrom(MasterDllPath);

            await dispatcher.InvokeAsync(() =>
            {
                sb.AppendLine("Program upload completed.");
                OutputProgressText = sb.ToString();
            });
        }

        public async Task UploadSlaveDllToNodes(Dispatcher dispatcher)
        {
            var sb = new StringBuilder();

            await dispatcher.InvokeAsync(() =>
            {
                sb.AppendLine("Slave program upload started.");
                OutputProgressText = sb.ToString();
            });


            var httpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            var slaveDllBytes = File.ReadAllBytes(SlaveDllPath);

            var request = new SendProgramRequest
            {
                ProgramBytecode = Google.Protobuf.ByteString.CopyFrom(slaveDllBytes)
            };

            var nodeTasks = new Task<SendProgramResponse>[NodeUrls.Count];

            for (int i = 0; i < nodeTasks.Length; i++)
            {
                nodeTasks[i] = UploadSlaveDllToNode(i, NodeUrls[i].Url, request, httpHandler);
            }

            var responses = await Task.WhenAll(nodeTasks);

            await dispatcher.InvokeAsync(() =>
            {
                foreach (var item in responses)
                {
                    sb.AppendLine($"Node {item.NodeIndex}: {item.SuccessMessage}");
                }
                sb.AppendLine("Program upload complete.");
                OutputProgressText = sb.ToString();
            });
        }

        private async Task<SendProgramResponse> UploadSlaveDllToNode(int index, string url, SendProgramRequest request, HttpClientHandler clientHandler)
        {
            using var channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions { HttpHandler = clientHandler });

            var client = new MathExecuter.MathExecuterClient(channel);

            return await client.SendProgramAsync(request);
        }
    }
}
