namespace Cookie.Serializers.Nested
{
    internal class Deserializers
    {
        /// <summary>
        /// Processes a given body string as serialized by the 
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
                        Node current = openPos == 0 ? open : open.AddBefore(openPos);
                        prevAdded = current;

                        // Update the first Node if the new Node is earlier in the sequence.
                        if (prevAdded.start < first.start)
                        {
                            first = prevAdded;
                        }

                        // Adjust Nodes to account for depth changes and split Nodes if necessary.
                        Node added = current;
                        while (true)
                        {
                            if (current.start < closePos && current.end > closePos)
                            {
                                // Split the current Node at the closing brace position and adjust depth.
                                var _break = current.AddBefore(closePos);
                                break;
                            }
                            // Step forwards
                            if (current.Next != null)
                            {
                                current = current.Next;
                                continue;
                            }
                            break; // Exit if no more Nodes to process.
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

            Clean(first, body, span);
            // Move to the next step
            Process(first, body, span);
        }


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
        /// Clean the stack hierarchy
        /// </summary>
        /// <param name="first"></param>
        /// <param name="body"></param>
        /// <param name="span"></param>
        internal static void Clean(Node first, string body, Span<char> span)
        {
            Node? n = first;
            int depth = 0;
            while (n != null)
            {
                // Some nodes are empty, but there is an edge case where stripping empty nodes
                // may strip stack changes and invalidate the depth stack, so we need to allow
                // for this....
                int len = n.end - n.start;
                if (len <= 0)
                {
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
                    // again, the same depth edge case
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
        internal static void Process(Node first, string body, Span<char> span)
        {
            // iterate forwards
            Node? n = first;

            // Create the underlying dictionary
            n.data = new Dictionary<string, object>();

            // Stack for tracking nodes as we move through the depth hierarchy
            Stack<Node?> stack = new();

            while (n != null)
            {
                // Manage the stack
                Node? previousLevel = null;
                while (stack.Count < n.depth - 1) stack.Push(null);
                while (stack.Count >= n.depth) previousLevel = stack.Pop();

                // We must keep bonked nodes to track depth
                // but... yeah
                if (n.start < 0)
                {
                    n = n.Next;
                    continue;
                }
                else stack.Push(n);

                // Compute the local object
                // This is always either a dictionary or a list
                n.data ??= DeserializeContainer(span.Slice(n.start, n.end - n.start));

                stack.Push(n);

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
                        if (key != null)
                        {
                            if (!dict.TryAdd(key, n.data))
                            {
                                dict[key] = n.data;
                            }
                        }
                        else
                        {
                            // We need to read the key of the frame immediately before us
                            if (prev.EndsWith("*"))
                            {
                                int index = prev.Slice(1).LastIndexOf(";");
                                if (index < 0) index = 1;
                                else index += 3;

                                // Read the value we should have
                                if (index < prev.Length - 1)
                                {
                                    prev = prev.Slice(index, prev.Length - index - 1).Trim();
                                    if (!dict.TryAdd(prev.ToString(), n.data))
                                    {
                                        dict[prev.ToString()] = n.data;
                                    }
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
        internal static void MergeBackwards(Node n, Node? target, Span<char> span, out string? key)
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
        /// Deserializes the given span into either a List of strings, or a string-keyed dictionary containing either
        /// strings, or objects.
        /// 
        /// This method does not populate sub-keys of dictionaries. Empty values are intended to be filled further up the 
        /// processing pipeline.
        /// </summary>
        /// <param name="span"></param>
        /// <returns></returns>
        internal static object? DeserializeContainer(Span<char> span)
        {
            // if the prefix is m, then we are in a mapping
            // and if the prefix is ";" then we are in a mapping and between {} groups
            // Both cases create a dictionary
            if (span[0] == ';' || span[0] == 'm')
            {
                Dictionary<string, object?> result = new();
                // let's go piece by piece
                int pos = 1;
                while (pos > 0 && pos < span.Length)
                {
                    // get the next one after the current one
                    int next = span.Slice(pos).IndexOf(";");
                    if (next < 0) next = span.Length;
                    else next += pos;

                    // Slice out the part we want
                    var split = span.Slice(pos, next - pos).Trim();
                    if (split.Length > 0)
                    {
                        // Find the asterisk key delimiter
                        int keyPos = split.IndexOf("*");
                        if (keyPos > 0)
                        {
                            // Split out the key and get the value, or null
                            var key = split.Slice(0, keyPos).Trim();
                            string? value = null;
                            if (keyPos < split.Length - 1)
                            {
                                value = split.Slice(keyPos + 1).Trim().ToString();
                            }
                            // store
                            result.TryAdd(key.ToString(), value);
                        }
                    }
                    // No next -> next = -1, so pos is now 0
                    pos = next <= 0 ? -1 : next + 1;
                }
                return result;
            }
            // Lists and string-dicts are essentially the same
            else if (span[0] == 'l' || span[0] == 'd')
            {
                List<string> results = [];
                int pos = 1;
                while (pos > 0 && pos <= span.Length)
                {
                    // get the next one after the current one
                    int next = span.Slice(pos).IndexOf(";");
                    if (next < 0) next = span.Length;
                    else next += pos;

                    // Now cut out the split region and add it to the list
                    var split = span.Slice(pos, next - pos).Trim();
                    if (split.Length > 0)
                    {
                        results.Add(split.ToString());
                    }
                    // No next -> next = -1, so pos is now 0
                    pos = next <= 0 ? -1 : next + 1;
                }
                // If it's a string-dict, then we can simply
                // boink the list entries by the squiggle
                if (span[0] == 'd')
                {
                    return results
                        .Select(x => x.Split("~"))
                        .Where(x => x.Length == 2)
                        .Select(x => (x[0], x[1]))
                        .ToDictionary();
                }
                else return results;
            }
            return null;

        }


    }
}
