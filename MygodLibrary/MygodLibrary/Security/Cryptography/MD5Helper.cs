using System.Security.Cryptography;
using System.Text;

namespace Mygod.Security.Cryptography
{
    /// <summary>
    /// 提供 MD5 计算的类。
    /// </summary>
    public static class MD5Helper
    {
        private static readonly MD5CryptoServiceProvider Provider = new MD5CryptoServiceProvider();
        /// <summary>
        /// 计算字符串的 MD5 值。
        /// </summary>
        /// <param name="stuff">要计算的字符串。</param>
        /// <returns>返回其 MD5 值，在正常情况下返回 32 位十六进制字符。</returns>
        public static string CalculateMD5(string stuff)
        {
            var b = Provider.ComputeHash(Encoding.UTF8.GetBytes(stuff));
            var builder = new StringBuilder();
            foreach (var t in b)
            {
                builder.Append(DecToHex((t & 240) >> 4));
                builder.Append(DecToHex(t & 15));
            }
            return builder.ToString();
        }
        private static char DecToHex(int v)
        {
            return (char) ((v < 10 ? 48 : 87) + v);
        }
    }
}
