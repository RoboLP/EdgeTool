//-----------------------------------------------------------------------
// <copyright file="BinaryPlistDictionary.cs" company="Tasty Codes">
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
    ///     Represents a dictionary in a binary plist.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", Justification = "The spelling is correct.")]
    internal class BinaryPlistDictionary
    {
        /// <summary>
        ///     Initializes a new instance of the BinaryPlistDictionary class.
        /// </summary>
        /// <param name="objectTable">A reference to the binary plist's object table.</param>
        /// <param name="size">The size of the dictionay.</param>
        public BinaryPlistDictionary(IList<BinaryPlistItem> objectTable, int size)
        {
            KeyReference = new List<int>(size);
            ObjectReference = new List<int>(size);
            ObjectTable = objectTable;
        }

        /// <summary>
        ///     Gets the dictionary's key reference collection.
        /// </summary>
        public IList<int> KeyReference { get; }

        /// <summary>
        ///     Gets the dictionary's object reference collection.
        /// </summary>
        public IList<int> ObjectReference { get; }

        /// <summary>
        ///     Gets a reference to the binary plist's object table.
        /// </summary>
        public IList<BinaryPlistItem> ObjectTable { get; }

        /// <summary>
        ///     Converts this instance into a <see cref="Dictionary{Object, Object}" />.
        /// </summary>
        /// <returns>
        ///     A <see cref="Dictionary{Object, Object}" /> representation this instance.
        /// </returns>
        public Dictionary<object, object> ToDictionary()
        {
            var dictionary = new Dictionary<object, object>();

            for (var i = 0; i < KeyReference.Count; i++)
            {
                int keyRef = KeyReference[i], objectRef = ObjectReference[i];

                if (keyRef < 0 || keyRef >= ObjectTable.Count || (ObjectTable[keyRef] != null && ObjectTable[keyRef].Value == this)
                               || objectRef < 0 || objectRef >= ObjectTable.Count
                               || (ObjectTable[objectRef] != null && ObjectTable[objectRef].Value == this)) continue;
                object keyValue = ObjectTable[keyRef] == null ? null : ObjectTable[keyRef].Value, 
                       objectValue = ObjectTable[objectRef] == null ? null : ObjectTable[objectRef].Value;
                var innerDict = objectValue as BinaryPlistDictionary;

                if (innerDict != null)
                    objectValue = innerDict.ToDictionary();
                else
                {
                    var innerArray = objectValue as BinaryPlistArray;

                    if (innerArray != null)
                        objectValue = innerArray.ToArray();
                }

                dictionary[keyValue] = objectValue;
            }

            return dictionary;
        }

        /// <summary>
        ///     Returns the string representation of this instance.
        /// </summary>
        /// <returns>This instance's string representation.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("{");
            
            for (var i = 0; i < KeyReference.Count; i++)
            {
                if (i > 0)
                    sb.Append(",");

                int keyRef = KeyReference[i], objectRef = ObjectReference[i];

                if (keyRef < 0 || keyRef >= ObjectTable.Count)
                    sb.Append("#" + keyRef);
                else if (ObjectTable[keyRef] != null && ObjectTable[keyRef].Value == this)
                    sb.Append("*" + keyRef);
                else
                    sb.Append(ObjectTable[keyRef]);

                sb.Append(":");

                if (objectRef < 0 || objectRef >= ObjectTable.Count)
                    sb.Append("#" + objectRef);
                else if (ObjectTable[objectRef] != null && ObjectTable[objectRef].Value == this)
                    sb.Append("*" + objectRef);
                else
                    sb.Append(ObjectTable[objectRef]);
            }

            return sb + "}";
        }
    }
}