using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DanilovSoft.MikroApi
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public class MikroTikPropertyAttribute : Attribute
    {
        public string Name { get; }
        public string EncodingName { get; set; }

        public MikroTikPropertyAttribute(string name)
        {
            Name = name;
        }
    }
}
