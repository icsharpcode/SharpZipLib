#if (PCL)
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace ICSharpCode.SharpZipLib
{
    /// <summary>
    /// Simulate ArrayList
    /// </summary>
    class ArrayList : List<Object>
    {
        class PComparer : IComparer<Object>
        {
            IComparer _Cmp;
            public PComparer(IComparer cmp)
            {
                _Cmp = cmp;
            }
            public int Compare(object x, object y)
            {
                return _Cmp.Compare(x, y);
            }
        }
        public ArrayList()
        {
        }
        public ArrayList(int capacity)
            : base(capacity)
        {
        }
        public new int Add(Object item)
        {
            base.Add(item);
            return Count - 1;
        }
        public void Sort(IComparer comparer)
        {
            base.Sort(new PComparer(comparer));
        }
        public virtual Array ToArray(Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");
            Contract.Ensures(Contract.Result<Array>() != null);
            Contract.EndContractBlock();
            var items = this.ToArray();
            Array array = Array.CreateInstance(type, items.Length);
            Array.Copy(items, 0, array, 0, items.Length);
            return array;
        }

    }
    /// <summary>
    /// Simulate Hashtable
    /// </summary>
    class Hashtable : Dictionary<Object, Object>
    {

    }
    /// <summary>
    /// Simulate ICloneable
    /// </summary>
    interface ICloneable
    {
        Object Clone();
    }
    /// <summary>
    /// Simulate System.IO.PathTooLongException
    /// </summary>
    public class PathTooLongException : Exception
    {
        /// <summary>
        /// Create a new exception
        /// </summary>
        public PathTooLongException()
            : base("Path too long")
        {
        }
        /// <summary>
        /// Create a new exception
        /// </summary>
        public PathTooLongException(String message)
            : base(message)
        {
        }
        /// <summary>
        /// Create a new exception
        /// </summary>
        public PathTooLongException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
#endif