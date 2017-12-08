using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ConcurrentBinaryTree
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            Test3(30,14,20);

        }
        
        public static void Test1()
        {
            var Tree = new ConcurrentBTree<Int32,Int32>(Int32.MinValue,Int32.MaxValue);
            var thread1 = new Thread(()=> Tree.put(6,6));
            var thread2 = new Thread(()=> Tree.remove(6));
            thread1.Start();
            thread2.Start();
            thread1.Join();
            thread2.Join();
            Tree.root.printTree();
   
        }
        
        public static void Test2(int insert,int search,int remove)
        {
            var Tree = new ConcurrentBTree<Int32,Int32>(Int32.MinValue,Int32.MaxValue);
            var random = new Random();
            var watch = new Stopwatch();
            watch.Start();
            int a = random.Next(40, 10000000);
            for (int i = 0; i < insert; i++)
            {
                Tree.put(a,a);
                a = random.Next(40, 10000000);
            }
            for (int i = 0; i < remove; i++)
            {
                Tree.remove(a);
                a = random.Next(40, 10000000);
            }
            for (int i = 0; i < search; i++)
            {
                Tree.get(a);
                a = random.Next(40, 10000000);
            }
            watch.Stop();
            Console.WriteLine("Sequental 1000000 inserts, 500000 removes, 500000 searches  took {0}ms", watch.ElapsedMilliseconds);
        }

        private static void Test3(int insert,int seatch,int remove)
        {
            var Tree = new ConcurrentBTree<Int32,Int32>(Int32.MinValue,Int32.MaxValue);
            
            var TaskQueue = new Queue<Task>();
            
            var random = new Random();
            int a = random.Next(40, 10000000);
            for (int i = 0; i < insert; i++)
            {
                var tmpa = a;
                TaskQueue.Enqueue(new Task(()=> Tree.put(tmpa,tmpa)));
                a = random.Next(40, 10000000);
            }
            for (int i = 0; i < remove; i++)
            {
                var tmpa = a;
                TaskQueue.Enqueue(new Task(()=> Tree.remove(tmpa)));
                random.Next(40, 10000000);
            }
            for (int i = 0; i < seatch; i++)
            {
                var tmpa = a;
                TaskQueue.Enqueue(new Task(()=> Tree.get(tmpa)));
              
               a = random.Next(40, 10000000);
            }
            var watch = new Stopwatch();
            watch.Start();
            

            foreach (var VARIABLE in TaskQueue)
            {
                VARIABLE.Start();
            }
            
            foreach (var VARIABLE in TaskQueue)
            {
                VARIABLE.Wait();
            }
            
            watch.Stop();
            Console.WriteLine("Parallel 10000000 inserts, 5000000 removals, 5000000 searches took {0}", watch.ElapsedMilliseconds);
            if(insert < 50)Tree.root.printTree();
            

        }
    }
}