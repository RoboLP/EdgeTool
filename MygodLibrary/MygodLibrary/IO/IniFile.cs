#pragma warning disable 612
namespace Mygod.IO
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Media;

    static class SafeNativeMethods
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern uint GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, 
                                                            uint size, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        internal static extern uint GetPrivateProfileSectionNames(IntPtr lpszReturnBuffer, uint nSize, string lpFileName);
    }
    /// <summary>
    /// 提供Ini文件的操作类。
    /// </summary>
    [Obsolete]
    public class IniFile : IEnumerable<IniSection>
    {
        #region 参数

        public string FilePath { get; }
        private readonly uint returnStringLong;

        #endregion

        #region 自定义方法

        /// <summary>
        /// 创建新的IniFile。
        /// </summary>
        /// <param name="filePath">Ini文件路径。</param>
        /// <param name="stringLong">文本长度，如果超过会被截断。</param>
        public IniFile(string filePath, uint stringLong = 32767)
        {
            FilePath = Path.GetFullPath(filePath);
            returnStringLong = stringLong;
        }

        /// <summary>
        /// 读取Ini数据。
        /// </summary>
        /// <param name="section">Ini节名。</param>
        /// <param name="key">Ini键名。</param>
        /// <param name="noText">当指定键不存在时返回值。</param>
        /// <returns></returns>
        public string ReadIniData(string section, string key, string noText = null)
        {
            if (File.Exists(FilePath))
            {
                var temp = new StringBuilder((int)returnStringLong);
                SafeNativeMethods.GetPrivateProfileString(section, key, noText, temp, returnStringLong, FilePath);
                return temp.ToString();
            }
            return noText;
        }

        /// <summary>
        /// 写出Ini数据。
        /// </summary>
        /// <param name="section">Ini节名。</param>
        /// <param name="key">Ini键名。</param>
        /// <param name="value">要写入的值。</param>
        public void WriteIniData(string section, string key, string value)
        {
            Task.Factory.StartNew(() => SafeNativeMethods.WritePrivateProfileString(section, key, value, FilePath));
        }

        #endregion

        IEnumerator<IniSection> IEnumerable<IniSection>.GetEnumerator()
        {
            var pReturnedString = Marshal.AllocCoTaskMem((int) returnStringLong);
            var bytesReturned = SafeNativeMethods.GetPrivateProfileSectionNames(pReturnedString, returnStringLong, FilePath);
            if (bytesReturned == 0)
            {
                Marshal.FreeCoTaskMem(pReturnedString);
                yield break;
            }
            var local = Marshal.PtrToStringAnsi(pReturnedString, (int)bytesReturned);
            Marshal.FreeCoTaskMem(pReturnedString);
            foreach (var name in local.Substring(0, local.Length - 1).Split('\0')) yield return new IniSection(this, name);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<IniSection>) this).GetEnumerator();
        }

        public IniSection this[string name] => new IniSection(this, name);
    }

    /// <summary>
    /// 提供Ini节的操作类。
    /// </summary>
    public class IniSection
    {
        /// <summary>
        /// 创建新的Ini节。
        /// </summary>
        /// <param name="iniFile">属于的Ini文件。</param>
        /// <param name="sectionName">节名。</param>
        protected internal IniSection(IniFile iniFile, string sectionName)
        {
            IniFile = iniFile;
            Name = sectionName;
        }

        /// <summary>
        /// 获取属于的Ini文件。
        /// </summary>
        public IniFile IniFile { get; }
        /// <summary>
        /// 获取当前节节名。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 从该文件中删除此节。
        /// </summary>
        public void Remove()
        {
            SafeNativeMethods.WritePrivateProfileString(Name, null, null, IniFile.FilePath);
        }
    }

    /// <summary>
    /// 定义特定的方法获得或设置值。
    /// </summary>
    /// <typeparam name="T">要获得的类型。</typeparam>
    public interface IIniData<T>
    {
        /// <summary>
        /// 获得ini文件中的值。
        /// </summary>
        /// <returns>返回获得的值。</returns>
        T Get();
        /// <summary>
        /// 设置ini文件中的值。
        /// </summary>
        /// <param name="value">要设置的值。</param>
        void Set(T value);

        void ResetToDefault();

        event EventHandler DataChanged;
    }
    /// <summary>
    /// 定义带参数的特定方法获得或设置值。
    /// </summary>
    /// <typeparam name="TKey">要获得的类型。</typeparam>
    /// <typeparam name="TResult">要获得的类型。</typeparam>
    public interface IIniDataWithParam<in TKey, TResult>
    {
        /// <summary>
        /// 获得ini文件中的值。
        /// </summary>
        /// <param name="key">参数。</param>
        /// <returns>返回获得的值。</returns>
        TResult Get(TKey key);
        /// <summary>
        /// 设置ini文件中的值。
        /// </summary>
        /// <param name="key">参数。</param>
        /// <param name="value">要设置的值。</param>
        void Set(TKey key, TResult value);

        IIniData<TResult> GetData(TKey key);
    }

    /// <summary>
    /// 从ini参数中获得字符串值。
    /// </summary>
    public class StringData : IIniData<string>
    {
        /// <summary>
        /// 构建一个 StringData 实例。
        /// </summary>
        /// <param name="inisection"></param>
        /// <param name="key"></param>
        /// <param name="defaultvalue"></param>
        public StringData(IniSection inisection, string key, string defaultvalue = null)
        {
            section = inisection;
            dataKey = key;
            defaultValue = defaultvalue;
            Get();
        }

        private readonly IniSection section;
        private readonly string defaultValue;
        private string dataKey;

        /// <summary>
        /// Key resetter.
        /// </summary>
        public string DataKey { get { return dataKey; } set { dataKey = value; requested = false; } }

        private string requestedValue;
        private bool requested;

        public string Get()
        {
            if (!requested)
            {
                requested = true;
                requestedValue = section.IniFile.ReadIniData(section.Name, dataKey, defaultValue);
            }
            return requestedValue;
        }
        public void Set(string value)
        {
            if (requestedValue == value) return;
            requested = true;
            requestedValue = value;
            section.IniFile.WriteIniData(section.Name, dataKey, value);
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ResetToDefault()
        {
            Set(defaultValue);
        }

        public event EventHandler DataChanged;
    }
    public class Int32Data : StringData, IIniData<int>
    {
        public Int32Data(IniSection section, string key, int value) : base(section, key, value.ToString(CultureInfo.InvariantCulture))
        {
            Get();
        }

        private int requestedValue;

        public new int Get()
        {
            return requestedValue = int.Parse(base.Get(), CultureInfo.InvariantCulture);
        }
        public void Set(int value)
        {
            if (requestedValue == value) return;
            requestedValue = value;
            Set(value.ToString(CultureInfo.InvariantCulture));
        }
    }
    public class Int64HexData : StringData, IIniData<long>
    {
        public Int64HexData(IniSection section, string key, long value = 0)
            : base(section, key, value.ToString("X", CultureInfo.InvariantCulture))
        {
            Get();
        }

        private long requestedValue;

        public new long Get()
        {
            return requestedValue = long.Parse(base.Get(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }
        public void Set(long value)
        {
            if (requestedValue == value) return;
            requestedValue = value;
            Set(value.ToString("X", CultureInfo.InvariantCulture));
        }
    }
    public class DoubleData : StringData, IIniData<double>
    {
        public DoubleData(IniSection section, string key, double value) : base(section, key, value.ToString(CultureInfo.InvariantCulture))
        {
            Get();
        }

        private double requestedValue;

        public new double Get()
        {
            return requestedValue = double.Parse(base.Get(), CultureInfo.InvariantCulture);
        }
        public void Set(double value)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (requestedValue == value) return;
            // ReSharper restore CompareOfFloatsByEqualityOperator
            requestedValue = value;
            Set(value.ToString(CultureInfo.InvariantCulture));
        }
    }
    public class BooleanData : StringData, IIniData<bool>
    {
        public BooleanData(IniSection inisection, string key, bool defaultvalue) : base(inisection, key, defaultvalue.ToString(CultureInfo.InvariantCulture))
        {
            Get();
        }

        private bool requestedValue;

        public new bool Get()
        {
            return requestedValue = bool.Parse(base.Get());
        }
        public void Set(bool value)
        {
            if (value == requestedValue) return;
            requestedValue = value;
            Set(value.ToString(CultureInfo.InvariantCulture));
        }
    }
    public class YesNoData : StringData, IIniData<bool>
    {
        public YesNoData(IniSection inisection, string key, bool defaultvalue = false)
            : base(inisection, key, defaultvalue ? "Yes" : "No")
        {
            Get();
        }

        private bool requestedValue;
        public new bool Get()
        {
            return requestedValue = (base.Get() ?? string.Empty).ToLowerInvariant() == "yes";
        }
        public void Set(bool value)
        {
            if (requestedValue == value) return;
            requestedValue = value;
            Set(value ? "Yes" : "No");
        }
    }
    public class PointData : StringData, IIniData<Point>
    {
        public PointData(IniSection section, string key, Point value)
            : base(section, key, value.ToString(CultureInfo.InvariantCulture))
        {
        }

        private Point requestedValue;

        public new Point Get()
        {
            return Point.Parse(base.Get());
        }
        public void Set(Point value)
        {
            if (requestedValue == value) return;
            requestedValue = value;
            Set(value.ToString(CultureInfo.InvariantCulture));
        }
    }
    public class ColorData : StringData, IIniData<Color>
    {
        public ColorData(IniSection section, string key, Color value)
            : base(section, key, value.ToString(CultureInfo.InvariantCulture))
        {
            defaultValue = value;
        }

        private readonly Color defaultValue;
        private Color requestedValue;

        public new Color Get()
        {
            var result = ColorConverter.ConvertFromString(base.Get());
            return (Color) (result ?? defaultValue);
        }
        public void Set(Color value)
        {
            if (requestedValue == value) return;
            requestedValue = value;
            Set(value.ToString(CultureInfo.InvariantCulture));
        }
    }

    public class StringListData : IniSection, IIniData<List<string>>
    {
        public StringListData(IniFile iniFile, string sectionName) : base(iniFile, sectionName)
        {
            data = new StringData(this, null);
            countData = new Int32Data(this, "Count", -1);
            Get();
        }

        private readonly StringData data;
        private readonly Int32Data countData;

        private List<string> requestedValue;
        private bool requested;

        public List<string> Get()
        {
            if (!requested)
            {
                requested = true;
                requestedValue = new List<string>();
                var count = countData.Get();
                for (var i = 0; i < count; i++)
                {
                    data.DataKey = i.ToString(CultureInfo.InvariantCulture);
                    requestedValue.Add(data.Get());
                }
            }
            return requestedValue.ToList(); // create a copy
        }
        public void Set(List<string> value)
        {
            if (requestedValue == value) return;
            requestedValue = value.ToList();    // create a copy
            var count = value.Count;
            countData.Set(count);
            for (var i = 0; i < count; i++)
            {
                data.DataKey = i.ToString(CultureInfo.InvariantCulture);
                data.Set(value[i]);
            }
            DataChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ResetToDefault()
        {
            Set(new List<string>());
        }

        public event EventHandler DataChanged;
    }
    public class DoubleListData : StringListData, IIniData<List<double>>
    {
        public DoubleListData(IniFile iniFile, string sectionName) : base(iniFile, sectionName)
        {
        }
        
        private List<double> requestedValue;
        private bool requested;

        public new List<double> Get()
        {
            if (!requested)
            {
                requestedValue = base.Get().Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToList();
                requested = true;
            }
            return requestedValue;
        }
        public void Set(List<double> value)
        {
            Set(value.Select(i => i.ToString(CultureInfo.InvariantCulture)).ToList());
        }
    }

    public class StringDictionaryData : IniSection, IIniDataWithParam<string, string>
    {
        public StringDictionaryData(IniFile iniFile, string sectionName, string defaultValue = null)
            : base(iniFile, sectionName)
        {
            Value = defaultValue;
        }

        protected readonly string Value;
        private readonly Dictionary<string, StringData> dictionary = new Dictionary<string, StringData>();

        public string Get(string key)
        {
            if (!dictionary.ContainsKey(key)) dictionary.Add(key, new StringData(this, key, Value));
            return dictionary[key].Get();
        }
        public void Set(string key, string data)
        {
            if (!dictionary.ContainsKey(key)) dictionary.Add(key, new StringData(this, key, Value));
            dictionary[key].Set(data);
        }

        public IIniData<string> GetData(string key)
        {
            if (dictionary.ContainsKey(key)) dictionary.Add(key, new StringData(this, key, Value));
            return dictionary[key];
        }
    }

    public class Int32DoubleDictionaryData : StringDictionaryData, IIniDataWithParam<int, double>
    {
        public Int32DoubleDictionaryData(IniFile iniFile, string sectionName, double value = double.NaN)
            : base(iniFile, sectionName, value.ToString(CultureInfo.InvariantCulture))
        {
        }

        public double Get(int key)
        {
            return double.Parse(Get(key.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
        }
        public void Set(int key, double data)
        {
            Set(key.ToString(CultureInfo.InvariantCulture), data.ToString(CultureInfo.InvariantCulture));
        }

        public IIniData<double> GetData(int key)
        {
            return new DoubleData(this, key.ToString(CultureInfo.InvariantCulture), double.Parse(Value, CultureInfo.InvariantCulture));
        }
    }
}
