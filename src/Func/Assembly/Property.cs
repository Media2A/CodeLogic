using System.Linq.Expressions;
using System.Reflection;

namespace CodeLogic
{
    public partial class CodeLogic_Funcs
    {
        public static Dictionary<string, object> GetCustomAttributes(PropertyInfo property)
        {
            var attributes = new Dictionary<string, object>();

            var customAttributes = property.GetCustomAttributes(true);
            foreach (var attribute in customAttributes)
            {
                attributes.Add(attribute.GetType().Name, attribute);
            }

            return attributes;
        }

        /// <summary>
        /// Returns a dictionary with the Assembly info
        /// </summary>
        /// <param name="File"></param>
        /// <returns></returns>
        public static string GetPropertyName<T>(Expression<Func<T>> propertyLambda)
        {
            var me = propertyLambda.Body as MemberExpression;

            if (me == null)
            {
                throw new ArgumentException("You must pass a lambda of the form: '() => Class.Property' or '() => object.Property'");
            }

            return me.Member.Name;
        }
        public static Dictionary<string, object> GetPropertyAttributes(PropertyInfo property)
        {
            Dictionary<string, object> attribs = new Dictionary<string, object>();
            // look for attributes that takes one constructor argument
            foreach (CustomAttributeData attribData in property.GetCustomAttributesData())
            {

                if (attribData.ConstructorArguments.Count == 1)
                {
                    string typeName = attribData.Constructor.DeclaringType.Name;
                    if (typeName.EndsWith("Attribute")) typeName = typeName.Substring(0, typeName.Length - 9);
                    attribs[typeName] = attribData.ConstructorArguments[0].Value;
                }

            }
            return attribs;
        }
        public static Dictionary<string, object> ObjectToDictionary(object obj)
        {
            Dictionary<string, object> ret = new Dictionary<string, object>();

            foreach (PropertyInfo prop in obj.GetType().GetProperties())
            {
                string propName = prop.Name;
                var val = obj.GetType().GetProperty(propName).GetValue(obj, null);
                if (val != null)
                {
                    ret.Add(propName, val);
                }
                else
                {
                    ret.Add(propName, null);
                }
            }

            return ret;
        }

        public static  object GetPropertyValue(object obj, string propertyName)
        {
            Type objectType = obj.GetType();
            PropertyInfo property = objectType.GetProperty(propertyName);
            return property.GetValue(obj);
        }
    }
}