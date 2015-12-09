using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;

namespace ReflectionLesson
{
    public class TempClass
    {
        private string _field;

        public string Property { get; set; }

        public TempClass()
        {
            _field = "From public default constructor";
        }

        public void Do()
        {
            Property = "Do method call";
        }

        public string Do2()
        {
            Property = "Do method call with return value";
            return "Yes";
        }
    }

    public class TempClass2
    {
        private string _field;

        private TempClass2()
        {
            _field = "From private default constructor";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            // get type
            var type = Assembly.GetEntryAssembly().GetType("ReflectionLesson.TempClass");
            type = typeof (TempClass);

            // create
            var obj1 = Activator.CreateInstance<TempClass>();
            obj1 = (TempClass)Activator.CreateInstance(typeof(TempClass));

            // method
            var res1 = obj1.GetType().GetMethod("Do").Invoke(obj1, null);
            var res2 = obj1.GetType().GetMethod("Do2").Invoke(obj1, null);

            // property get set
            var prop = type.GetProperties().FirstOrDefault();
            var propGet = prop.GetValue(obj1);
            prop.SetValue(obj1, "New value from reflection");

            // field get set
            var field = obj1.GetType().GetField("_field", BindingFlags.NonPublic | BindingFlags.Instance);
            var fieldGet = field.GetValue(obj1);
            field.SetValue(obj1, "Field from reflection");



            // create 2,3,4,5
            TempClass2 obj2;
            try
            {
                obj2 = Activator.CreateInstance<TempClass2>();
            }
            catch (Exception x)
            {
            }
            obj2 = (TempClass2)Activator.CreateInstance(typeof(TempClass2), true);

            obj2 = (TempClass2)typeof(TempClass2)
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, Type.EmptyTypes, null)
                .Invoke(null);

            // without constructor
            obj2 = (TempClass2) FormatterServices.GetUninitializedObject(typeof (TempClass2));

            var func = Expression.Lambda<Func<TempClass2>>(Expression.New(typeof (TempClass2))).Compile();
            obj2 = func.Invoke();



            // property get set 2
            var propGet2 = ExpressionUtil.GetPropertyValue(obj1, "Property");
            ExpressionUtil.SetPropertyValue(obj1, "Property", "from ExpressionUtil");



            // get type 2
            var assembly = Assembly.Load("mscorlib");
            var types = assembly.GetTypes().OrderBy(x => x.Name).ToArray();
            var typeType = assembly.GetType("System.Type");
            var typeMethods = typeType.GetMethods();

            assembly = Assembly.LoadFrom(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reflectionLesson.exe"));
            types = assembly.GetTypes();
            var thisType = types.FirstOrDefault(x => x.Name == "Program");
            var thisMethod = thisType.GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static);
            thisMethod.Invoke(null, new object[] {new string[] {}});

            Console.WriteLine("Thank you");
        }
    }

    public static class ExpressionUtil
    {
        private static readonly ConcurrentDictionary<Tuple<Type, string>, Delegate> CacheGetPropertyValue = new ConcurrentDictionary<Tuple<Type, string>, Delegate>();
        private static readonly ConcurrentDictionary<Tuple<Type, string>, Delegate> CacheSetPropertyValue = new ConcurrentDictionary<Tuple<Type, string>, Delegate>();

        public static object GetPropertyValue(object obj, string propertyName)
        {
            var key = Tuple.Create(obj.GetType(), propertyName);
            var @delegate = CacheGetPropertyValue.GetOrAdd(key, x =>
            {
                var objectParameter = Expression.Parameter(x.Item1, "obj");
                var property = Expression.Property(objectParameter, x.Item2);
                return Expression.Lambda(property, objectParameter).Compile();
            });
            return @delegate.DynamicInvoke(obj);
        }

        public static void SetPropertyValue(object obj, string propertyName, object value)
        {
            var key = Tuple.Create(obj.GetType(), propertyName);
            var @delegate = CacheSetPropertyValue.GetOrAdd(key, x =>
            {
                var objectParameter = Expression.Parameter(x.Item1, "obj");
                var property = Expression.Property(objectParameter, x.Item2);
                var valueParameter = Expression.Parameter(property.Type, "val"); ;
                var assignExpression = Expression.Assign(property, valueParameter);
                return Expression.Lambda(assignExpression, objectParameter, valueParameter).Compile();
            });
            @delegate.DynamicInvoke(obj, value);
        }
    }
}
