namespace Intervals.Tools;

using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// AATree is a self balancing binary search tree that stores and retrieves ordered items efficiently.
/// More information can be found here: https://en.wikipedia.org/wiki/AA_tree
/// </summary>
/// <typeparam name="T"></typeparam>
internal class AATree<T> : IEnumerable<T>
{
    internal class Node
    {
        private readonly Action<Node> _onChildChanged;
        private Node? _left;
        private Node? _right;

        public Node(T element, Action<Node> onChildChanged)
        {
            Value = element;
            Level = 1;
            _onChildChanged = onChildChanged;
        }

        public T Value { get; set; }

        public Node? Right
        {
            get => _right;
            set
            {
                _right = value;
                _onChildChanged.Invoke(this);
            }
        }

        public Node? Left
        {
            get => _left;
            set
            {
                _left = value;
                _onChildChanged.Invoke(this);
            }
        }

        public int Level { get; set; }
    }

    private Node? _root;
    private IComparer<T> _comparer;
    private readonly Action<Node> _onChildChanged;

    /// <summary>
    /// Creates an empty AATree.
    /// </summary>
    public AATree()
        : this(Comparer<T>.Default, (_) => {})
    { }

    /// <summary>
    /// Creates AATree with comparison.
    /// </summary>
    /// <param name="comparer"></param>
    public AATree(Comparison<T> comparison)
        : this(Comparer<T>.Create(comparison), (_) => {})
    { }

    /// <summary>
    /// Creates AATree with comparer.
    /// </summary>
    /// <param name="comparer"></param>
    public AATree(IComparer<T> comparer, Action<Node> onChildChanged)
    {
        _comparer = comparer;
        _onChildChanged = onChildChanged;
    }

    internal Node? Root => _root;

    public int Count { get; private set; }

    public bool Add(T element)
    {
        var oldCount = Count;
        _root = Insert(_root, element);

        return Count != oldCount;
    }

    private Node Insert(Node? node, T element)
    {
        if (node is null)
        {
            node = new Node(element, _onChildChanged);
            Count++;
        }

        var comparison = _comparer.Compare(element, node.Value);
        if (comparison < 0)
        {
            node.Left = Insert(node.Left, element);
        }
        else if (comparison > 0)
        {
            node.Right = Insert(node.Right, element);
        }

        node = Skew(node);
        node = Split(node);

        return node!;
    }

    public bool Remove(T element)
    {
        (_root, var isRemoved) = Remove(_root, element);

        if (isRemoved)
        {
            Count--;
        }

        return isRemoved;
    }

    private (Node?, bool) Remove(Node? node, T element)
    {
        var isRemoved = false;
        if (node is null)
        {
            return (default, isRemoved);
        }

        var comparison = _comparer.Compare(element, node.Value);
        if (comparison > 0)
        {
            (node.Right, isRemoved) = Remove(node.Right, element);
        }
        else if (comparison < 0)
        {
            (node.Left, isRemoved) = Remove(node.Left, element);
        }
        else if (node!.Left is null)
        {
            return (node!.Right, true);
        }
        else
        {
            var nextNode = FindMin(node.Right!);
            nextNode!.Right = RemoveMin(node.Right!);
            nextNode!.Left = node.Left;
            nextNode.Level = node.Level;

            node = nextNode;

            isRemoved = true;
        }

        node = BalanceRemoval(node);
        return (node, isRemoved);
    }

    private Node? BalanceRemoval(Node? node)
    {
        if ((node!.Left?.Level ?? 0) < node?.Level - 1 || (node!.Right?.Level ?? 0) < node!.Level - 1)
        {
            node!.Level--;
            if (node!.Right?.Level > node.Level)
            {
                node.Right.Level = node.Level;
            }

            node = Skew(node);
            node!.Right = Skew(node.Right);
            if (node!.Right?.Right is not null)
            {
                node!.Right.Right = Skew(node.Right.Right);
            }

            node = Split(node);
            node!.Right = Split(node.Right);
        }

        return node;
    }

    private Node? FindMin(Node node)
    {
        if (node.Left is null)
        {
            return node;
        }

        return FindMin(node.Left);
    }

    private Node? RemoveMin(Node? node)
    {
        if (node!.Left is null)
        {
            return node.Right;
        }

        node.Left = RemoveMin(node.Left);
        node = BalanceRemoval(node);

        return node;
    }

    private Node? Split(Node? node)
    {
        if (node?.Right?.Right?.Level == node?.Level)
        {
            node = RotateRight(node!);
            node.Level++;
        }

        return node;
    }

    private Node RotateRight(Node node)
    {
        var temp = node.Right;

        node.Right = temp?.Left;
        temp!.Left = node;

        return temp;
    }

    private Node? Skew(Node? node)
    {
        if (node?.Left?.Level == node?.Level)
        {
            node = RotateLeft(node!);
        }

        return node;
    }

    private Node RotateLeft(Node node)
    {
        var temp = node.Left;

        node.Left = temp?.Right;
        temp!.Right = node;

        return temp;
    }

    public bool Contains(T element)
    {
        var current = _root;

        while (current != null)
        {
            var comparison = _comparer.Compare(element, current.Value);
            if (comparison > 0)
            {
                current = current.Right;
            }
            else if (comparison < 0)
            {
                current = current.Left;
            }
            else
            {
                return true;
            }
        }

        return false;
    }

    public void Clear()
    {
        _root = null;
        Count = 0;
    }

    public IEnumerator<T> GetEnumerator()
    {
        var visited = new HashSet<Node>();
        var stack = new Stack<Node>();
        if (_root is not null)
        {
            stack.Push(_root);
        }

        while (stack.Count > 0)
        {
            var current = stack.Peek();
            while (current?.Left is not null && !visited.Contains(current.Left))
            {
                stack.Push(current.Left);
                current = current.Left;
            }

            current = stack.Pop();
            visited.Add(current);

            yield return current.Value;

            if (current.Right is not null && !visited.Contains(current.Right))
            {
                stack.Push(current.Right);
            }
        };
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}