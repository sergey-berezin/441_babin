using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
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
using System.Threading;
using Ookii.Dialogs.Wpf;
using ArcFaceLib;

namespace wpfapp
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

        private VistaFolderBrowserDialog dirChoosingDialog = new();
        private string[] dirPathes = new string[2];    
        private Face arcfacenet = new Face();
        private CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
        private void RenderImages(string dirPath, ListBox list)
        {
            list.Items.Clear(); 
            
            var jpg = Directory.GetFiles(dirPath).Where(p => p.EndsWith(".jpg"));
            var jpeg = Directory.GetFiles(dirPath).Where(p => p.EndsWith(".jpg"));
            var png = Directory.GetFiles(dirPath).Where(p => p.EndsWith(".png"));

            var allImages = png.Concat(jpg.Concat(jpeg));

            foreach (var p in allImages) 
            {
                StackPanel pair = new StackPanel();
                pair.Orientation = Orientation.Horizontal;

                Image image = new()
                {
                    Source = new BitmapImage(new Uri(p)),
                    Width = 112
                };
                
                pair.Children.Add(image);
                TextBlock path = new TextBlock();
                path.Text = p;
                path.Visibility = Visibility.Collapsed;
                pair.Children.Add(path);
                list.Items.Add(pair);
            }
        }

        private void DirButtonClick(object sender, RoutedEventArgs e)
        {
            Button source = (Button)e.Source;
            dirChoosingDialog.ShowDialog();

            switch (source.Name)
            {
                case "FirstDirButton":
                    dirPathes[0] = dirChoosingDialog.SelectedPath;
                    RenderImages(dirPathes[0], ListBox1);
                    break;
                case "SecondDirButton":
                    dirPathes[1] = dirChoosingDialog.SelectedPath;
                    RenderImages(dirPathes[1], ListBox2);
                    break;
            }
        }

        private void CancelButtonClick(object sender, RoutedEventArgs e) 
        {
            if(cancelTokenSource != null)
            {
                cancelTokenSource.Cancel();
            }
            ProgressBar.Value = 0;
        }

        private async void CompareClick(object sender, RoutedEventArgs e) 
        {
            ProgressBar.Value = 0;
            CompareButton.IsEnabled = false;
            CancelButton.IsEnabled = true;
            cancelTokenSource = new CancellationTokenSource();
            CancellationToken token = cancelTokenSource.Token;

            var path1 = ((StackPanel)ListBox1.SelectedItem).Children.OfType<TextBlock>().Last().Text;
            var path2 = ((StackPanel)ListBox2.SelectedItem).Children.OfType<TextBlock>().Last().Text;
            var img1 = File.ReadAllBytes(path1);
            var img2 = File.ReadAllBytes(path2);
            Tuple <byte[], byte[]> pair = Tuple.Create(img1, img2);
            ProgressBar.Value += 50;

            var task = Task.Run(() => arcfacenet.CompareAsync(pair, token));
            await task;

            if (token.IsCancellationRequested)
            {
                ProgressBar.Value = 0;
                DistanceBlock.Text = "Distance";
                SimilarityBlock.Text = "Similarity";
            }
            else
            {
                ProgressBar.Value += 50;
                var distance = task.Result.Item1;
                var similarity = task.Result.Item2;
                DistanceBlock.Text = "Distance: " + distance.ToString();
                SimilarityBlock.Text = "Similarity: " + similarity.ToString();
            }
            CompareButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
        }
        private void ListChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListBox1.SelectedItems.Count != 0 && ListBox2.SelectedItems.Count != 0)
            {
                CompareButton.IsEnabled = true;
            }
        }
    }
}