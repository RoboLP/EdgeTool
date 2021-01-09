using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Mygod
{
    /// <summary>
    /// 辅助类。
    /// </summary>
    public static class Helper
    {
        /// <summary>
        /// 用于将错误转化为可读的字符串。
        /// </summary>
        /// <param name="e">错误。</param>
        /// <returns>错误字符串。</returns>
        public static string GetMessage(this Exception e)
        {
            var result = new StringBuilder();
            GetMessage(e, result);
            return result.ToString();
        }

        private static void GetMessage(Exception e, StringBuilder result)
        {
            while (e != null && !(e is AggregateException))
            {
                result.AppendFormat("({0}) {1}{2}{3}{2}", e.GetType(), e.Message, Environment.NewLine, e.StackTrace);
                e = e.InnerException;
            }
            var ae = e as AggregateException;
            if (ae != null) foreach (var ex in ae.InnerExceptions) GetMessage(ex, result);
        }

        /// <summary>
        /// ToString with CultureInfo.InvariantCulture.
        /// </summary>
        /// <param name="value">The object.</param>
        /// <typeparam name="T">The type.</typeparam>
        public static string ToStringInvariant<T>(this T value)
        {
            if (value == null) return null;
            var method = typeof(T).GetMethod("ToString", BindingFlags.Public, null,
                new[] {typeof(IFormatProvider)}, null);
            if (method != null && method.ReturnType == typeof(string))
                return (string) method.Invoke(value, new object[] {CultureInfo.InvariantCulture});
            return value.ToString();    // fallback to simple method
        }

        /// <summary>
        /// An implementation of the Contains member of string that takes in a 
        /// string comparison. The traditional .NET string Contains member uses 
        /// StringComparison.Ordinal.
        /// </summary>
        /// <param name="s">The string.</param>
        /// <param name="value">The string value to search for.</param>
        /// <param name="comparison">The string comparison type.</param>
        /// <returns>Returns true when the substring is found.</returns>
        public static bool Contains(this string s, string value, StringComparison comparison)
        {
            return s.IndexOf(value, comparison) >= 0;
        }

        public static void DoNothing<T>(this T _)
        {
        }

        public static string ToValidPath(this string path, bool slashes = true)
        {
            if (slashes) path = path.Replace("\\", "＼").Replace("/", "／");
            return path.Replace(":", "：").Replace("*", "＊").Replace("?", "？").Replace("\"", "＂")
                       .Replace("<", "＜").Replace(">", "＞").Replace("|", "｜").Replace("%", "％")
                       .Replace("#", "＃");      // convert ALL THOSE GODDAMN THINGS or it will definitely go wrong
        }

        public static string UrlDecode(this string str)
        {
            return str == null ? null : Uri.UnescapeDataString(str.Replace('+', ' '));
        }

        public static string UrlEncode(this string str)
        {
            return str == null ? null : Uri.EscapeDataString(str);
        }

        public static void CheckLastWin32Error()
        {
            var error = Marshal.GetLastWin32Error();
            if (error != 0) throw new Win32Exception(error);
        }

        public static string Substr(this string str, int from, int to = -1)
        {
            return from <= to ? str.Substring(from, to - from)
                              : str.Substring(to + 1, from - to).Reverse().Aggregate(string.Empty, (s, c) => s + c);
        }

        private static readonly string[]
            Units = { null, "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB", "BB", "NB", "DB", "CB" };

        public static string GetSize(long size, string bytes = "Bytes")
        {
            double n = size;
            byte i = 0;
            while (n > 1000)
            {
                n /= 1024;
                i++;
            }
            return i == 0 ? $"{size:N0} {bytes}" : $"{n:N} {Units[i]} ({size:N0} {bytes})";
        }

        public static string GetSize(double size, string bytes = "Bytes")
        {
            var n = size;
            byte i = 0;
            while (n > 1000)
            {
                n /= 1024;
                i++;
            }
            return n.ToString("N") + ' ' + (i == 0 ? bytes : Units[i] + " (" + size.ToString("N") + ' ' + bytes + ')');
        }

        public static int ReadCharExtended(this BinaryReader reader, long length)
        {
            var stream = reader.BaseStream;
            return stream.Position == length ? -1 : reader.ReadChar();
        }
    }
}
