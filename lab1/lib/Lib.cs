using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace ArcFaceLib {
    public class Face
    {   
        private InferenceSession session;
        private static Mutex mtx = new();
        private static float Length(float[] v) => (float)Math.Sqrt(v.Select(x => x*x).Sum());
        private static float Distance(float[] v1, float[] v2) => Length(v1.Zip(v2).Select(p => p.First - p.Second).ToArray());
        private static float Similarity(float[] v1, float[] v2) => v1.Zip(v2).Select(p => p.First * p.Second).Sum();
        private float[] Normalize(float[] v) 
        {
            var len = Length(v);
            return v.Select(x => x / len).ToArray();
        }
        private DenseTensor<float> ImageToTensor(Image<Rgb24> img)
        {
            var w = img.Width;
            var h = img.Height;
            var t = new DenseTensor<float>(new[] { 1, 3, h, w });

            img.ProcessPixelRows(pa => 
            {
                for (int y = 0; y < h; y++)
                {           
                    Span<Rgb24> pixelSpan = pa.GetRowSpan(y);
                    for (int x = 0; x < w; x++)
                    {
                        t[0, 0, y, x] = pixelSpan[x].R;
                        t[0, 1, y, x] = pixelSpan[x].G;
                        t[0, 2, y, x] = pixelSpan[x].B;
                    }
                }
            });

            return t;
        }

        public Tuple<float, float> Compare(Tuple<byte[], byte[]> img){
            var face1 = Image.Load<Rgb24>(img.Item1);
            var face2 = Image.Load<Rgb24>(img.Item2);
            var embeddings1 = GetEmbeddings(face1);
            var embeddings2 = GetEmbeddings(face2);
            return Tuple.Create(Distance(embeddings1, embeddings2), Similarity(embeddings1, embeddings2));
        }

        public async Task<Tuple<float, float>> CompareAsync(Tuple<byte[], byte[]> img, CancellationToken token) 
        {
            var stream1 = new MemoryStream(img.Item1);
            var task1 = Image.LoadAsync<Rgb24>(stream1, token);
            var stream2 = new MemoryStream(img.Item2);
            var task2 = Image.LoadAsync<Rgb24>(stream2, token);
            await Task.WhenAll(task1, task2);
            if (token.IsCancellationRequested) 
            {
                return Tuple.Create<float, float>(0, 0);
            }

            var face1 = task1.Result;
            var face2 = task2.Result;        
            var embeddingTask1 = Task.Run(() => GetEmbeddingsAsync(face1, token));
            var embeddingTask2 = Task.Run(() => GetEmbeddingsAsync(face2, token));
            await Task.WhenAll(embeddingTask1, embeddingTask2);
            if (token.IsCancellationRequested)
            {
                return Tuple.Create<float, float>(0,0);
            }
            float[] embeddings1 = embeddingTask1.Result;
            float[] embeddings2 = embeddingTask2.Result;
            return Tuple.Create(Distance(embeddings1, embeddings2), Similarity(embeddings1, embeddings2));
        }

        private float[] GetEmbeddings(Image<Rgb24> face) 
        {
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("data", ImageToTensor(face)) };
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);
            return Normalize(results.First(v => v.Name == "fc1").AsEnumerable<float>().ToArray());
        }

        private async Task<float[]> GetEmbeddingsAsync(Image<Rgb24> face, CancellationToken token) 
        {   
            var inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("data", ImageToTensor(face)) };
            var data = new float[0];
            await Task.Factory.StartNew(() =>
            {
                mtx.WaitOne();
                using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results = session.Run(inputs);
                mtx.ReleaseMutex();
                data = Normalize(results.First(v => v.Name == "fc1").AsEnumerable<float>().ToArray());
            }, TaskCreationOptions.LongRunning);
            return data;
        }

        public Face() 
        {
            session = new InferenceSession("lib/arcfaceresnet100-8.onnx");  
        }
    }
}