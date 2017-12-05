using System;
using System.Collections.Generic;
using System.Threading;

namespace ConcurrentBinaryTree
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var Tree = new ConcurrentBTree<Int32,Int32>(Int32.MinValue,Int32.MaxValue);
            Thread []threads = new Thread[25];
            for (int i = 0; i < 25; i++)
            {
                var k = i;
                threads[i] = new Thread(()=>Tree.put(k,k));
            }
            foreach (var VARIABLE in threads)
            {
                VARIABLE.Start();
            }
            
            foreach (var VARIABLE in threads)
            {
                VARIABLE.Join();
            }
           
            Tree.root.left.printTree();
            //Console.WriteLine(Tree.get(4));

        }
    }
}