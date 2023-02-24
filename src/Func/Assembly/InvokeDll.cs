using System.Reflection;

namespace CodeLogic
{
    public partial class CodeLogic_Funcs
    {
        public static object GetObjectInvokeDll(string invFileName, string invType, string invMethodName)
        {
            try
            {
                var file = AppDomain.CurrentDomain.BaseDirectory + invFileName;
                if (!File.Exists(file))
                {
                    throw new Exception("File not found");
                }
                Assembly assembly = Assembly.LoadFrom(file);

                Type type = assembly.GetType(invType);

                object o = Activator.CreateInstance(type);

                var method = type.GetMethod(invMethodName);

                var result = method.Invoke(o, null);

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static string GetStringInvokeDll(string invFileName, string invType, string invMethodName)
        {
            var file = AppDomain.CurrentDomain.BaseDirectory + invFileName;

            if (!File.Exists(file))
            {
                throw new Exception("File not found");
            }

            Assembly assembly = Assembly.LoadFrom(file);

            Type type = assembly.GetType(invType);

            object o = Activator.CreateInstance(type);

            var method = type.GetMethod(invMethodName);

            var result = method.Invoke(o, null).ToString();

            return result;
        }

        public static object GetObjectInvokeDllWithParm(string invFileName, string invType, string invMethodName, object[] invParm)
        {
            var file = AppDomain.CurrentDomain.BaseDirectory + invFileName;

            if (!File.Exists(file))
            {
                throw new Exception("File not found: " + file);
            }

            Assembly assembly = Assembly.LoadFrom(file);

            Type type = assembly.GetType(invType);

            object o = Activator.CreateInstance(type);

            var method = type.GetMethod(invMethodName);

            var result = method.Invoke(o, invParm);

            return result;
        }
        public static string GetStringInvokeDllWithParm(string invFileName, string invType, string invMethodName, object[] invParm)
        {
            var file = AppDomain.CurrentDomain.BaseDirectory + invFileName;

            if (!File.Exists(file))
            {
                throw new Exception("File not found: " + file);
            }

            Assembly assembly = Assembly.LoadFrom(file);

            Type type = assembly.GetType(invType);

            object o = Activator.CreateInstance(type);

            var method = type.GetMethod(invMethodName);

            var result = method.Invoke(o, invParm);

            return result.ToString();
        }
    }
}