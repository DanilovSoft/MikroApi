using System;

namespace DanilovSoft.MikroApi;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class MikroTikPropertyAttribute : Attribute
{
    public MikroTikPropertyAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public string? EncodingName { get; set; }
}
