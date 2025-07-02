using System;
using System.Collections.Generic;
using System.Linq;

namespace Ouro.StdLib.Collections
{
    /// <summary>
    /// Binary Tree Node
    /// </summary>
    public class BinaryTreeNode<T>
    {
        public T Value { get; set; }
        public BinaryTreeNode<T>? Left { get; set; }
        public BinaryTreeNode<T>? Right { get; set; }
        
        public BinaryTreeNode(T value)
        {
            Value = value;
        }
    }
    
    /// <summary>
    /// Binary Search Tree
    /// </summary>
    public class BinarySearchTree<T> where T : IComparable<T>
    {
        private BinaryTreeNode<T>? root;
        private int count;
        
        public int Count => count;
        public bool IsEmpty => count == 0;
        
        /// <summary>
        /// Insert a value into the tree
        /// </summary>
        public void Insert(T value)
        {
            root = InsertRecursive(root, value);
            count++;
        }
        
        private BinaryTreeNode<T> InsertRecursive(BinaryTreeNode<T>? node, T value)
        {
            if (node == null)
                return new BinaryTreeNode<T>(value);
                
            int comparison = value.CompareTo(node.Value);
            
            if (comparison < 0)
                node.Left = InsertRecursive(node.Left, value);
            else if (comparison > 0)
                node.Right = InsertRecursive(node.Right, value);
                
            return node;
        }
        
        /// <summary>
        /// Search for a value in the tree
        /// </summary>
        public bool Contains(T value)
        {
            return SearchRecursive(root, value);
        }
        
        private bool SearchRecursive(BinaryTreeNode<T>? node, T value)
        {
            if (node == null)
                return false;
                
            int comparison = value.CompareTo(node.Value);
            
            if (comparison == 0)
                return true;
            else if (comparison < 0)
                return SearchRecursive(node.Left, value);
            else
                return SearchRecursive(node.Right, value);
        }
        
        /// <summary>
        /// Remove a value from the tree
        /// </summary>
        public bool Remove(T value)
        {
            int initialCount = count;
            root = RemoveRecursive(root, value);
            return count < initialCount;
        }
        
        private BinaryTreeNode<T>? RemoveRecursive(BinaryTreeNode<T>? node, T value)
        {
            if (node == null)
                return null;
                
            int comparison = value.CompareTo(node.Value);
            
            if (comparison < 0)
            {
                node.Left = RemoveRecursive(node.Left, value);
            }
            else if (comparison > 0)
            {
                node.Right = RemoveRecursive(node.Right, value);
            }
            else
            {
                count--;
                
                if (node.Left == null)
                    return node.Right;
                if (node.Right == null)
                    return node.Left;
                    
                // Node with two children: get inorder successor
                node.Value = FindMin(node.Right).Value;
                node.Right = RemoveRecursive(node.Right, node.Value);
            }
            
            return node;
        }
        
        private BinaryTreeNode<T> FindMin(BinaryTreeNode<T> node)
        {
            while (node.Left != null)
                node = node.Left;
            return node;
        }
        
        /// <summary>
        /// Get values in order
        /// </summary>
        public IEnumerable<T> InOrder()
        {
            return InOrderTraversal(root);
        }
        
        private IEnumerable<T> InOrderTraversal(BinaryTreeNode<T>? node)
        {
            if (node != null)
            {
                foreach (var value in InOrderTraversal(node.Left))
                    yield return value;
                    
                yield return node.Value;
                
                foreach (var value in InOrderTraversal(node.Right))
                    yield return value;
            }
        }
        
        /// <summary>
        /// Get values in pre-order
        /// </summary>
        public IEnumerable<T> PreOrder()
        {
            return PreOrderTraversal(root);
        }
        
        private IEnumerable<T> PreOrderTraversal(BinaryTreeNode<T>? node)
        {
            if (node != null)
            {
                yield return node.Value;
                
                foreach (var value in PreOrderTraversal(node.Left))
                    yield return value;
                    
                foreach (var value in PreOrderTraversal(node.Right))
                    yield return value;
            }
        }
        
        /// <summary>
        /// Get values in post-order
        /// </summary>
        public IEnumerable<T> PostOrder()
        {
            return PostOrderTraversal(root);
        }
        
        private IEnumerable<T> PostOrderTraversal(BinaryTreeNode<T>? node)
        {
            if (node != null)
            {
                foreach (var value in PostOrderTraversal(node.Left))
                    yield return value;
                    
                foreach (var value in PostOrderTraversal(node.Right))
                    yield return value;
                    
                yield return node.Value;
            }
        }
        
        /// <summary>
        /// Get height of the tree
        /// </summary>
        public int Height()
        {
            return HeightRecursive(root);
        }
        
        private int HeightRecursive(BinaryTreeNode<T>? node)
        {
            if (node == null)
                return 0;
                
            return 1 + global::System.Math.Max(HeightRecursive(node.Left), HeightRecursive(node.Right));
        }
    }
    
    /// <summary>
    /// AVL Tree Node with balance factor
    /// </summary>
    public class AVLTreeNode<T> : BinaryTreeNode<T>
    {
        public int Height { get; set; }
        public new AVLTreeNode<T>? Left { get; set; }
        public new AVLTreeNode<T>? Right { get; set; }
        
        public AVLTreeNode(T value) : base(value)
        {
            Height = 1;
        }
    }
    
    /// <summary>
    /// Self-balancing AVL Tree
    /// </summary>
    public class AVLTree<T> where T : IComparable<T>
    {
        private AVLTreeNode<T>? root;
        private int count;
        
        public int Count => count;
        public bool IsEmpty => count == 0;
        
        /// <summary>
        /// Insert a value into the AVL tree
        /// </summary>
        public void Insert(T value)
        {
            root = InsertRecursive(root, value);
            count++;
        }
        
        private AVLTreeNode<T> InsertRecursive(AVLTreeNode<T>? node, T value)
        {
            // Normal BST insertion
            if (node == null)
                return new AVLTreeNode<T>(value);
                
            int comparison = value.CompareTo(node.Value);
            
            if (comparison < 0)
                node.Left = InsertRecursive(node.Left, value);
            else if (comparison > 0)
                node.Right = InsertRecursive(node.Right, value);
            else
                return node; // Duplicate values not allowed
                
            // Update height
            node.Height = 1 + global::System.Math.Max(GetHeight(node.Left), GetHeight(node.Right));
            
            // Get balance factor
            int balance = GetBalance(node);
            
            // Left Heavy
            if (balance > 1)
            {
                if (value.CompareTo(node.Left!.Value) < 0)
                {
                    // Left-Left case
                    return RotateRight(node);
                }
                else
                {
                    // Left-Right case
                    node.Left = RotateLeft(node.Left);
                    return RotateRight(node);
                }
            }
            
            // Right Heavy
            if (balance < -1)
            {
                if (value.CompareTo(node.Right!.Value) > 0)
                {
                    // Right-Right case
                    return RotateLeft(node);
                }
                else
                {
                    // Right-Left case
                    node.Right = RotateRight(node.Right);
                    return RotateLeft(node);
                }
            }
            
            return node;
        }
        
        private int GetHeight(AVLTreeNode<T>? node)
        {
            return node?.Height ?? 0;
        }
        
        private int GetBalance(AVLTreeNode<T>? node)
        {
            return node == null ? 0 : GetHeight(node.Left) - GetHeight(node.Right);
        }
        
        private AVLTreeNode<T> RotateRight(AVLTreeNode<T> y)
        {
            var x = y.Left!;
            var T2 = x.Right;
            
            // Perform rotation
            x.Right = y;
            y.Left = T2;
            
            // Update heights
            y.Height = global::System.Math.Max(GetHeight(y.Left), GetHeight(y.Right)) + 1;
            x.Height = global::System.Math.Max(GetHeight(x.Left), GetHeight(x.Right)) + 1;
            
            return x;
        }
        
        private AVLTreeNode<T> RotateLeft(AVLTreeNode<T> x)
        {
            var y = x.Right!;
            var T2 = y.Left;
            
            // Perform rotation
            y.Left = x;
            x.Right = T2;
            
            // Update heights
            x.Height = global::System.Math.Max(GetHeight(x.Left), GetHeight(x.Right)) + 1;
            y.Height = global::System.Math.Max(GetHeight(y.Left), GetHeight(y.Right)) + 1;
            
            return y;
        }
        
        /// <summary>
        /// Search for a value
        /// </summary>
        public bool Contains(T value)
        {
            return SearchRecursive(root, value);
        }
        
        private bool SearchRecursive(AVLTreeNode<T>? node, T value)
        {
            if (node == null)
                return false;
                
            int comparison = value.CompareTo(node.Value);
            
            if (comparison == 0)
                return true;
            else if (comparison < 0)
                return SearchRecursive(node.Left, value);
            else
                return SearchRecursive(node.Right, value);
        }
        
        /// <summary>
        /// Get values in sorted order
        /// </summary>
        public IEnumerable<T> InOrder()
        {
            return InOrderTraversal(root);
        }
        
        private IEnumerable<T> InOrderTraversal(AVLTreeNode<T>? node)
        {
            if (node != null)
            {
                foreach (var value in InOrderTraversal(node.Left))
                    yield return value;
                    
                yield return node.Value;
                
                foreach (var value in InOrderTraversal(node.Right))
                    yield return value;
            }
        }
    }
    
    /// <summary>
    /// B-Tree Node
    /// </summary>
    public class BTreeNode<T> where T : IComparable<T>
    {
        public List<T> Keys { get; set; }
        public List<BTreeNode<T>?> Children { get; set; }
        public bool IsLeaf { get; set; }
        
        public BTreeNode(bool isLeaf)
        {
            Keys = new List<T>();
            Children = new List<BTreeNode<T>?>();
            IsLeaf = isLeaf;
        }
    }
    
    /// <summary>
    /// B-Tree for efficient disk-based storage
    /// </summary>
    public class BTree<T> where T : IComparable<T>
    {
        private BTreeNode<T>? root;
        private readonly int degree; // Minimum degree
        private int count;
        
        public int Count => count;
        public bool IsEmpty => count == 0;
        
        public BTree(int degree = 3)
        {
            if (degree < 2)
                throw new ArgumentException("Degree must be at least 2");
                
            this.degree = degree;
            root = new BTreeNode<T>(true);
        }
        
        /// <summary>
        /// Search for a key in the B-tree
        /// </summary>
        public bool Contains(T key)
        {
            return Search(root, key) != null;
        }
        
        private (BTreeNode<T>? node, int index)? Search(BTreeNode<T>? node, T key)
        {
            if (node == null)
                return null;
                
            int i = 0;
            while (i < node.Keys.Count && key.CompareTo(node.Keys[i]) > 0)
                i++;
                
            if (i < node.Keys.Count && key.CompareTo(node.Keys[i]) == 0)
                return (node, i);
                
            if (node.IsLeaf)
                return null;
                
            return Search(node.Children[i], key);
        }
        
        /// <summary>
        /// Insert a key into the B-tree
        /// </summary>
        public void Insert(T key)
        {
            if (root == null)
            {
                root = new BTreeNode<T>(true);
                root.Keys.Add(key);
                count++;
                return;
            }
            
            if (root.Keys.Count == 2 * degree - 1)
            {
                var newRoot = new BTreeNode<T>(false);
                newRoot.Children.Add(root);
                SplitChild(newRoot, 0);
                root = newRoot;
            }
            
            InsertNonFull(root, key);
            count++;
        }
        
        private void InsertNonFull(BTreeNode<T> node, T key)
        {
            int i = node.Keys.Count - 1;
            
            if (node.IsLeaf)
            {
                node.Keys.Add(default(T)!);
                while (i >= 0 && key.CompareTo(node.Keys[i]) < 0)
                {
                    node.Keys[i + 1] = node.Keys[i];
                    i--;
                }
                node.Keys[i + 1] = key;
            }
            else
            {
                while (i >= 0 && key.CompareTo(node.Keys[i]) < 0)
                    i--;
                    
                i++;
                
                if (node.Children[i]!.Keys.Count == 2 * degree - 1)
                {
                    SplitChild(node, i);
                    if (key.CompareTo(node.Keys[i]) > 0)
                        i++;
                }
                
                InsertNonFull(node.Children[i]!, key);
            }
        }
        
        private void SplitChild(BTreeNode<T> parent, int index)
        {
            var fullChild = parent.Children[index]!;
            var newChild = new BTreeNode<T>(fullChild.IsLeaf);
            
            // Move the second half of keys to new child
            for (int j = 0; j < degree - 1; j++)
            {
                newChild.Keys.Add(fullChild.Keys[j + degree]);
            }
            
            // If not leaf, move the second half of children
            if (!fullChild.IsLeaf)
            {
                for (int j = 0; j < degree; j++)
                {
                    newChild.Children.Add(fullChild.Children[j + degree]);
                }
                // Remove children from degree to end
                while (fullChild.Children.Count > degree)
                {
                    fullChild.Children.RemoveAt(fullChild.Children.Count - 1);
                }
            }
            
            // Move median key up to parent
            parent.Keys.Insert(index, fullChild.Keys[degree - 1]);
            parent.Children.Insert(index + 1, newChild);
            
            // Remove moved keys from full child
            int removeCount = degree;
            for (int i = 0; i < removeCount; i++)
            {
                fullChild.Keys.RemoveAt(degree - 1);
            }
        }
        
        /// <summary>
        /// Get all keys in sorted order
        /// </summary>
        public IEnumerable<T> InOrder()
        {
            return InOrderTraversal(root);
        }
        
        private IEnumerable<T> InOrderTraversal(BTreeNode<T>? node)
        {
            if (node == null)
                yield break;
                
            int i;
            for (i = 0; i < node.Keys.Count; i++)
            {
                if (!node.IsLeaf && i < node.Children.Count)
                {
                    foreach (var key in InOrderTraversal(node.Children[i]))
                        yield return key;
                }
                
                yield return node.Keys[i];
            }
            
            if (!node.IsLeaf && i < node.Children.Count)
            {
                foreach (var key in InOrderTraversal(node.Children[i]))
                    yield return key;
            }
        }
    }
    
    /// <summary>
    /// Trie (Prefix Tree) for string operations
    /// </summary>
    public class Trie
    {
        private class TrieNode
        {
            public Dictionary<char, TrieNode> Children { get; set; }
            public bool IsEndOfWord { get; set; }
            
            public TrieNode()
            {
                Children = new Dictionary<char, TrieNode>();
                IsEndOfWord = false;
            }
        }
        
        private readonly TrieNode root;
        private int count;
        
        public int Count => count;
        
        public Trie()
        {
            root = new TrieNode();
        }
        
        /// <summary>
        /// Insert a word into the trie
        /// </summary>
        public void Insert(string word)
        {
            var current = root;
            
            foreach (char c in word)
            {
                if (!current.Children.ContainsKey(c))
                    current.Children[c] = new TrieNode();
                    
                current = current.Children[c];
            }
            
            if (!current.IsEndOfWord)
            {
                current.IsEndOfWord = true;
                count++;
            }
        }
        
        /// <summary>
        /// Search for a word in the trie
        /// </summary>
        public bool Contains(string word)
        {
            var current = root;
            
            foreach (char c in word)
            {
                if (!current.Children.ContainsKey(c))
                    return false;
                    
                current = current.Children[c];
            }
            
            return current.IsEndOfWord;
        }
        
        /// <summary>
        /// Check if any word starts with prefix
        /// </summary>
        public bool StartsWith(string prefix)
        {
            var current = root;
            
            foreach (char c in prefix)
            {
                if (!current.Children.ContainsKey(c))
                    return false;
                    
                current = current.Children[c];
            }
            
            return true;
        }
        
        /// <summary>
        /// Get all words with given prefix
        /// </summary>
        public IEnumerable<string> GetWordsWithPrefix(string prefix)
        {
            var current = root;
            
            foreach (char c in prefix)
            {
                if (!current.Children.ContainsKey(c))
                    yield break;
                    
                current = current.Children[c];
            }
            
            foreach (var word in GetAllWords(current, prefix))
                yield return word;
        }
        
        private IEnumerable<string> GetAllWords(TrieNode node, string prefix)
        {
            if (node.IsEndOfWord)
                yield return prefix;
                
            foreach (var child in node.Children)
            {
                foreach (var word in GetAllWords(child.Value, prefix + child.Key))
                    yield return word;
            }
        }
        
        /// <summary>
        /// Remove a word from the trie
        /// </summary>
        public bool Remove(string word)
        {
            if (!Contains(word))
                return false;
                
            RemoveHelper(root, word, 0);
            count--;
            return true;
        }
        
        private bool RemoveHelper(TrieNode node, string word, int index)
        {
            if (index == word.Length)
            {
                node.IsEndOfWord = false;
                return node.Children.Count == 0;
            }
            
            char c = word[index];
            if (!node.Children.ContainsKey(c))
                return false;
                
            var child = node.Children[c];
            bool shouldDeleteChild = RemoveHelper(child, word, index + 1);
            
            if (shouldDeleteChild)
            {
                node.Children.Remove(c);
                return !node.IsEndOfWord && node.Children.Count == 0;
            }
            
            return false;
        }
    }
} 