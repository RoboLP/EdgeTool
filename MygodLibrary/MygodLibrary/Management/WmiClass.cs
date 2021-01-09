using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Management;

namespace Mygod.Management
{
    /// <summary>
    /// Proxy class to use WMI objects through dynamic .NET objects
    /// Based on: http://www.codeproject.com/Tips/449864/WMI-Proxy-Class-using-System-Dynamic
    /// </summary>
    public class WmiClass : DynamicObject, IDisposable
    {
        public static IEnumerable<WmiClass> Query(ManagementObjectSearcher searcher)
        {
            return from ManagementObject obj in searcher.Get() select new WmiClass(obj);
        }
        public static IEnumerable<WmiClass> Query(SelectQuery query)
        {
            using (var searcher = new ManagementObjectSearcher(query)) return Query(searcher);
        }
        public static IEnumerable<WmiClass> Query(ManagementScope scope, ObjectQuery query)
        {
            using (var searcher = new ManagementObjectSearcher(scope, query)) return Query(searcher);
        }

        public ManagementObject Object { get; }
        
        private readonly string[] members;
        
        public WmiClass(ManagementObject obj)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            Object = obj;
            try
            {
                using (var c = new ManagementClass(obj.ClassPath.ClassName))
                    members = c.Methods.OfType<MethodData>().Select(item => item.Name)
                        .Concat(c.Properties.OfType<PropertyData>().Select(item => item.Name)).ToArray();
            }
            catch (Exception)
            {
                // ignored
            }
        }
        
        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return members;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            try
            {
                result = Object.GetPropertyValue(binder.Name);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            try
            {
                result = Object.InvokeMethod(binder.Name, args);
                return true;
            }
            catch
            {
                result = null;
                return false;
            }
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            try
            {
                Object.SetPropertyValue(binder.Name, value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public override string ToString()
        {
            return Object.Path.ToString();
        }

        public void Dispose()
        {
            Object.Dispose();
        }
    }
}
