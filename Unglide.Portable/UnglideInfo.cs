using System;
using System.Linq;
using System.Reflection;

namespace Unglide
{
    internal class UnglideInfo
    {
        static UnglideInfo()
        {
            NumericTypes = new[] {
                typeof(Int16),
                typeof(Int32),
                typeof(Int64),
                typeof(UInt16),
                typeof(UInt32),
                typeof(UInt64),
                typeof(Single),
                typeof(Double)
            };
        }

        private static readonly Type[] NumericTypes;

        private readonly FieldInfo _field;
        private readonly PropertyInfo _prop;
        private readonly bool _isNumeric;

        private readonly object _target;

        public string Name { get; private set; }

        public object Value
        {
            get { return _field != null ? _field.GetValue(_target) : _prop.GetValue(_target, null); }
            set
            {

                if (_isNumeric)
                {
                    Type type = null;
                    if (_field != null) type = _field.FieldType;
                    if (_prop != null) type = _prop.PropertyType;
                    if (AnyEquals(type, NumericTypes))
                        value = Convert.ChangeType(value, type);
                }

                if (_field != null)
                    _field.SetValue(_target, value);
                else
                    _prop?.SetValue(_target, value, null);
            }
        }

        public UnglideInfo(object target, string property, bool writeRequired = true)
        {
            _target = target;
            Name = property;

            Type targetType;
            if (IsType(target))
            {
                targetType = (Type)target;
            }
            else
            {
                targetType = target.GetType();
            }

            _field = targetType.GetTypeInfo().DeclaredFields.FirstOrDefault(f =>
                string.Equals(property, f.Name) && !f.IsStatic);

            _prop = writeRequired
                ? targetType.GetTypeInfo().DeclaredProperties.FirstOrDefault(p =>
                    string.Equals(property, p.Name) && !p.GetMethod.IsStatic && p.CanRead && p.CanWrite)
                : targetType.GetTypeInfo().DeclaredProperties.FirstOrDefault(p =>
                    string.Equals(property, p.Name) && !p.GetMethod.IsStatic && p.CanRead);

            if (_field == null)
            {
                if (_prop == null)
                {
                    //	Couldn't find either
                    throw new Exception(string.Format("Field or '{0}' property '{1}' not found on object of type {2}.",
                        writeRequired ? "read/write" : "readable",
                        property, targetType.FullName));
                }
            }

            var valueType = Value.GetType();
            _isNumeric = AnyEquals(valueType, NumericTypes);
            CheckPropertyType(valueType, property, targetType.Name);
        }

        bool IsType(object target)
        {
            var type = target.GetType();
            var baseType = typeof(Type);

            if (type == baseType)
                return true;

            var rootType = typeof(object);

            while (type != null && type != rootType)
            {
                var info = type.GetTypeInfo();
                var current = info.IsGenericType && info.IsGenericTypeDefinition ? info.GetGenericTypeDefinition() : type;
                if (baseType == current)
                    return true;
                type = info.BaseType;
            }

            return false;
        }

        private void CheckPropertyType(Type type, string prop, string targetTypeName)
        {
            if (!ValidatePropertyType(type))
            {
                throw new InvalidCastException(string.Format("Property is invalid: ({0} on {1}).", prop, targetTypeName));
            }
        }

        protected virtual bool ValidatePropertyType(Type type)
        {
            return _isNumeric;
        }

        static bool AnyEquals<T>(T value, params T[] options)
        {
            foreach (var option in options)
                if (value.Equals(option)) return true;

            return false;
        }
    }
}
