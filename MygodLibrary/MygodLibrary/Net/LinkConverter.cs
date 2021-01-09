using System;
using System.Linq;
using System.Text;

namespace Mygod.Net
{
    public static class LinkConverter
    {
        public static string Base64Encode(string str, Encoding encoding = null)
        {
            return Convert.ToBase64String((encoding ?? Encoding.Default).GetBytes(str));
        }

        public static string Base64Decode(string str, Encoding encoding = null)
        {
            return (encoding ?? Encoding.Default).GetString(Convert.FromBase64String(str));
        }

        public static string PublicEncode(string link, string linkpre, string prefix, string suffix, string name)
        {
            if (string.IsNullOrEmpty(link)) throw new ArgumentNullException(nameof(link));
            if (link.ToLowerInvariant().StartsWith(linkpre.ToLowerInvariant(), StringComparison.Ordinal))
                throw new ArgumentException("该链接已经是" + name + "下载链接。");
            return linkpre + Base64Encode(prefix + link + suffix);
        }

        public static string PublicDecode(string link, string linkpre, string prefix, string suffix, string name)
        {
            if (string.IsNullOrEmpty(link)) throw new ArgumentNullException(nameof(link));
            if (!link.ToLowerInvariant().StartsWith(linkpre.ToLowerInvariant(), StringComparison.Ordinal))
                throw new ArgumentException("该链接不是" + name + "下载链接。");
            link = link.TrimEnd('\\', '/', ' ', '\t', '\r', '\n');
            var and = link.IndexOf('&');
            if (and >= 0) link = link.Substring(0, and);
            var result = Base64Decode(link.Substring(linkpre.Length));
            return result.Substring(prefix.Length, result.Length - prefix.Length - suffix.Length);
        }

        public static string ThunderEncode(string link)
        {
            return PublicEncode(link, "thunder://", "AA", "ZZ", "迅雷");
        }

        public static string ThunderDecode(string link)
        {
            return PublicDecode(link, "thunder://", "AA", "ZZ", "迅雷");
        }

        public static string FlashGetEncode(string link)
        {
            return PublicEncode(link, "flashget://", "[FLASHGET]", "[FLASHGET]", "快车");
        }

        public static string FlashGetDecode(string link)
        {
            return PublicDecode(link, "flashget://", "[FLASHGET]", "[FLASHGET]", "快车");
        }

        public static string QQDLEncode(string link)
        {
            return PublicEncode(link, "qqdl://", string.Empty, string.Empty, "旋风");
        }

        public static string QQDLDecode(string link)
        {
            return PublicDecode(link, "qqdl://", string.Empty, string.Empty, "旋风");
        }

        public static string RayFileEncode(string link)
        {
            return PublicEncode(link, "fs2you://", string.Empty, string.Empty, "RayFile");
        }

        public static string RayFileDecode(string link)
        {
            return PublicDecode(link, "fs2you://", string.Empty, string.Empty, "RayFile");
        }

        public static string Reverse(string value)
        {
            return value.Reverse().Aggregate(string.Empty, (c, s) => c + s);
        }

        public static string Encode(LinkType target, string value)
        {
            switch (target)
            {
                case LinkType.Normal:
                    return value;
                case LinkType.Thunder:
                    return ThunderEncode(value);
                case LinkType.FlashGet:
                    return FlashGetEncode(value);
                case LinkType.QQDL:
                    return QQDLEncode(value);
                case LinkType.RayFile:
                    return RayFileEncode(value);
                default:
                    throw new ArgumentException("未知的链接格式！");
            }
        }

        public static string Decode(LinkType source, string value)
        {
            switch (source)
            {
                case LinkType.Normal:
                    return value;
                case LinkType.Thunder:
                    return ThunderDecode(value);
                case LinkType.FlashGet:
                    return FlashGetDecode(value);
                case LinkType.QQDL:
                    return QQDLDecode(value);
                case LinkType.RayFile:
                    return RayFileDecode(value);
                default:
                    throw new ArgumentException("未知的链接格式！");
            }
        }

        public static string Decode(string value)
        {
            return Decode(GetUrlType(value), value);
        }

        public static string ConvertUrl(LinkType source, LinkType target, string value)
        {
            return source == target ? value : Encode(target, Decode(source, value));
        }
        public static string ConvertUrl(LinkType target, string value)
        {
            return ConvertUrl(GetUrlType(value), target, value);
        }

        public static LinkType GetUrlType(string value)
        {
            var l = value.ToLowerInvariant();
            if (l.StartsWith("thunder://", StringComparison.Ordinal)) return LinkType.Thunder;
            if (l.StartsWith("flashget://", StringComparison.Ordinal)) return LinkType.FlashGet;
            if (l.StartsWith("qqdl://", StringComparison.Ordinal)) return LinkType.QQDL;
            return l.StartsWith("fs2you://", StringComparison.Ordinal) ? LinkType.RayFile : LinkType.Normal;
        }
    }

    public enum LinkType
    {
        Normal, Thunder, FlashGet, QQDL, RayFile
    }
}
