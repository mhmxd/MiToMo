using Common.Constants;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Common.Helpers
{
    public class IOTools
    {

        public static StreamWriter PrepareFile<T>(string filePath, string fileName)
        {
            //string directoryPath = Path.GetDirectoryName(timedFilePath);
            if (!Directory.Exists(filePath))
            {
                Directory.CreateDirectory(filePath);
            }

            string timestamp = DateTime.Now.ToString(ExpStrs.DATE_TIME_FORMAT);
            filePath = Path.Combine(filePath, $"{fileName}-{timestamp}.csv");

            bool timedFileExists = File.Exists(filePath);
            bool timedFileIsEmpty = !timedFileExists || new FileInfo(filePath).Length == 0;
            StreamWriter writer = new StreamWriter(filePath, append: true, Encoding.UTF8);
            writer.AutoFlush = true;
            if (timedFileIsEmpty)
            {
                WriteHeader<T>(writer);
            }

            return writer;
        }

        public static void WriteHeader<T>(StreamWriter streamWriter)
        {
            //var fields = typeof(T).GetFields();
            //var headers = fields.Select(f => f.Name);
            //_detailTrialLogWriter.WriteLine(string.Join(";", headers));

            // Writing first the parent class fields, then the child class fields
            var type = typeof(T);
            var baseType = type.BaseType;

            // 1. Get fields from the base class (parent)
            // BindingFlags.DeclaredOnly ensures we only get fields directly defined in the base class,
            // not its own base classes, or the derived class's fields.
            var parentFields = baseType != null && baseType != typeof(object)
                ? baseType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                : Enumerable.Empty<FieldInfo>();

            // 2. Get fields from the derived class (child)
            // BindingFlags.DeclaredOnly ensures we only get fields directly defined in the derived class.
            var childFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            // 3. Combine them: Parent fields first, then Child fields.
            var allFields = parentFields.Concat(childFields);

            // 4. Extract names and write to the file.
            var headers = allFields.Select(f => f.Name);
            streamWriter.WriteLine(string.Join(";", headers));
        }

        public static void WriteTrialLog<T>(T log, string filePath, StreamWriter writer)
        {
            var type = typeof(T);
            var baseType = type.BaseType;

            // 1. Get fields from the base class (parent)
            // Use BindingFlags.Public and BindingFlags.Instance to match the default GetFields behavior.
            var parentFields = baseType != null && baseType != typeof(object)
                ? baseType.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                : Enumerable.Empty<FieldInfo>();

            // 2. Get fields from the derived class (child)
            var childFields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

            // 3. Combine them: Parent fields first, then Child fields.
            var orderedFields = parentFields.Concat(childFields);

            // 4. Get values in the same order.
            var values = orderedFields
                .Select(f => f.GetValue(log)?.ToString() ?? "");

            // 5. Write the values.
            writer.WriteLine(string.Join(";", values));
            //streamWriter.Flush();
        }
    }
}
