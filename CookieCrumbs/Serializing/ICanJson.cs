using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CookieCrumbs.Serializing
{
    public interface ICanJson
    {
        public string GetTargetIdentifier(SerializationEngine engine);

        public Dictionary<string, object> ToCompoundableDictionary(SerializationEngine engine)
        {
            Dictionary<string, object> data = new();
            foreach (var p in GetType().GetProperties())
            {
                data.Add(p.Name, p.GetValue(this)!);
            }
            return data;
        }

        public void FromCompound(SerializationEngine engine, Dictionary<string, object> dict)
        {
            foreach (var property in GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!property.CanWrite || !dict.ContainsKey(property.Name))
                    continue;

                var value = dict[property.Name];
                try
                {
                    var convertedValue = ConvertValue(property.PropertyType, value);
                    property.SetValue(this, convertedValue);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to set property '{property.Name}': {ex.Message}");
                }
            }
        }


        private object? ConvertValue(Type targetType, object value)
        {
            if (value == null)
            {
                return targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null
                    ? Activator.CreateInstance(targetType) // Default value for non-nullable types
                    : null; // Null for reference or nullable types
            }

            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            // Handle dictionary types
            if (underlyingType.IsGenericType && typeof(IDictionary).IsAssignableFrom(underlyingType))
            {
                var keyType = underlyingType.GetGenericArguments()[0];
                var valueType = underlyingType.GetGenericArguments()[1];

                if (value is IDictionary sourceDict)
                {
                    var convertedDict = Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType)) as IDictionary;
                    foreach (DictionaryEntry entry in sourceDict)
                    {
                        var convertedKey = ConvertValue(keyType, entry.Key);
                        var convertedValue = ConvertValue(valueType, entry.Value);
                        convertedDict?.Add(convertedKey, convertedValue);
                    }
                    return convertedDict;
                }
            }

            // Handle list types
            if (underlyingType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(underlyingType))
            {
                var elementType = underlyingType.GetGenericArguments().First();

                if (value is IEnumerable sourceList)
                {
                    var convertedList = Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType)) as IList;
                    foreach (var item in sourceList)
                    {
                        convertedList?.Add(ConvertValue(elementType, item));
                    }
                    return convertedList;
                }
            }

            // Handle direct conversions
            if (underlyingType.IsEnum)
            {
                return Enum.Parse(underlyingType, value.ToString() ?? "", ignoreCase: true);
            }

            if (underlyingType == typeof(string)) return value.ToString();
            if (underlyingType == typeof(int)) return int.TryParse(value.ToString(), out var i) ? i : 0;
            if (underlyingType == typeof(double)) return double.TryParse(value.ToString(), out var d) ? d : 0.0;
            if (underlyingType == typeof(bool)) return bool.TryParse(value.ToString(), out var b) && b;

            // Attempt generic conversion
            return Convert.ChangeType(value, underlyingType);
        }
    }


}

