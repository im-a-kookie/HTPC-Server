namespace Cookie.Serializers.Nested
{
    public class NestedDecoder
    {

        public static object Destringify(string value)
        {
            var span = value.AsSpan();
            char i = span[0];
            span = span.Slice(1);

            switch (i)
            {
                case 'i': return int.TryParse(span, out var n) ? n : -1;
                case 'f': return double.TryParse(span, out var d) ? d : -1;
                case 's': return span.ToString();
            }
            return value;
        }


        /// <summary>
        /// Processes a given body string as serialized by methods in <see cref="Serializers.Nested.NestedEncoder"/>
        /// </summary>
        /// <param name="body"></param>
        public static void Process(string body)
        {
            // Remove carriage return characters from the input string.
            body = body.Replace("\r", "");

            // Convert the string into a character array and then to a Span<char> for efficient manipulation.
            var arr = body.ToArray();
            var span = arr.AsSpan();

            // Initialize the first Node covering the entire input string.
            Node first = new Node(0, body.Length);
            Node? last = first; // Tracks the last Node.
            Node? prevAdded = first; // Tracks the previously added Node.

            // Find the position of the first closing brace '}'.
            int closePos = span.IndexOf("}") + 1;
            if (closePos > 0)
            {
                // Add a new Node after the first closing brace.
                // cutting this manually seems simpler
                last = first.AddAfter(closePos);

                // Continue processing while there are closing braces.
                while (closePos > 0)
                {
                    // Find the position of the last opening brace '{' before the current closing brace.
                    int openPos = span.Slice(0, closePos).LastIndexOf("{");
                    if (openPos >= 0)
                    {
                        // Clear the characters between the opening and closing braces in the span.
                        arr[openPos] = ' ';
                        arr[closePos - 1] = ' ';

                        // Locate the Node that corresponds to the current opening brace.
                        Node open = prevAdded;
                        while (openPos > open!.end) open = open.Next!;
                        while (open!.start > openPos) open = open.Previous!;

                        // Add a new Node before the current opening brace and mark it as closed.
                        Node? current = openPos == 0 ? open : open.AddBefore(openPos);
                        prevAdded = current;

                        // Update the first Node if the new Node is earlier in the sequence.
                        if (prevAdded.start < first.start)
                        {
                            first = prevAdded;
                        }

                        // Adjust Nodes to account for depth changes and split Nodes if necessary.
                        Node added = current;
                        while (current != null)
                        {
                            // catch nodes that encapsulte the closing brace
                            if (current.start < closePos && current.end > closePos)
                            {
                                // Split the current Node at the closing brace position and adjust depth.
                                var _break = current.AddBefore(closePos);
                                break;
                            }
                            current = current.Next;
                        }
                    }
                    else break; // Exit if no opening brace is found.

                    if (last == null) break;

                    // Find the next closing brace after the last processed position.
                    closePos = span.Slice(last.start).IndexOf("}");
                    if (closePos < 0) break;
                    else closePos += last.start + 1; // Adjust the position to be absolute.
                }
            }

            PopulateDepthAndClean(first, body, span);
            // Move to the next step
            Process(first, body, span);
        }

        /// <summary>
        /// Simple debug trace
        /// </summary>
        /// <param name="node"></param>
        /// <param name="body"></param>
        internal static void Debug(Node node, string body)
        {
            Console.WriteLine("Body Length: " + body.Length);
            var n = node;
            while (n != null)
            {
                int len = n.end - n.start;
                if (len > 0 && n.start > 0)
                {
                    Console.WriteLine($"({n.start},{n.end}) {body.Substring(n.start, len)}");
                }
                n = n.Next;
            }
        }

        /// <summary>
        /// Clean the node list, cleans empty nodes where possible, trims whitespace, and populates
        /// depth information through the nodes.
        /// 
        /// <para>Depth information is critical during rebuilding later, and is much easier to trace here
        /// than during list construction, so this method MUST be called.
        /// </para>
        /// </summary>
        /// <param name="first"></param>
        /// <param name="body"></param>
        /// <param name="span"></param>
        private static void PopulateDepthAndClean(Node first, string body, Span<char> span)
        {
            Node? n = first;
            int depth = 0;
            while (n != null)
            {
                // Some nodes are empty, but there is an edge case where stripping empty nodes
                // may strip depth changes and invalidate the depth stack, so we need to allow
                // for this....
                int len = n.end - n.start;
                if (len <= 0)
                {
                    // It only matters if a stripped node would dip below the current frame
                    if (n.Previous != null && n.Next != null)
                    {
                        if (n.depth >= n.Previous.depth || n.depth >= n.Next.depth)
                        {
                            n.Next.Previous = n.Previous;
                            n.Previous.Next = n.Next;
                        }
                    }
                    // bonk
                    n.start = -1;
                    n = n.Next;
                    continue;
                }

                // Calculate the depth
                if (body[n.start] == '{') ++depth;
                n.depth = depth;
                if (body[n.end - 1] == '}') --depth;

                // clean the inner body 
                var trimmed = span.Slice(n.start, n.end - n.start).TrimStart();
                n.start += len - trimmed.Length;
                len = trimmed.Length;

                // do the same for the end
                trimmed = trimmed.TrimEnd();
                n.end -= len - trimmed.Length;
                len = trimmed.Length;

                // If we zeroed it, then remove it
                if (len <= 0 || len == 1 && span[n.start] == ';')
                {
                    // As before, handling depth tracing
                    if (n.Previous != null && n.Next != null)
                    {
                        if (n.depth >= n.Previous.depth || n.depth >= n.Next.depth)
                        {
                            n.Next.Previous = n.Previous;
                            n.Previous.Next = n.Next;
                        }
                    }
                    n.start = -1;
                }
                n = n.Next;
            }
        }

        /// <summary>
        /// Process a node hierarchy from the given first node, using the given underlying
        /// span information.
        /// </summary>
        /// <param name="first"></param>
        /// <param name="body"></param>
        /// <param name="arr"></param>
        /// <param name="span"></param>
        private static void Process(Node first, string body, Span<char> span)
        {
            // iterate forwards
            Node? n = first;

            // Create the underlying dictionary
            n.data = new Dictionary<string, object>();

            // Stack for tracking nodes as we move through the depth hierarchy
            Stack<Node?> depthStack = new();

            while (n != null)
            {
                // Manage the stack
                Node? previousLevel = null;
                while (depthStack.Count < n.depth - 1) depthStack.Push(null);
                while (depthStack.Count >= n.depth) previousLevel = depthStack.Pop();

                // We must keep bonked nodes to track depth
                // but... yeah
                if (n.start < 0)
                {
                    n = n.Next;
                    continue;
                }
                else depthStack.Push(n);

                // Compute the local object
                // This is always either a dictionary or a list
                n.data ??= DeserializeContainer(span.Slice(n.start, n.end - n.start));

                depthStack.Push(n);

                // Walk through bonked entries
                Node? previousNode = n.Previous;
                while (previousNode != null && previousNode.start < 0)
                    previousNode = previousNode.Previous;

                string? key = null;

                // If we are within a mapping, then we need to keep careful track of
                // the stack hierarchy
                if (span[n.start] == ';' || span[n.start] == 'm')
                {
                    if (previousNode != null && previousNode.depth > n.depth + 1)
                    {
                        previousNode = previousLevel;
                        MergeBackwards(n, previousNode, span, out key);
                    }
                }

                // If we do have a previous node, then this is good
                if (previousNode != null)
                {
                    // Get the data span
                    var prev = span.Slice(previousNode.start, previousNode.end - previousNode.start);

                    // As we are a collection
                    // The previous value *must* be a dictionary<string,object>
                    // Henceforth:
                    previousNode.data ??= DeserializeContainer(prev);

                    // Load the previous data object and insert ourselves into it
                    if (previousNode.data is Dictionary<string, object?> dict)
                    {
                        // If we are using a key already, having merged into a previous frame
                        if (key != null) dict[key] = n.data;
                        else
                        {
                            if (prev.EndsWith("*"))
                            {
                                int index = prev.Slice(1).LastIndexOf(";") + 3;
                                index = index < 1 ? 1 : index; // Ensure index is at least 1

                                if (index < prev.Length - 1)
                                {
                                    prev = prev.Slice(index, prev.Length - index - 1).Trim();
                                    dict[prev.ToString()] = n.data;
                                }
                            }
                        }

                        // Skip cancelled members
                        Node? next = n.Next;
                        while (next != null && next.start < 0)
                        {
                            next = next.Next;
                        }

                        // If we have a next, and it matches our level
                        // Then we are assured that the container is shared
                        // Since collection groups are always prefixed by a key*
                        if (next != null && next.depth == previousNode.depth)
                        {
                            var nextslice = span.Slice(next.start, next.end - next.start);
                            next.data ??= DeserializeContainer(nextslice);

                            // Now merge the dictionaries, forwards to the next
                            if (next.data is Dictionary<string, object?> nextDict)
                            {
                                foreach (var kv in dict)
                                {
                                    nextDict.TryAdd(kv.Key, kv.Value);
                                }

                                previousNode.data = next.data;
                            }
                        }
                    }
                }

                n = n.Next;
            }
        }

        /// <summary>
        /// Merge the node in the current frame back into the target frame. This allows
        /// the node hierarchy to be resolved as the navigated depth changes arbitrarily
        /// </summary>
        /// <param name="n"></param>
        /// <param name="target"></param>
        /// <param name="span"></param>
        /// <param name="key"></param>
        private static void MergeBackwards(Node n, Node? target, Span<char> span, out string? key)
        {
            key = null;
            if (target == null) return;

            // Process the current node if its content ends with '*'.
            if (span[n.end - 1] == '*')
            {
                int pos = span.Slice(n.start, n.end - n.start).LastIndexOf(";") + 1;
                if (pos > 0)
                {
                    // Extract and trim the key from the slice.
                    key = span
                        .Slice(pos + n.start, n.end - n.start - pos - 1)
                        .Trim()
                        .ToString();


                    // Merge dictionaries if both nodes' data are dictionaries.
                    if (target.data is Dictionary<string, object> parentDict &&
                        n.data is Dictionary<string, object> childDict)
                    {
                        foreach (var kv in childDict)
                        {
                            parentDict.TryAdd(kv.Key, kv.Value);
                        }

                        n.data = parentDict; // Update the current node's data.
                    }
                }
            }
        }



        /// <summary>
        /// Deserializes the given span into either a List of strings or a string-keyed dictionary 
        /// containing either strings or objects.
        /// This method does not populate sub-keys of dictionaries. Empty values are intended to 
        /// be filled further up the processing pipeline.
        /// </summary>
        /// <param name="span">The span to deserialize.</param>
        /// <returns>The deserialized object (List or Dictionary) or null for unsupported input.</returns>
        /// <exception cref="ArgumentException">Thrown when input is invalid or malformed.</exception>
        private static object? DeserializeContainer(ReadOnlySpan<char> span)
        {
            if (span.IsEmpty)
                throw new ArgumentException("Input span cannot be empty.");

            char prefix = span[0];
            // ; and m indicate that this is a generic mapping
            if (prefix == ';' || prefix == 'm')
                return DeserializeMapping(span.Slice(1));

            // l or d indicate that this is a simpler list/dictionary string enumeration
            else if (prefix == 'l' || prefix == 'd')
                return DeserializeListOrDictionary(span.Slice(1), prefix == 'd');

            // Bonk otherwise
            throw new ArgumentException($"Unsupported prefix '{prefix}' in span.");
        }

        /// <summary>
        /// Deserializes a mapping construct represented by the given span
        /// </summary>
        private static Dictionary<string, object?> DeserializeMapping(ReadOnlySpan<char> span)
        {
            Dictionary<string, object?> result = new();
            int pos = 0;

            // manual "split" by stepping to each location of the split delimiter.
            // uUsing Span makes it faster
            while (pos < span.Length)
            {
                // get the entry delimiter, or the end of the block
                int next = span.Slice(pos).IndexOf(';');
                if (next < 0) next = span.Length;
                else next += pos;

                // now process the entry
                var segment = span.Slice(pos, next - pos).Trim();
                if (!segment.IsEmpty)
                {
                    // Mappings must have Key*, so find it
                    int keyDelimiter = segment.IndexOf('*');
                    if (keyDelimiter < 0)
                        throw new ArgumentException($"Malformed mapping entry: '{segment.ToString()}'. Missing Key!");

                    // now slice out the key/value
                    var key = segment.Slice(0, keyDelimiter).Trim();
                    var value = keyDelimiter < segment.Length - 1
                        ? segment.Slice(keyDelimiter + 1).Trim()
                        : ReadOnlySpan<char>.Empty;

                    if (!key.IsEmpty)
                        result.TryAdd(key.ToString(), value.IsEmpty ? null : Destringify(value.ToString()));
                    else
                        throw new ArgumentException("Mapping key cannot be empty.");
                }

                // and move past the semicolon
                pos = next + 1;
            }

            return result;
        }

        /// <summary>
        /// Deserializes a list or string-keyed dictionary represented by a span.
        /// </summary>
        private static object DeserializeListOrDictionary(ReadOnlySpan<char> span, bool isDictionary)
        {
            List<string> results = new();
            int pos = 0;

            // Same as mapping deserializer, jump to each entry delimiter and process
            while (pos < span.Length)
            {
                int next = span.Slice(pos).IndexOf(';');
                if (next < 0) next = span.Length;
                else next += pos;

                // but only need the value here, so... easy
                var segment = span.Slice(pos, next - pos).Trim();
                if (!segment.IsEmpty)
                    results.Add(segment.ToString());

                pos = next + 1;
            }

            // Now just remap dictionary based on key-value pairing
            if (isDictionary)
            {
                return results
                    .Select(entry =>
                    {
                        string[] parts = entry.Split('~');
                        if (parts.Length != 2)
                            throw new ArgumentException($"Malformed dictionary entry: '{entry}'. Key/Value malformatted.");
                        return (Key: parts[0], Value: parts[1]);
                    })
                    .ToDictionary(pair => pair.Key, pair => (object?)pair.Value);
            }
            return results;
        }


    }
}
