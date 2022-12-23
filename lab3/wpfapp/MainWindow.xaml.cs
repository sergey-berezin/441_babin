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
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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
        private object locker = new object();
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

                System.Windows.Controls.Image image = new()
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

        private int RenderImagesFromDb(ListBox list)
        {
            lock(locker)
            {
                list.Items.Clear();
                using var db = new ImageContext();
                var count = 0;
                foreach (var i in db.Images)
                {   
                    StackPanel pair = new StackPanel();
                    pair.Orientation = Orientation.Horizontal;

                    System.Windows.Controls.Image image = new()
                    {
                        Source = new BitmapImage(new Uri(i.path)),
                        Width = 112
                    };

                    pair.Children.Add(image);
                    TextBlock path = new TextBlock();
                    path.Text = i.path;
                    path.Visibility = Visibility.Hidden;
                    pair.Children.Add(path);
                    list.Items.Add(pair);
                    count += 1;
                }
                return count;
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

        private void ShowDbButtonClick(object sender, RoutedEventArgs e)
        {
            var count = RenderImagesFromDb(ListBox3);
            if (count > 0)
            {
                DeleteFromDbButton.IsEnabled = true;
            }
        }

        public void DeleteImageFromDb()
        {
            lock(locker)
            {
                var selectedItem = (StackPanel)ListBox3.SelectedItem;

                if (selectedItem != null)
                {
                    using var db = new ImageContext();
                    var items = db.Images.Where(image =>
                    image.path == selectedItem.Children.OfType<TextBlock>().Last().Text);
                    var item = items.FirstOrDefault();
                    if (item != null)
                    {
                        db.Images.Remove(item);
                        db.SaveChanges();
                        ListBox3.Items.Remove(selectedItem);
                    }
                }
            }
        }

        private void DeleteFromDbButtonClick(object sender, RoutedEventArgs e)
        {
            DeleteImageFromDb();
            var count = RenderImagesFromDb(ListBox3);
            if (count <= 0)
            {
                DeleteFromDbButton.IsEnabled = false;
            }
        }

        private void AddImageToDb(string hash, string path, byte[] imageBin, string embedding)
        {
            lock(locker)
            {
                using var db = new ImageContext();
                var count = db.Images.Count(image =>
                image.path == path);
                if (count == 0)
                {
                    ImageDB image = new()
                    {
                        hash = hash,
                        path = path,
                        image = imageBin,
                        embedding = embedding
                    };
                    db.Add(image);
                    db.SaveChanges();
                }
            }
        }

        private void AddToDbButtonClick(object sender, RoutedEventArgs e)
        {   
            AddToDbButton.IsEnabled = false;
            if (ListBox1.SelectedItems.Count != 0) 
            {
                var path = ((StackPanel)ListBox1.SelectedItem).Children.OfType<TextBlock>().Last().Text;
                byte[] imageBytes = File.ReadAllBytes(path);
                var hash = Convert.ToBase64String(imageBytes);
                //var hash = imageBytes.GetHashCode().ToString();
                var embedding = arcfacenet.GetEmbeddingsByPath(path);
                AddImageToDb(hash, path, imageBytes, String.Join(" ", embedding));
                //RenderImagesFromDb(ListBox3);
            }
            if (ListBox2.SelectedItems.Count != 0) 
            {
                var path = ((StackPanel)ListBox2.SelectedItem).Children.OfType<TextBlock>().Last().Text;
                byte[] imageBytes = File.ReadAllBytes(path);
                var hash = Convert.ToBase64String(imageBytes);
                //var hash = imageBytes.GetHashCode().ToString();
                var embedding = arcfacenet.GetEmbeddingsByPath(path);
                AddImageToDb(hash, path, imageBytes, String.Join(" ", embedding));
                //RenderImagesFromDb(ListBox3);
            }
            AddToDbButton.IsEnabled = true;
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
            var hash1 = Convert.ToBase64String(img1);
            var hash2 = Convert.ToBase64String(img2);
            // var hash1 = img1.GetHashCode().ToString();
            // var hash2 = img2.GetHashCode().ToString();
            Tuple <byte[], byte[]> pair = Tuple.Create(img1, img2);
            ProgressBar.Value += 50;

            // search proper images in db
            bool[] loadedFromDbFlags = new bool[2]{false, false};
            using var db = new ImageContext();
            var img1fromDb = db.Images.Where(item => item.hash == hash1).Where(item => Enumerable.SequenceEqual(item.image, img1)).SingleOrDefault();
            var img2fromDb = db.Images.Where(item => item.hash == hash2).Where(item => Enumerable.SequenceEqual(item.image, img2)).SingleOrDefault();

            float[]? embedding1 = null;
            float[]? embedding2 = null;

            if (img1fromDb != null)
            {   
                loadedFromDbFlags[0] = true;
                List<float> embeddingList = new();
                Array.ForEach(img1fromDb.embedding.Split(' '), i => { embeddingList.Add(float.Parse(i)); });
                embedding1 = embeddingList.ToArray();
            }
            else
            {
                embedding1 = arcfacenet.GetEmbeddingsByPath(path1);
            }

            if (img2fromDb != null)
            {   
                loadedFromDbFlags[1] = true;
                List<float> embeddingList = new();
                Array.ForEach(img2fromDb.embedding.Split(' '), i => { embeddingList.Add(float.Parse(i)); });
                embedding2 = embeddingList.ToArray();
            }
            else
            {
                embedding2 = arcfacenet.GetEmbeddingsByPath(path2);
            }

            Tuple<float, float> results = arcfacenet.CompareByEmbeddings(embedding1, embedding2);

            // var task = Task.Run(() => arcfacenet.CompareAsync(pair, token));
            // await task;

            if (token.IsCancellationRequested)
            {
                ProgressBar.Value = 0;
                DistanceBlock.Text = "Distance";
                SimilarityBlock.Text = "Similarity";
            }
            else
            {
                ProgressBar.Value += 50;
                //var distance = task.Result.Item1;
                //var similarity = task.Result.Item2;
                var distance = results.Item1;
                var similarity = results.Item2;
                DistanceBlock.Text = "Distance: " + distance.ToString();
                //SimilarityBlock.Text = "Similarity: " + similarity.ToString();
                SimilarityBlock.Text = "Similarity: " + similarity.ToString() + "\n"
                + loadedFromDbFlags[0].ToString() + " " + loadedFromDbFlags[1];
            }
            CompareButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
        }

        // private async void CompareClick(object sender, RoutedEventArgs e) 
        // {
        //     ProgressBar.Value = 0;
        //     CompareButton.IsEnabled = false;
        //     CancelButton.IsEnabled = true;
        //     cancelTokenSource = new CancellationTokenSource();
        //     CancellationToken token = cancelTokenSource.Token;

        //     var path1 = ((StackPanel)ListBox1.SelectedItem).Children.OfType<TextBlock>().Last().Text;
        //     var path2 = ((StackPanel)ListBox2.SelectedItem).Children.OfType<TextBlock>().Last().Text;
        //     var img1 = File.ReadAllBytes(path1);
        //     var img2 = File.ReadAllBytes(path2);
        //     Tuple <byte[], byte[]> pair = Tuple.Create(img1, img2);
        //     ProgressBar.Value += 50;

        //     var task = Task.Run(() => arcfacenet.CompareAsync(pair, token));
        //     await task;

        //     if (token.IsCancellationRequested)
        //     {
        //         ProgressBar.Value = 0;
        //         DistanceBlock.Text = "Distance";
        //         SimilarityBlock.Text = "Similarity";
        //     }
        //     else
        //     {
        //         ProgressBar.Value += 50;
        //         var distance = task.Result.Item1;
        //         var similarity = task.Result.Item2;
        //         DistanceBlock.Text = "Distance: " + distance.ToString();
        //         SimilarityBlock.Text = "Similarity: " + similarity.ToString();
        //     }
        //     CompareButton.IsEnabled = true;
        //     CancelButton.IsEnabled = false;
        // }

        private void ListChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListBox1.SelectedItems.Count != 0 || ListBox2.SelectedItems.Count != 0)
            {
                AddToDbButton.IsEnabled = true;
            }

            if (ListBox1.SelectedItems.Count != 0 && ListBox2.SelectedItems.Count != 0)
            {
                CompareButton.IsEnabled = true;
            }
        }
    }
}