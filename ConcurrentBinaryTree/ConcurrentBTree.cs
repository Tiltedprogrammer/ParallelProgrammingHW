using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.Threading;

namespace ConcurrentBinaryTree
{
    public class ConcurrentBTree<K, V> where K : IComparable<K>
    {
        
        public readonly Node<K, V> root;

        public ConcurrentBTree(K min,K max)
        {
            
            Node<K,V> parent = new Node<K,V>(min);
            root = new Node<K,V>(max, null, parent, parent, parent);
            root.parent = parent;
            parent.right = root;
            parent.succ = root;
            
        }
        
        public V get(K key) {
           
            Node<K,V> node = root;
            Node<K,V> child;
            K val;
            int res = -1;
            while (true) {
                if (res == 0) break;
                if (res > 0) {
                    child = node.right;
                } else {
                    child = node.left;
                }
                if (child == null) break;
                node = child;
                val = node.key;
                res = key.CompareTo(val);
            }
            while (res < 0) {
                node = node.pred;
                val =  node.key;
                res = key.CompareTo(val);
            }
            while (res > 0) {
                node = node.succ;
                val =  node.key;
                res = key.CompareTo(val);
            } 
            if (res == 0 && node.mark) {
                return (V) node.value;
            }
            return default(V);
        }
        
        public V put(K key, V value) {
            return insert(key, value);
        }
        
        private V insert(K key, V value) {
            Node<K,V> node;
            K nodeValue;
            int res;
            while (true) {
                node = root;
                Node<K,V> child;
                res = -1;
                while (true) {
                    if (res == 0) break;
                    if (res > 0) {
                        child = node.right;
                    } else {
                        child = node.left;
                    }
                    if (child == null) break;
                    node = child;
                    nodeValue = node.key;
                    res = key.CompareTo(nodeValue);
                }
                Node<K,V> pred = res > 0 ? node : node.pred;
                pred.lockSuccLock();
                if (pred.mark) {
                    K predVal = pred.key;
                    int predRes = pred== node? res: key.CompareTo(predVal);
                    if (predRes > 0) {
                        Node<K,V> succ = pred.succ;
                        K succVal = succ.key;
                        int res2 = succ == node? res: key.CompareTo(succVal);
                        if (res2 <= 0) {
                            if (res2 == 0) {
                                pred.unlockSuccLock();
                                //Console.WriteLine("Key already exists");
                                return default(V);
                            }
                            Node<K,V> parent = chooseParent(pred, succ, node);
                            Node<K,V> newNode = new Node<K,V>(key, value, pred, succ, parent);
                            succ.pred = newNode;
                            pred.succ = newNode;
                            pred.unlockSuccLock();
                            insertToTree(parent, newNode, parent == pred);
                            return default(V);
                        }
                    }
                }
                pred.unlockSuccLock();
            }
        }
        
        
        private Node<K,V> chooseParent(Node<K,V> pred, 
        Node<K,V> succ, Node<K,V> firstCand) {
            Node<K,V> candidate = firstCand == pred || firstCand == succ? firstCand: pred;
            while (true) {
                candidate.lockTreeLock();
                if (candidate == pred) {
                    if (candidate.right == null) {
                        return candidate;
                    }
                    candidate.unlockTreeLock();
                    candidate = succ;
                } else {
                    if (candidate.left == null) {
                        return candidate;
                    }
                    candidate.unlockTreeLock();
                    candidate = pred;
                }
                Thread.Yield();
            }
        }
        
        private void insertToTree(Node<K,V> parent, Node<K,V> newNode,bool isRight) {
            
            if (isRight) {
                parent.right = newNode;
            } else {
                parent.left = newNode;
            }
            
             parent.unlockTreeLock();
            
        }
        
        private Node<K,V> lockParent(Node<K,V> node) {
            while (true)
            {
                var parent = node.parent;
                parent.lockTreeLock();
                if (node.parent == parent && parent.mark) return parent;
                parent.unlockTreeLock();
            }
        }

        public V remove(K key)
        {
            int res;
            K nodeKey;
            Node<K, V> p;
            
            while (true)
            {
                Node<K, V> node = root;
                Node<K, V> child;
                res = -1;
                while (true)
                {
                    if (res == 0) break;
                    if (res > 0)
                    {
                        child = node.right;
                    }
                    else
                    {
                        child = node.left;
                    }
                    if (child == null) break;
                    node = child;
                    nodeKey = node.key;
                    res = key.CompareTo(nodeKey);
                }

                p = res > 0 ? node : node.pred;
                p.lockSuccLock();
                Node<K, V> s = p.succ;

                if (p.key.CompareTo(key) < 0 && s.key.CompareTo(key) >= 0 && p.mark)
                {
                    if (s.key.CompareTo(key) > 0)
                    {
                        p.unlockSuccLock(); // key doesnt exist
                        return default(V);
                    }
                    
                    s.lockSuccLock();
                    bool hasTwoChildren = acquireTreeLocks(s);
                    var sParent = lockParent(s);
                    
                    //Update logical order

                    s.mark = false;
                    Node<K, V> sSucc = s.succ;
                    sSucc.pred = p;
                    p.succ = sSucc;
                    s.unlockSuccLock();
                    p.unlockSuccLock();
                    
                    //Physical remove
                    //lockParent(s);
                    removeFromTree(s,sParent,hasTwoChildren);
                    //s.parent.unlockTreeLock();
                    return (V)s.value;
                }
                
                p.unlockSuccLock();

            }
            
        }

        private bool acquireTreeLocks(Node<K, V> node)
        {
            
            while (true)
            {
                node.lockTreeLock();
                //var p = lockParent(node);
                
                Node<K, V> right = node.right;
                Node<K, V> left = node.left;
                
                if (right == null || left == null)
                {
                    if (right != null)
                    {
                        if(!right.tryLockTreeLock())
                        {
                           // p.unlockTreeLock();
                            node.unlockTreeLock();
                            Thread.Yield();
                            continue;
                            
                        }
                    }
                    else if (left != null)
                    {
                        if (!left.tryLockTreeLock())
                        {
                           // Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
                            //p.unlockTreeLock();
                            node.unlockTreeLock();
                            Thread.Yield();
                            continue;
                        }

                    }
                    return false;

                }

                Node<K, V> s = node.succ;
                if (s.parent != node)
                {
                    Node<K, V> parent = s.parent;
                    if (!parent.tryLockTreeLock())
                    {
                        //Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
                       // p.unlockTreeLock();
                        node.unlockTreeLock();
                        Thread.Yield();
                        continue;
                    }
                    if (parent != s.parent || !parent.mark)
                    {
                       // Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
                        parent.unlockTreeLock();
                        //p.unlockTreeLock();
                        node.unlockTreeLock();
                        Thread.Yield();
                        continue;
                    }

                }

                if (!s.tryLockTreeLock())
                {
                    //Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
                    node.unlockTreeLock();
                    if(s.parent != node)s.parent.unlockTreeLock();                                       
                    Thread.Yield();
                    continue;
                }

                if (s.right != null) 
                {
                    
                    if (!s.right.tryLockTreeLock())
                    {
                        //Console.WriteLine(Thread.CurrentThread.ManagedThreadId);
                        node.unlockTreeLock();
                        s.unlockTreeLock();
                        if(s.parent != node) s.parent.unlockTreeLock();
                      //  p.unlockTreeLock();
                        Thread.Yield();
                        continue;
                    }
                }
                    
                return true;


            }
        }

        private void removeFromTree(Node<K, V> node, Node<K, V> parent, bool hasTwoChildren)
        {
            Node<K, V> child;
            if (!hasTwoChildren)
            {

                child = node.right == null ? node.left : node.right;
                //parent = node.parent;
                updateChild(parent, node, child);
                if(child != null)child.unlockTreeLock();
                parent.unlockTreeLock();
                node.unlockTreeLock();
                return;

            }
            else
            {
                //Node<K,V> parent = node.parent;
                Node<K, V> succ = node.succ;
                child = succ.right;
                Node<K,V> oldParent = succ.parent;
                updateChild(oldParent, succ, child);
                succ.left = node.left;
                succ.right = node.right;
                node.left.parent = succ;

                if (node.right != null)
                {
                    node.right.parent = succ;
                }
                updateChild(parent, node, succ);
                if (child != null)
                {
                    child.unlockTreeLock();
                }
                //succ.unlockTreeLock();
                if (oldParent == node)
                {
                    oldParent = succ;
                }
                else
                {
                    succ.unlockTreeLock();
                }
                oldParent.unlockTreeLock();
                parent.unlockTreeLock();
                node.unlockTreeLock();
                    
                
            }
           // node.parent.unlockTreeLock();
            


        }
       
        
        private void updateChild(Node<K, V> parent, Node<K, V> oldChild,
            Node<K, V> newChild) {

            if (parent.left == oldChild)
            {
                parent.left = newChild;
            }
             else 
            {
                parent.right = newChild;
            }
            if (newChild != null) newChild.parent = parent;
        }


    }
}