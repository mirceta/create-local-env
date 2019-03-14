using System;
using System.ComponentModel;

namespace si.birokrat.next.common.reflection {
    public static class Property {
        public static bool Has(Type type, string propertyName) {
            return Get(type, propertyName) != null;
        }

        public static object GetValue<T>(T item, string propertyName) {
            return Get(typeof(T), propertyName).GetValue(item);
        }

        public static bool HasType(Type objectType, Type propertyType, string propertyName) {
            return Get(objectType, propertyName).PropertyType == propertyType;
        }

        private static PropertyDescriptor Get(Type type, string propertyName) {
            return TypeDescriptor.GetProperties(type).Find(propertyName, true);
        }
    }
}
