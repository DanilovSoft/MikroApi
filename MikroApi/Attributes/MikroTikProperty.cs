using System;

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
