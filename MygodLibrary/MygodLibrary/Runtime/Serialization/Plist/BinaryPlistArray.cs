//-----------------------------------------------------------------------
// <copyright file="BinaryPlistArray.cs" company="Tasty Codes">
//     Copyright (c) 2011 Chad Burggraf.
//     Inspired by BinaryPListParser.java, copyright (c) 2005 Werner Randelshofer
//          http://www.java2s.com/Open-Source/Java-Document/Swing-Library/jide-common/com/jidesoft/plaf/aqua/BinaryPListParser.java.htm
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Mygod.Runtime.Serialization.Plist
{
    /// <summary>
    ///     Represents an array value in a binary plist.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "The spelling is correct.")]
    internal class BinaryPlistArray
    {
        /// <summary>
        ///     Initializes a new instance of the BinaryPlistArray class.
        /// </summary>
        /// <param name="objectTable">A reference to the binary plist's object table.</param>
        /// <param name="size">The size of the array.</param>
        public BinaryPlistArray(IList<BinaryPlistItem> objectTable, int size = 0)
        {
            ObjectReference = new List<int>(size);
            ObjectTable = objectTable;
        }

        /// <summary>
        ///     Gets the array's object reference collection.
        /// </summary>
        public IList<int> ObjectReference { get; }

        /// <summary>
        ///     Gets a reference to the binary plist's object table.
        /// </summary>
        public IList<BinaryPlistItem> ObjectTable { get; }

        /// <summary>
        ///     Converts this instance into an <see cref="T:object[]" /> array.
        /// </summary>
        /// <returns>
        ///     The <see cref="T:object[]" /> array representation of this instance.
        /// </returns>
        public object[] ToArray()
        {
            var array = new object[ObjectReference.Count];

            for (var i = 0; i < array.Length; i++)
            {
                var objectRef = ObjectReference[i];

                if (objectRef < 0 || objectRef >= ObjectTable.Count
                                  || (ObjectTable[objectRef] != null && ObjectTable[objectRef].Value == this)) continue;
                var objectValue = ObjectTable[objectRef] == null ? null : ObjectTable[objectRef].Value;
                var innerDict = objectValue as BinaryPlistDictionary;

                if (innerDict != null) objectValue = innerDict.ToDictionary();
                else
                {
                    var innerArray = objectValue as BinaryPlistArray;

                    if (innerArray != null)
                        objectValue = innerArray.ToArray();
                }

                array[i] = objectValue;
            }

            return array;
        }

        /// <summary>
        ///     Returns the string representation of this instance.
        /// </summary>
        /// <returns>This instance's string representation.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("[");

            for (var i = 0; i < ObjectReference.Count; i++)
            {
                if (i > 0)
                    sb.Append(",");

                var objectRef = ObjectReference[i];

                if (ObjectTable.Count > objectRef && (ObjectTable[objectRef] == null || ObjectTable[objectRef].Value != this))
                    sb.Append(ObjectReference[objectRef]);
                else
                    sb.Append("*" + objectRef);
            }

            return sb + "]";
        }
    }
}