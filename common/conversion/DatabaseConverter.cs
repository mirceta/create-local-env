using System;
using System.Data;
using System.Reflection;

namespace si.birokrat.next.common.conversion {
    public static class DatabaseConverter {
        public static string ToString(object data, string value = "") {
            return data == DBNull.Value ? value : Convert.ToString(data);
        }

        public static object FromString(string data) {
            return string.IsNullOrEmpty(data) ? DBNull.Value : (object)data;
        }

        public static bool ToBoolean(object data, bool value = false) {
            return data == DBNull.Value ? value : Convert.ToBoolean(data);
        }

        public static object FromBoolean(bool data) {
            return data;
        }

        public static byte ToByte(object data, byte value = 0) {
            return data == DBNull.Value ? value : Convert.ToByte(data);
        }

        public static object FromByte(byte data) {
            return data;
        }

        public static short ToShort(object data, short value = 0) {
            return data == DBNull.Value ? value : Convert.ToInt16(data);
        }

        public static object FromShort(short data) {
            return data;
        }

        public static int ToInteger(object data, int value = 0) {
            return data == DBNull.Value ? value : Convert.ToInt32(data);
        }

        public static object FromInteger(int data) {
            return data;
        }

        public static long ToLong(object data, long value = 0) {
            return data == DBNull.Value ? value : Convert.ToInt64(data);
        }

        public static object FromLong(long data) {
            return data;
        }

        public static float ToFloat(object data, float value = 0) {
            return data == DBNull.Value ? value : Convert.ToSingle(data);
        }

        public static object FromFloat(object data) {
            return data;
        }

        public static double ToDouble(object data, double value = 0) {
            return data == DBNull.Value ? value : Convert.ToDouble(data);
        }

        public static object FromDouble(object data) {
            return data;
        }

        public static decimal ToDecimal(object data, decimal value = 0) {
            return data == DBNull.Value ? value : Convert.ToDecimal(data);
        }

        public static object FromDecimal(decimal data) {
            return data;
        }

        public static DateTime ToDateTime(object data, DateTime? value = null) {
            return data == DBNull.Value ? (value == null ? DateTime.MinValue : (DateTime)value) : Convert.ToDateTime(data);
        }

        public static object FromDateTime(DateTime data) {
            return data == null ? DBNull.Value : (object)data;
        }

        public static DateTimeOffset ToDateTimeOffset(object data, DateTimeOffset? value = null) {
            return data == DBNull.Value ? (value == null ? DateTimeOffset.MinValue : (DateTimeOffset)value) : (DateTimeOffset)data;
        }

        public static object FromDateTimeOffset(DateTimeOffset data) {
            return data == null ? DBNull.Value : (object)data;
        }

        public static Guid ToGuid(object data, Guid? value = null) {
            return data == DBNull.Value ? (value == null ? Guid.Empty : (Guid)value) : (Guid)data;
        }

        public static object FromGuid(Guid data) {
            return data == null ? DBNull.Value : (object)data;
        }

        public static ulong TimestampToLong(object data) {
            if (data != DBNull.Value) {
                byte[] byteData = (byte[])data;
                Array.Reverse(byteData);
                return BitConverter.ToUInt64(byteData, 0);
            }
            return 0;
        }

        public static bool ObjectFromDataRow(object data, DataRow row, bool includeUnderscores = false) {
            bool result = true;
            Type dataType = data.GetType();
            PropertyInfo[] propertyInfo = dataType.GetProperties();
            foreach (PropertyInfo property in propertyInfo) {
                Type propertyType = property.PropertyType;
                string name = property.Name;
                if (name.StartsWith("_") && !includeUnderscores) {
                    continue;
                }
                if (!row.Table.Columns.Contains(name)) {
                    continue;
                }
                if (name.StartsWith("sync_ts")) {
                    property.SetValue(data, TimestampToLong(row[name]), null);
                    continue;
                }
                if (propertyType == typeof(string))
                    property.SetValue(data, ToString(row[name]), null);
                else if (propertyType == typeof(bool))
                    property.SetValue(data, ToBoolean(row[name]), null);
                else if (propertyType == typeof(Byte))
                    property.SetValue(data, ToByte(row[name]), null);
                else if (propertyType == typeof(Byte?))
                    property.SetValue(data, ToByte(row[name]), null);
                else if (propertyType == typeof(Int16))
                    property.SetValue(data, ToShort(row[name]), null);
                else if (propertyType == typeof(Int16?))
                    property.SetValue(data, ToShort(row[name]), null);
                else if (propertyType == typeof(Int32))
                    property.SetValue(data, ToInteger(row[name]), null);
                else if (propertyType == typeof(Int32?))
                    property.SetValue(data, ToInteger(row[name]), null);
                else if (propertyType == typeof(Int64))
                    property.SetValue(data, ToLong(row[name]), null);
                else if (propertyType == typeof(Int64?))
                    property.SetValue(data, ToLong(row[name]), null);
                else if (propertyType == typeof(Single))
                    property.SetValue(data, ToFloat(row[name]), null);
                else if (propertyType == typeof(Single?))
                    property.SetValue(data, ToFloat(row[name]), null);
                else if (propertyType == typeof(Double))
                    property.SetValue(data, ToDouble(row[name]), null);
                else if (propertyType == typeof(Double?))
                    property.SetValue(data, ToDouble(row[name]), null);
                else if (propertyType == typeof(Decimal))
                    property.SetValue(data, ToDecimal(row[name]), null);
                else if (propertyType == typeof(Decimal?))
                    property.SetValue(data, ToDecimal(row[name]), null);
                else if (propertyType == typeof(DateTime))
                    property.SetValue(data, ToDateTime(row[name]), null);
                else if (propertyType == typeof(DateTime?))
                    property.SetValue(data, ToDateTime(row[name]), null);
                else if (propertyType == typeof(DateTimeOffset))
                    property.SetValue(data, ToDateTimeOffset(row[name]), null);
                else if (propertyType == typeof(DateTimeOffset?))
                    property.SetValue(data, ToDateTimeOffset(row[name]), null);
                else if (propertyType == typeof(Guid))
                    property.SetValue(data, ToGuid(row[name]), null);
                else
                    throw new Exception("ObjectFromDataRow type not supported!");
            }
            return result;
        }
    }
}
