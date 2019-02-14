using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CorePerformance
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var worker = new Worker();
            worker.CreateFile();
            var result = worker.SimLongLookupWithCacheAsync().Result;

            Console.ReadKey();
        }

        public class Worker
        {
            private Dictionary<int, long> _alreadyProcessedCache = new Dictionary<int, long>();
            private int _cacheHits;
            private int _keyInCacheCount;
            private bool _printDetails = false;

            private List<int> CreateTestSet()
            {
                List<int> numbersToProcess = new List<int>();

                for (int a = 15; a > 2; a = a - 3)
                {
                    for (int b = 5000; b > 100; b = b - 10)
                    {
                        for (int c = 0; c < a; c++)
                        {
                            numbersToProcess.Add(b);
                        }
                    }
                }
                return numbersToProcess;
            }

            public async Task<long> SimLongLookupWithCacheAsync()
            {

                var numbersToProcess = CreateTestSet();

                long resultVerification1 = 0;
                long resultVerification2 = 0;
                DateTime start = DateTime.Now;

                numbersToProcess.ForEach(num =>
                {
                    resultVerification1 = resultVerification1 + LookupNumber(num).Result;
                });

                Console.WriteLine(resultVerification1 + " in " + DateTime.Now.Subtract(start).TotalMilliseconds + " with " + _cacheHits + " cache hits. "
                    + _keyInCacheCount + " cache key adds rejected");

                _alreadyProcessedCache.Clear();
                _cacheHits = 0;
                _keyInCacheCount = 0;
                start = DateTime.Now;

                numbersToProcess.ForEach(async num =>
                {
                    resultVerification2 = resultVerification2 + await LookupNumber(num);
                });

                Console.WriteLine(resultVerification2 + " in " + DateTime.Now.Subtract(start).TotalMilliseconds + " with " + _cacheHits + " cache hits. "
                    + _keyInCacheCount + " cache key adds rejected");

                return resultVerification1 - resultVerification2;

            }

            private async Task<long> LookupNumber(int n)
            {
                try
                {
                    if (_alreadyProcessedCache.ContainsKey(n))
                    {
                        _cacheHits++;
                        return _alreadyProcessedCache[n];
                    }
                    else
                    {
                        var result = await FindInFileAsync(n);

                        if (!_alreadyProcessedCache.ContainsKey(n))
                        {
                            _alreadyProcessedCache.Add(n, result);
                        }
                        else
                        {
                            _keyInCacheCount++;//shouldn't really ever get here
                        }
                        return result;
                    }
                }
                catch (Exception any)
                {
                    Console.WriteLine(any.Message);
                    return 0;
                }
            }

            private async Task<long> FindInFileAsync(int n)
            {
                var inFile = new FileStream("Data.bin", FileMode.Open, FileAccess.Read);

                byte[] intBuffer = new byte[4];
                byte[] longBuffer = new byte[8];

                if (await inFile.ReadAsync(intBuffer, 0, 4) != 4) throw new Exception("somethings wrong");
                if (await inFile.ReadAsync(longBuffer, 0, 8) != 8) throw new Exception("somethings wrong");
                int key = BitConverter.ToInt32(intBuffer, 0);
                long val = BitConverter.ToInt64(longBuffer, 0);
                while (key != n)
                {
                    if (await inFile.ReadAsync(intBuffer, 0, 4) != 4) throw new Exception("somethings wrong");
                    if (await inFile.ReadAsync(longBuffer, 0, 8) != 8) throw new Exception("somethings wrong");
                    key = BitConverter.ToInt32(intBuffer, 0);
                    val = BitConverter.ToInt64(longBuffer, 0);
                }
                inFile.Close();
                return val;
            }

            //just some process intensive thing from the internet
            public async Task<long> FindPrimeNumber(int n)
            {
                if (_printDetails)
                {
                    Console.WriteLine("FindPrimeNumber " + n);
                }
                int count = 0;
                long a = 2;
                while (count < n)
                {
                    long b = 2;
                    int prime = 1;// to check if found a prime
                    while (b * b <= a)
                    {
                        if (a % b == 0)
                        {
                            prime = 0;
                            break;
                        }
                        b++;
                    }
                    if (prime > 0)
                    {
                        count++;
                    }
                    a++;
                }
                return (--a);
            }

            internal void CreateFile()
            {
                var distinceNumbers = CreateTestSet().Distinct().Reverse().ToList();
                var outFile = new FileStream("Data.bin", FileMode.Create);
                distinceNumbers.ForEach(num =>
                {
                    var intByteArray = BitConverter.GetBytes(num);
                    outFile.Write(intByteArray, 0, intByteArray.Length);
                    var longByteArray = BitConverter.GetBytes(FindPrimeNumber(num).Result);
                    outFile.Write(longByteArray, 0, longByteArray.Length);
                });
                outFile.Close();
                Console.WriteLine(distinceNumbers.Count + " keys written to " + outFile.Name);
            }
        }
    }
}
