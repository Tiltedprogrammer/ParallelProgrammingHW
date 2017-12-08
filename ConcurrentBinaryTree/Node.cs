using System;
using System.Threading;

namespace ConcurrentBinaryTree
{
    public class Node<K, V> where K : IComparable<K>
        {
            public readonly K key;

            public volatile object  value;

            //deletion flag if false then node is removed
            public volatile bool mark;

            /** The predecessor of the node (with respect to the ordering layout). */
            public volatile Node<K, V> pred;

            /** The successor of the node (with respect to the ordering layout). */
            public volatile Node<K, V> succ;

            /** The lock that protects the node's succ field and the pred field of the node pointed by succ. */
            public object succLock;

            /** The parent of the node (with respect to the tree layout). */
            public volatile Node<K, V> parent;
            
            public volatile Node<K, V> left;
            
            public volatile Node<K, V> right;

            /** The lock that protects the node's parent field and the right/left fields of the node. */
            public object treeLock;

            public Node(K key, object value, Node<K, V> pred, Node<K, V> succ, Node<K, V> parent)
            {
                this.key = key;
                this.value = value;
                mark = true;

                this.pred = pred;
                this.succ = succ;
                succLock = new object();

                this.parent = parent;
                right = null;
                left = null;
                treeLock = new object();

            }

            public Node(K key)
            {
                this.key = key;
                value = null;
                mark = true;

                pred = null;
                succ = null;
                succLock = new object();

                parent = null;
                right = null;
                left = null;
                treeLock = new object();


            }


            public void lockTreeLock()
            {
                Monitor.Enter(treeLock);
            }

            /**
             * Attempt to lock the node's treeLock without blocking.
             * 
             * return true if the lock was acquired, and false otherwise
             */
            public bool tryLockTreeLock()
            {
                return Monitor.TryEnter(treeLock);
            }

            /**
             * Release the node's treeLock.
             */
            public void unlockTreeLock()
            {
                Monitor.Exit(treeLock);
            }

            public void lockSuccLock()
            {
                Monitor.Enter(succLock);
            }

            /**
             * Release the node's succLock.
             */
            public void unlockSuccLock()
            {
                Monitor.Exit(succLock);
            }

            public void printTree()
            {
                if (right != null)
                {
                    right.printTree(true, "");
                }
                printNodeValue();
                if (left != null)
                {
                    left.printTree(false, "");
                }
            }

            private void printNodeValue()
            {
                if (value == null)
                {
                    Console.Write("<null>");
                }
                else
                {
                    Console.Write(value);
                }
                Console.Write('\n');
            }


            private void printTree(bool isRight, string indent)
            {
                if (right != null)
                {
                    right.printTree(true, indent + (isRight ? "        " : " |      "));
                }
                Console.Write(indent);
                if (isRight)
                {
                    Console.Write(" /");
                }
                else
                {
                    Console.Write(" \\");
                }
                Console.Write("----- ");
                printNodeValue();
                if (left != null)
                {
                    left.printTree(false, indent + (isRight ? " |      " : "        "));
                }
            }
        }
}