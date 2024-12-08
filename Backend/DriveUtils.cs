using System.Diagnostics;

namespace Backend
{
    internal class DriveUtils
    {

        public static double RunDriveBench(string root, int threads)
        {
            //1. Write a 10mb file
            Stopwatch s = Stopwatch.StartNew();

            try
            {
                File.Delete($"{root}/bench.file");
            }
            catch { }

            int iterations = 1024 * 4;

            int[] positions = new int[iterations];

            for (int i = 0; i < iterations; i++)
            {
                positions[i] = i;
            }
            Random.Shared.Shuffle(positions);


            using var f = File.OpenWrite($"{root}/bench.file");
            int step = 4096 * 2;

            for (int i = 0; i < iterations; i++)
            {
                Random r = new Random(i);
                byte[] b = new byte[step];
                r.NextBytes(b);
                f.Write(b);
            }

            f.Close();


            int counter = 0;
            Parallel.For(0, iterations, new ParallelOptions() { MaxDegreeOfParallelism = threads }, (i) =>
            {
                // get the random position for our thread
                int ii = positions[i];
                int pos = ii * step;

                //read the file into a bugger
                byte[] buffer = new byte[step];
                using var file = File.OpenRead($"{root}/bench.file");
                file.Position = pos;
                file.ReadExactly(buffer);

                //now validate the contents
                Random r = new Random(i);
                byte[] check = new byte[step];
                r.NextBytes(check);

                for (int j = 0; j < check.Length; j++)
                {
                    if (check[j] != buffer[j])
                    {
                        Interlocked.Increment(ref counter);
                    }
                }
            });

            return s.Elapsed.TotalMilliseconds;

        }
    }
}
