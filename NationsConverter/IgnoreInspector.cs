using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.TypeInspectors;

namespace NationsConverter
{
    public class IgnoreInspector : TypeInspectorSkeleton
    {
        private readonly ITypeInspector _innerTypeDescriptor;

        public IgnoreInspector(ITypeInspector innerTypeDescriptor)
        {
            _innerTypeDescriptor = innerTypeDescriptor;
        }

        public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object container)
        {
            var props = _innerTypeDescriptor.GetProperties(type, container);
            props = props.Where(p => p.Type != typeof(Stream) && p.Type.BaseType != typeof(Stream) && p.Type != typeof(byte[]) && p.GetCustomAttribute<IgnoreDataMemberAttribute>() == null);
            return props;
        }
    }
}
