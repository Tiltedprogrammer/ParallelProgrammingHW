using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Data.SqlTypes;
using System.Security.Cryptography;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace ConsoleApplication
{
    internal class PrimeNumbers
    {
        public static void Main(string[] args)
        {
            int[] counts = new int[] {1000, 10000, 100000, 1000000, 10000000};
           foreach (var n in counts)
            {
                TimeTest(n);
            }     
        }
        static void TimeTest(int n) {
            Stopwatch stopWatch = new Stopwatch();
            
            stopWatch.Start();
            ParallelPrimes.ThreadedSearch(n);
            stopWatch.Stop();
            Console.WriteLine("Parallel prime search for {0} values took {1} ms.",
                n, stopWatch.ElapsedMilliseconds);
            
            stopWatch.Restart();
            ParallelPrimes.SequentalPrimes(n);
            stopWatch.Stop();
            Console.WriteLine("Sequental prime search for {0} values took {1} ms.",
                n, stopWatch.ElapsedMilliseconds);
            stopWatch.Restart();
            ParallelPrimes.ThreadPoolPrimes(n);
            stopWatch.Stop();
            Console.WriteLine("Threadpool prime search for {0} values took {1} ms.",
                n, stopWatch.ElapsedMilliseconds);
            stopWatch.Restart();
            ParallelPrimes.TaskPrimes(n);
            stopWatch.Stop();
            Console.WriteLine("Task prime search for {0} values took {1} ms.",
                n, stopWatch.ElapsedMilliseconds);
            
            Console.WriteLine();
        }
    }

    public static class EuratosphenesSieve
    {
        public static IEnumerable<int> SieveOfErathostenes(int upperLimit)
        {
            //BitArray works just like a bool[] but takes up a lot less space.
            BitArray composite = new BitArray(upperLimit);

            
            int sqrt= (int)Math.Sqrt(upperLimit);
            for (int p = 2; p < sqrt; ++p) {
                if (composite[p]) continue; 

                yield return p; 

               
                for (int i = p * p; i < upperLimit; i += p)
                    composite[i] = true;
            }
            for (int p = sqrt; p < upperLimit; ++p) {
                if (!composite[p]) yield return p;
            }
        }

       }
    public class SegmentedSieve{
        const int L1D_CACHE_SIZE = 32768;

       static public IList<int> segmented_sieve(int limit)
        {
            var res = new List<int>();
            int sqrt = (int) Math.Sqrt(limit);
            int segment_size = Math.Max(sqrt, L1D_CACHE_SIZE);

            int s = 3;
            int n = 3;

            
            var is_prime = new BitArray(sqrt + 1);
            is_prime.SetAll(true);
            for (int i = 2; i * i <= sqrt; i++)
            {
                if (is_prime[i])
                    for (int j = i * i; j <= sqrt; j += i)
                    { 
                        is_prime[j] = false;  
                    }
            }
            
            var sieve = new BitArray(segment_size);
            var primes = new List<int>();
            var next = new List<int>();

            for (int low = 0; low <= limit; low += segment_size)
            {
                sieve.SetAll(true);

                
                int high = Math.Min(low + segment_size - 1, limit);

                
                for (; s * s <= high; s += 2)
                {
                    if (is_prime[s])
                    {
                        primes.Add(s);
                        next.Add(s * s - low);
                    }
                }

                
                for (var i = 0; i < primes.Count; i++)
                {
                    int j = next[i];
                    for (int k = primes[i] * 2; j < segment_size; j += k)
                    {
                        sieve[j] = false;
                    }
                    next[i] = j - segment_size;
                }

                for (; n <= high; n += 2)
                {
                    if (sieve[n - low]) // n is a prime
                        res.Add(n);
                }
            }


            return res;
        }

    }

    public static class ParallelPrimes
    {
        public static bool isPrime(int n)
        {
            if (n % 2 == 0 && n != 2 || n == 1) return false;
            for (int i = 3; i <= Math.Round(Math.Sqrt(n)); i +=2)
            {
                if (n % i == 0) return false;
            }
            return true;
        }

        public static List<int> checkRange(int start, int end)
        {
            //if we lock the list for concatination threads are queued and executed randomly most of the time so we get
            //not ordered list of primes
                var primes = new List<int>();
                for (int i = start; i <= end; i++)
                {
                    if (isPrime(i)) primes.Add(i);
                }
            return primes;
        }

        public static List<int> ThreadedSearch(int range)
        {
            var primes = new List<int>();
            int th_count = 8;
            int start = 1;
            int i = 0;
            int segment_size_divisor = 1;
            int end; 
            Thread[] threads = new Thread[th_count];
            var res = new List<int>[th_count]; 
            for (; i < th_count - 1; i++)
                {
                    segment_size_divisor *= 2;
                    end = range - range / segment_size_divisor;
                    var start1 = start;
                    var end1 = end;
                    var indx = i;
                    threads[i] = new Thread(() => res[indx] = checkRange(start1, end1));
                    threads[i].Start();
                    start = end + 1;
                }
            threads[th_count - 1] = new Thread(() => res[th_count - 1] = checkRange(start,range));
            threads[th_count - 1].Start();
            foreach (var thread in threads)
            {
                thread.Join();
            }
            for(int j=0; j < th_count;j++)
            {
                 primes.AddRange(res[j]);
            }
            return primes;
        }
        public static List<int> SequentalPrimes(int range)
        {
            var primes = new List<int>();
            for (int i = 1; i <= range; i++)
            {
                if(isPrime(i))primes.Add(i);
            }
            return primes;
        }

        public static List<int> ThreadPoolPrimes(int range)
        {
            Func<int,int,List<int>> checkRangeRef = checkRange;
            var primes = new List<int>();
            int start = 1;
            int i = 0;
            int segment_size_divisor = 1;
            int end; 
            int interval_count = 8;
            var res = new List<int>[interval_count];
            IAsyncResult[] tmpres = new IAsyncResult[interval_count];
            for (; i < interval_count - 1; i++)
            {
                segment_size_divisor *= 2;
                end = range - range / segment_size_divisor;
                var start1 = start;
                var end1 = end;
                tmpres[i] = checkRangeRef.BeginInvoke(start1, end1, null, null);
                start = end + 1;
            }
            tmpres[interval_count - 1] = checkRangeRef.BeginInvoke(start, range, null, null);
            for (int j = 0; j < interval_count; j++)
            {
                res[j] = checkRangeRef.EndInvoke (tmpres[j]);
            }
            for(int j=0; j < interval_count;j++)
            {
                primes.AddRange(res[j]);
            }
            return primes;
        }

        public static List<int> TaskPrimes(int range)
        {
            var primes = new List<int>();
            int start = 1;
            int end = range;
            int median = range / 2;
            Task<List<int>> task1 = Task.Factory.StartNew<List<int>>(
                () => checkRange(start, median));
            Task<List<int>> task2 = Task.Factory.StartNew<List<int>>(
                () => checkRange(median + 1,end));
            primes.AddRange(task1.Result);
            primes.AddRange(task2.Result);
            return primes;

        }
    }

}

  
