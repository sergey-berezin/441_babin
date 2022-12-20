using ArcFaceLib;

Console.WriteLine("Start");
// var img1 = File.ReadAllBytes("images/face1.png");
// var img2 = File.ReadAllBytes("images/face2.png");

Console.WriteLine("Load images from directory");
var img1 = File.ReadAllBytes("images/face1.jpg");
var img2 = File.ReadAllBytes("images/face2.jpg");
Tuple <byte[], byte[]> pair = Tuple.Create(img1, img2);
Console.WriteLine("Start model");

CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
CancellationToken token = cancelTokenSource.Token;
var f = new Face();
var task = Task.Run(() => f.CompareAsync(pair, token));
Console.WriteLine("Task started");
task.Wait();
Console.WriteLine("Task completed");

var distance = task.Result.Item1;
var similarity = task.Result.Item2;
Console.WriteLine($"Distance =  {distance}");
Console.WriteLine($"Similarity =  {similarity}");