﻿using Loyc.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flame.Intermediate
{
    /// <summary>
    /// Defines a mutable view of an IR table that behaves like an ordered set.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class IRTableBuilder<T> : INodeStructure<IReadOnlyList<T>>
    {
        public IRTableBuilder(string TableName)
        {
            this.TableName = TableName;
            this.nodes = new List<LNode>();
            this.items = new List<T>();
            this.mappedItems = new Dictionary<T, int>();
        }

        /// <summary>
        /// Gets the table's name.
        /// </summary>
        public string TableName { get; private set; }

        private List<LNode> nodes;
        private List<T> items;
        private Dictionary<T, int> mappedItems;

        /// <summary>
        /// Gets the given element's index in this table.
        /// If this table contains no entry matching the given element,
        /// a new node is created and added to the table.
        /// </summary>
        /// <param name="Element"></param>
        /// <param name="CreateNode"></param>
        /// <returns></returns>
        public int GetIndex(T Element, Func<T, LNode> CreateNode)
        {
            int result;
            if (mappedItems.TryGetValue(Element, out result))
            {
                return result;
            }
            else
            {
                int index = nodes.Count;
                nodes.Add(CreateNode(Element));
                items.Add(Element);
                mappedItems[Element] = index;
                return index;
            }
        }
        
        public LNode Node
        {
            get
            {
                return NodeFactory.Call(TableName, nodes);
            }
        }

        public IReadOnlyList<T> Value
        {
            get { return items; }
        }
    }
}
