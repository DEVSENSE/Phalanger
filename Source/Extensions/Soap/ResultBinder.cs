using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PHP.Core;
using System.Reflection;
using System.Xml.Serialization;

namespace PHP.Library.Soap
{
    /// <summary>
    /// Transforms objects returned by SOAP service into PHP-like return argument format
    /// </summary>
    internal class ResultBinder
    {

        private static stdClass WrapToStdClass(object obj, string name)
        {
            var runtimeFields = new PhpArray(1);
            runtimeFields[name] = obj;

            return new stdClass()
            {
                RuntimeFields = runtimeFields
            };
        }

        /// <summary>
        /// Binds the result object to PHP-like SOAP return argument format
        /// </summary>
        /// <param name="graph">The object (graph) to bind.</param>
        /// <param name="functionName">Name of the SOAP method</param>
        /// <param name="wrapResult">Wrap result to stdClass</param>
        internal static object BindResult(object graph, string functionName, bool wrapResult)
        {
            //I can also just return CLR type and wrap it with PHP.Core.Reflection.ClrObject.WrapDynamic
            object res = Bind(graph);

            if (wrapResult)
                return WrapToStdClass(res, functionName + "Result");
            else
                return res;
        }

        private static object BindEnum(object obj, Type type)
        {
            return Enum.GetName(type, obj);
        }


        private static object BindObject(object obj, Type type)
        {
            FieldInfo[] fi = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            var runtimeFields = new PhpArray(fi.Length);
            object value;
            bool specified = true;
            FieldInfo field;

            for (int i = 0; i < fi.Length; ++i)
            {
                field = fi[i];

                specified = true;

                if (i + 1 < fi.Length && Attribute.IsDefined(fi[i + 1], typeof(XmlIgnoreAttribute)))
                {
                    value = fi[i + 1].GetValue(obj);
                    if (value == null)
                        specified = false;
                    else
                        specified = (bool)value;

                    i++;
                }

                if (specified)
                {
                    value = Bind(field.GetValue(obj), field);
                    if (value != null)
                        runtimeFields.Add(field.Name, value);
                }
            }

            return new stdClass()
            {
                RuntimeFields = runtimeFields
            };
        }

        private static string GetArrayItemTypeName(Type type, FieldInfo fi)
        {
            if (fi == null)
                return "item";

            object[] attr = fi.GetCustomAttributes(false);

            for (int i = 0; i < attr.Length; ++i)
            {
                if (attr[i].GetType() == typeof(XmlArrayItemAttribute))
                {
                    XmlArrayItemAttribute arrayItemAttr = (XmlArrayItemAttribute)attr[i];
                    if (!String.IsNullOrEmpty(arrayItemAttr.ElementName))
                        return arrayItemAttr.ElementName;
                    else
                        return type.GetElementType().Name;
                }
                else if (attr[i].GetType() == typeof(XmlArrayAttribute))
                {
                    return type.GetElementType().Name;
                }
            }

            return null;
        }

        private static object BindArray(object obj, Type type, FieldInfo targetFieldInfo)
        {
            Array array = (Array)obj;
            object res;
            string elementName;

            elementName = GetArrayItemTypeName(type, targetFieldInfo);

            switch (array.Length)
            {
                case 0:
                    return new stdClass();

                case 1:
                    res = Bind(array.GetValue(0));
                    break;

                default:

                    //array.Length > 1
                    PhpArray result = new PhpArray(array.Length);

                    for (int i = 0; i < array.Length; ++i)
                    {
                        result[i] = Bind(array.GetValue(i));
                    }

                    res = result;
                    break;
            }

            if (elementName != null)
                return WrapToStdClass(res, elementName);
            else
                return res;
        }

        private static object Bind(object graph, FieldInfo targetFieldInfo = null)
        {
            if (graph == null)
                return null;

            Type type = graph.GetType();

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Decimal:
                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Char:
                case TypeCode.String:

                    if (type.IsEnum)
                        return BindEnum(graph, type);


                    return PHP.Core.Convert.ClrLiteralToPhpLiteral(graph);

                case TypeCode.DateTime:

                    DateTime dt = (DateTime)graph;

                    if (dt.TimeOfDay == TimeSpan.Zero)
                         return dt.ToString("yyyy-MM-dd");

                    return dt.ToString("yyyy-MM-ddTHH:mm:ss.fffffffzzz");

                case TypeCode.Object:
                    {
                        if (type.IsArray)
                            return BindArray(graph, type, targetFieldInfo);

                        return BindObject(graph, type);

                    }

                default:
                    Debug.Fail("Unknown type");
                    return null;
            }
        }
    }
}
