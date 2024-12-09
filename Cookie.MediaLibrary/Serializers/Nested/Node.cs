namespace Cookie.Serializers.Nested
{
    /// <summary>
    /// Represents a node in a doubly linked list structure, storing data and positional information.
    /// </summary>
    internal class Node
    {
        /// <summary>
        /// The data associated with this node. Can be of any type or null.
        /// </summary>
        public object? data;

        /// <summary>
        /// The start index of the span represented by this node.
        /// </summary>
        public int start;

        /// <summary>
        /// The end index of the span represented by this node.
        /// </summary>
        public int end;

        /// <summary>
        /// The depth of the node in a hierarchical structure, used for nesting levels.
        /// </summary>
        public int depth;

        /// <summary>
        /// Reference to the previous node in the linked list. Null if this is the first node.
        /// </summary>
        public Node? Previous = null;

        /// <summary>
        /// Reference to the next node in the linked list. Null if this is the last node.
        /// </summary>
        public Node? Next = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="Node"/> class with specified start and end positions.
        /// </summary>
        /// <param name="start">The start index of the span.</param>
        /// <param name="end">The end index of the span.</param>
        public Node(int start, int end)
        {
            this.start = start;
            this.end = end;
        }

        /// <summary>
        /// Splits the current node and adds a new node immediately after it.
        /// </summary>
        /// <param name="pos">The position at which to split the current node.</param>
        /// <returns>The newly created node.</returns>
        public Node AddAfter(int pos)
        {
            Node n = new Node(pos, end);
            end = pos;
            if (Next != null)
            {
                Next.Previous = n;
                n.Next = Next;
            }
            Next = n;
            n.Previous = this;
            return n;
        }

        /// <summary>
        /// Splits the current node and adds a new node immediately before it.
        /// </summary>
        /// <param name="pos">The position at which to split the current node.</param>
        /// <returns>The newly created node.</returns>
        public Node AddBefore(int pos)
        {
            Node n = new Node(start, pos);
            start = pos;
            if (Previous != null)
            {
                Previous.Next = n;
                n.Previous = Previous;
            }
            Previous = n;
            n.Next = this;
            return n;
        }

        /// <summary>
        /// Returns a string representation of the node, showing its start and end positions.
        /// </summary>
        /// <returns>A string in the format "(start,end)".</returns>
        public override string ToString()
        {
            return $"({start},{end})";
        }
    }
}
