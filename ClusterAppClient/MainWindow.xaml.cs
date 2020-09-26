using ClusterAppClient.ViewModels;
using ClusterWorkerNode;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Printing;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClusterAppClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private bool CheckHash(byte[] hash, byte maskBits)
        {
            for (int j = 0; j < hash.Length && maskBits != 0; j++)
            {
                var delta = maskBits > 8 ? (byte)8 : maskBits;

                var result = hash[j] & (byte)((1 << delta) - 1);

                if(result!=0)
                {
                    return false;
                }

                maskBits -= delta;
            }

            return true;
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            /*var watch = new System.Diagnostics.Stopwatch();

            watch.Start();

            FindExpectedHashWithVector();

            watch.Stop();

            MessageBox.Show($"ExecutionTime {watch.ElapsedMilliseconds}");*/

            
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var watch = new System.Diagnostics.Stopwatch();

            watch.Start();

            FindExpectedHash();

            watch.Stop();

            MessageBox.Show($"ExecutionTime {watch.ElapsedMilliseconds}");
        }

        private void FindExpectedHash()
        {
            using var mySHA256 = SHA256.Create();

            var hardLevel = (byte)30;

            var buffer = new byte[4];

            for (int i = 0; i < int.MaxValue; i++)
            {
                var maskBits = hardLevel;

                BitConverter.TryWriteBytes(buffer, i);

                var hash = mySHA256.ComputeHash(buffer);

                if (CheckHash(hash, hardLevel))
                {
                    TextBox1.Text = i.ToString();
                    return;
                }
            }

            TextBox1.Text = "I can't do it";
        }

        private void FindExpectedHashWithVector()
        {
            using var mySHA256 = SHA256.Create();

            Span<byte> maskBytes = stackalloc byte[32];

            var hardLevel = (byte)25;

            for (int j = 0; j < maskBytes.Length && hardLevel != 0; j++)
            {
                var delta = hardLevel > 8 ? (byte)8 : hardLevel;

                maskBytes[j] |= (byte)((1 << delta) - 1);

                hardLevel -= delta;
            }

            var maskVector = new Vector<byte>(maskBytes);

            var buffer = new byte[4];

            for (int i = 0; i < int.MaxValue; i++)
            {
                BitConverter.TryWriteBytes(buffer, i);

                var hash = mySHA256.ComputeHash(buffer);

                var hashVector = new Vector<byte>(hash);

                if ((hashVector & maskVector) == Vector<byte>.Zero)
                {
                    TextBox1.Text = i.ToString();
                    return;
                }
            }

            TextBox1.Text = "I can't do it";
        }

        private long LongRandom(long min, long max, Random rand)
        {
            long result = rand.Next((int)(min >> 32), (int)(max >> 32));
            result <<= 32;
            result |= (long)rand.Next((int)min, (int)max);
            return result;
        }

        private async void FindHashButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                await vm.FindHashAndNonceAsync(Dispatcher);
            }
        }

        private void AddNodeButton_Click(object sender, RoutedEventArgs e)
        {
            if(DataContext is MainWindowViewModel vm)
            {
                vm.AddNode();
            }
        }

        private void RemoveNodeButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.RemoveNode();
            }
        }
    }
}
