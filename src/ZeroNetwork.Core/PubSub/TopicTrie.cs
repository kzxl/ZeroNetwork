using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ZeroNetwork.PubSub
{
    /// <summary>
    /// Thread-safe Trie data structure for MQTT-style topic routing.
    /// Supports exact matching, single-level wildcards ('+'), and multi-level wildcards ('#').
    /// Designed with Copy-On-Write subscriber collections for zero-lock read dispatching.
    /// </summary>
    /// <typeparam name="TSubscriber">Type of the subscriber token/handler.</typeparam>
    public sealed class TopicTrie<TSubscriber>
    {
        private sealed class TrieNode
        {
            public readonly ConcurrentDictionary<string, TrieNode> Children = new ConcurrentDictionary<string, TrieNode>(StringComparer.Ordinal);
            public TrieNode? SingleWildcardChild; // '+'
            public TrieNode? MultiWildcardChild;  // '#'

            // Copy-On-Write array for lock-free read iteration
            public volatile TSubscriber[] Subscribers = Array.Empty<TSubscriber>();
            public readonly object SyncRoot = new object();
        }

        private readonly TrieNode _root = new TrieNode();

        /// <summary>
        /// Registers a subscriber for the specified topic pattern.
        /// </summary>
        /// <param name="pattern">Topic pattern, e.g. "sensors/+/temperature" or "factory/#".</param>
        /// <param name="subscriber">Subscriber instance.</param>
        public void Add(string pattern, TSubscriber subscriber)
        {
            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentNullException(nameof(pattern));
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            string[] segments = pattern.Split('/');
            ValidatePattern(segments);

            TrieNode current = _root;

            for (int i = 0; i < segments.Length; i++)
            {
                string seg = segments[i];

                if (seg == "+")
                {
                    if (current.SingleWildcardChild == null)
                    {
                        lock (current.SyncRoot)
                        {
                            if (current.SingleWildcardChild == null)
                                current.SingleWildcardChild = new TrieNode();
                        }
                    }
                    current = current.SingleWildcardChild;
                }
                else if (seg == "#")
                {
                    if (current.MultiWildcardChild == null)
                    {
                        lock (current.SyncRoot)
                        {
                            if (current.MultiWildcardChild == null)
                                current.MultiWildcardChild = new TrieNode();
                        }
                    }
                    current = current.MultiWildcardChild;
                    break; // '#' must be the last segment
                }
                else
                {
                    current = current.Children.GetOrAdd(seg, _ => new TrieNode());
                }
            }

            lock (current.SyncRoot)
            {
                var list = new List<TSubscriber>(current.Subscribers) { subscriber };
                current.Subscribers = list.ToArray();
            }
        }

        /// <summary>
        /// Removes a subscriber from the specified topic pattern.
        /// </summary>
        /// <param name="pattern">Topic pattern.</param>
        /// <param name="subscriber">Subscriber instance to remove.</param>
        /// <returns><c>true</c> if removed; otherwise <c>false</c>.</returns>
        public bool Remove(string pattern, TSubscriber subscriber)
        {
            if (string.IsNullOrEmpty(pattern) || subscriber == null)
                return false;

            string[] segments = pattern.Split('/');
            TrieNode? current = _root;

            for (int i = 0; i < segments.Length; i++)
            {
                string seg = segments[i];
                if (seg == "+")
                {
                    current = current.SingleWildcardChild;
                }
                else if (seg == "#")
                {
                    current = current.MultiWildcardChild;
                    break;
                }
                else
                {
                    if (!current.Children.TryGetValue(seg, out current))
                        return false;
                }

                if (current == null)
                    return false;
            }

            if (current == null) return false;

            lock (current.SyncRoot)
            {
                var list = new List<TSubscriber>(current.Subscribers);
                bool removed = list.Remove(subscriber);
                if (removed)
                {
                    current.Subscribers = list.ToArray();
                }
                return removed;
            }
        }

        /// <summary>
        /// Finds all subscribers whose patterns match the concrete published topic.
        /// </summary>
        /// <param name="topic">Concrete topic, e.g. "sensors/line1/temperature".</param>
        /// <param name="results">List to append matched subscribers into.</param>
        public void GetMatches(string topic, List<TSubscriber> results)
        {
            if (string.IsNullOrEmpty(topic) || results == null)
                return;

            string[] segments = topic.Split('/');
            CollectMatches(_root, segments, 0, results);
        }

        private static void CollectMatches(TrieNode current, string[] segments, int index, List<TSubscriber> results)
        {
            // 1. If this node has a '#' wildcard child, it matches this segment and all subsequent segments
            if (current.MultiWildcardChild != null)
            {
                var multiSubs = current.MultiWildcardChild.Subscribers;
                if (multiSubs.Length > 0)
                {
                    results.AddRange(multiSubs);
                }
            }

            // 2. If we reached the end of the topic segments, collect direct subscribers at this node
            if (index == segments.Length)
            {
                var exactSubs = current.Subscribers;
                if (exactSubs.Length > 0)
                {
                    results.AddRange(exactSubs);
                }
                return;
            }

            string seg = segments[index];

            // 3. Exact literal segment match
            if (current.Children.TryGetValue(seg, out var literalChild))
            {
                CollectMatches(literalChild, segments, index + 1, results);
            }

            // 4. Single-level '+' wildcard match (matches exactly 1 segment)
            if (current.SingleWildcardChild != null)
            {
                CollectMatches(current.SingleWildcardChild, segments, index + 1, results);
            }
        }

        /// <summary>
        /// Tests if a topic pattern matches a concrete topic.
        /// </summary>
        public static bool IsMatch(string pattern, string topic)
        {
            if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(topic))
                return false;

            string[] pSegs = pattern.Split('/');
            string[] tSegs = topic.Split('/');

            int pi = 0;
            int ti = 0;

            while (pi < pSegs.Length && ti < tSegs.Length)
            {
                if (pSegs[pi] == "#")
                {
                    return true; // '#' at end matches everything remaining
                }

                if (pSegs[pi] != "+" && !string.Equals(pSegs[pi], tSegs[ti], StringComparison.Ordinal))
                {
                    return false;
                }

                pi++;
                ti++;
            }

            if (pi < pSegs.Length && pSegs[pi] == "#")
                return true;

            return pi == pSegs.Length && ti == tSegs.Length;
        }

        private static void ValidatePattern(string[] segments)
        {
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == "#" && i != segments.Length - 1)
                {
                    throw new ArgumentException("Multi-level wildcard '#' can only appear as the last segment of the pattern.", nameof(segments));
                }
            }
        }
    }
}
