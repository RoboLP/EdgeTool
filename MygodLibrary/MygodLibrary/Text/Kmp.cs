// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global

namespace Mygod.Text
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// 提供KMP匹配字符串的类。
    /// </summary>
    public class Kmp
    {
        /// <summary>
        /// 创建一个KMP类。如果要匹配多次，则创建实例比调用静态方法的执行效率高。
        /// </summary>
        /// <param name="pattern">用于匹配的字符串。</param>
        public Kmp(byte[] pattern)
        {
            Pattern = pattern;
        }

        private int[] next;
        private byte[] pattern;
        /// <summary>
        /// 获取或设置模式串，即用于匹配的字符串。
        /// </summary>
        public byte[] Pattern
        {
            get { return pattern; }
            set
            {
                if (value == null || value.Length == 0) throw new ArgumentException("模式串不能为空串！");
                pattern = value;
                next = new int[pattern.Length];
                next[0] = -1;
                if (pattern.Length <= 1) return;
                next[1] = 0;
                int i = 2, j = 0;
                while (i < pattern.Length)
                    if (pattern[i - 1] == pattern[j]) next[i++] = ++j;
                    else
                    {
                        j = next[j];
                        if (j == -1) next[i++] = ++j;
                    }
            }
        }

        /// <summary>
        /// 一个更加低级、可扩展的 KMP 搜索实现。
        /// </summary>
        public class Searcher
        {
            public Searcher(Kmp kmp)
            {
                Kmp = kmp;
            }
            public Searcher(byte[] kmp)
            {
                Kmp = new Kmp(kmp);
            }

            public Kmp Kmp { get; }
            private int i;

            public bool Enter(byte next)
            {
                if (i == -1) i++;
                else if (next == Kmp.Pattern[i])
                {
                    i++;
                    if (i >= Kmp.Pattern.Length) return true;
                }
                else i = Kmp.next[i];
                return false;
            }
        }

        /// <summary>
        /// KMP匹配字符串，效率为O(N+M)，其中N是text.Length，M是pattern.Length。
        /// </summary>
        /// <param name="text">要查找的字符串。</param>
        /// <param name="pos">开始搜索的位置。</param>
        /// <returns>返回查找到的位置，如果没有匹配则返回-1。</returns>
        public int Match(byte[] text, int pos = 0)
        {
            var i = 0;
            while (i < pattern.Length && pos < text.Length)
            {
                if (text[pos] == pattern[i])
                {
                    pos++;
                    i++;
                }
                else
                {
                    i = next[i];
                    if (i == -1)
                    {
                        pos++;
                        i++;
                    }
                }
            }
            return i < pattern.Length ? -1 : pos - i;
        }
        /// <summary>
        /// KMP匹配流，效率为O(N+M)，其中N是流的长度，M是pattern.Length。
        /// </summary>
        /// <param name="stream">要匹配的流。</param>
        /// <returns>返回是否找到匹配串，如果找到，偏移量会被设置在匹配的串开始。</returns>
        public bool Match(Stream stream)
        {
            if (!stream.CanSeek || !stream.CanRead) throw new ArgumentException("当前流不支持读取或查找功能！");
            if (stream.Position >= stream.Length) return false;
            int i = 0, n = stream.ReadByte();
            do
            {
                if (n == pattern[i])
                {
                    n = stream.ReadByte();
                    i++;
                }
                else
                {
                    i = next[i];
                    if (i == -1)
                    {
                        n = stream.ReadByte();
                        i++;
                    }
                }
            } while (i < pattern.Length && stream.Position < stream.Length);
            if (i < pattern.Length) return false;
            stream.Position -= i;
            return true;
        }
        /// <summary>
        /// KMP匹配字符串，效率为O(N+M)，其中N是text.Length，M是pattern.Length。
        /// </summary>
        /// <param name="text">要查找的字符串。</param>
        /// <returns>返回所有找到的位置。</returns>
        public IEnumerable<int> MatchAll(byte[] text)
        {
            var pos = Match(text);
            while (pos >= 0)
            {
                yield return pos;
                pos = Match(text, pos + 1);
            }
        }
        /// <summary>
        /// KMP匹配字符串，效率为O(N+M)，其中N是text.Length，M是pattern.Length。
        /// </summary>
        /// <param name="stream">要查找的流。</param>
        /// <returns>返回所有找到的位置。</returns>
        public IEnumerable<long> MatchAll(Stream stream)
        {
            while (Match(stream)) yield return stream.Position++;
        }

        /// <summary>
        /// KMP匹配字符串，效率为O(N+M)，其中N是text.Length，M是pattern.Length。
        /// </summary>
        /// <param name="text">要匹配的字符串。</param>
        /// <param name="pattern">要搜索的字符串</param>
        /// <param name="pos">起始搜索的位置。</param>
        /// <returns>返回查找到的位置，如果没有匹配则返回-1。</returns>
        public static int Match(byte[] text, byte[] pattern, int pos = 0)
        {
            return new Kmp(pattern).Match(text, pos);
        }

        /// <summary>
        /// KMP匹配流，效率为O(N+M)，其中N是流的长度，M是pattern.Length。
        /// </summary>
        /// <param name="stream">要匹配的流。</param>
        /// <param name="pattern">要匹配的字符串。</param>
        /// <returns>返回是否找到匹配串，如果找到，偏移量会被设置在匹配的串开始。</returns>
        public static bool Match(Stream stream, byte[] pattern)
        {
            return new Kmp(pattern).Match(stream);
        }
        /// <summary>
        /// KMP匹配字符串，效率为O(N+M)，其中N是text.Length，M是pattern.Length。
        /// </summary>
        /// <param name="text">要查找的字符串。</param>
        /// <param name="pattern">要匹配的字符串。</param>
        /// <returns>返回找到的所有位置。</returns>
        public static IEnumerable<int> MatchAll(byte[] text, byte[] pattern)
        {
            return new Kmp(pattern).MatchAll(text);
        }
        /// <summary>
        /// KMP匹配字符串，效率为O(N+M)，其中N是text.Length，M是pattern.Length。
        /// </summary>
        /// <param name="stream">要查找的流。</param>
        /// <param name="pattern">要匹配的字符串。</param>
        /// <returns>返回所有找到的位置。</returns>
        public static IEnumerable<long> MatchAll(Stream stream, byte[] pattern)
        {
            return new Kmp(pattern).MatchAll(stream);
        }
    }
}
