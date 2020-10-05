using ClusterWorkerNode;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
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

        public string SourceText { get; set; } = string.Empty;

        public int SelectedNodeIndex { get; set; }

        public ClusterNodePath SelectedNode { get; set; }

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

        public async Task FindHashAndNonceAsync(Dispatcher dispatcher)
        {
            var pool = new Task<MathDataResponse>[NodeUrls.Count];

            var sb = new StringBuilder();

            var random = new Random();

            

            await dispatcher.InvokeAsync(() =>
            {
                sb.AppendLine("Process started.");
                OutputProgressText = sb.ToString();
            });


            for (int i = 0; i < pool.Length; i++)
            {
                MathDataRequest request = new MathDataRequest()
                {
                    HardLevel = HardLevel,
                    Size = pool.Length,
                    Index = i,
                    Text = SourceText,
                };
                var url = NodeUrls[i].Url;
                pool[i] = Task.Run(() => ExecuteHashSearchAsync(url, request));
            }

            var result = await Task.WhenAny(pool);

            await dispatcher.InvokeAsync(() =>
            {
                sb.AppendLine("Result: ").AppendLine();

                sb.AppendLine($"Hash: {result.Result.Hash}").AppendLine($"Nonce: {result.Result.Nonce}").AppendLine();

                sb.AppendLine("Process completed.");
                OutputProgressText = sb.ToString();
            });
        }

        public async Task<MathDataResponse> ExecuteHashSearchAsync(string url, MathDataRequest request)
        {
            var httpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using var channel = GrpcChannel.ForAddress(url, new GrpcChannelOptions { HttpHandler = httpHandler });

            var client = new MathExecuter.MathExecuterClient(channel);

            return await client.ExecuteAsync(request);
        }
    }
}
