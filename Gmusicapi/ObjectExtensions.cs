﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Gmusicapi
{
    public static class ObjectExtensions
    {
        private static class New<T>
        {
            public static readonly Func<T> Instance = Creator();

            static Func<T> Creator()
            {
                Type t = typeof(T);
                if (t == typeof(string))
                    return Expression.Lambda<Func<T>>(Expression.Constant(string.Empty)).Compile();

                if (t.HasDefaultConstructor())
                    return Expression.Lambda<Func<T>>(Expression.New(t)).Compile();

                return () => (T)FormatterServices.GetUninitializedObject(t);
            }
        }

        private static bool HasDefaultConstructor(this Type t)
        {
            return t.IsValueType || t.GetConstructor(Type.EmptyTypes) != null;
        }

        public static T ToObject<T>(this IronPython.Runtime.PythonDictionary source)
            where T : class, new()
        {
            T someObject = new T();
            Type someObjectType = someObject.GetType();

            foreach (var item in source)
            {
                try
                {
                    var newValue = item.Value;
                    if (newValue.GetType() == typeof(IronPython.Runtime.List))
                    {

                        MethodInfo method = typeof(ObjectExtensions).GetMethod("ToList");
                        MethodInfo generic = method.MakeGenericMethod(someObjectType.GetProperty((String)item.Key).PropertyType.GenericTypeArguments[0]);
                        newValue = generic.Invoke(null, new object[] { (IronPython.Runtime.List)newValue });
                    }

                    else if (newValue.GetType() == typeof(IronPython.Runtime.PythonDictionary))
                    {
                        MethodInfo method = typeof(ObjectExtensions).GetMethod("ToObject");
                        MethodInfo generic = method.MakeGenericMethod(someObjectType.GetProperty((String)item.Key).PropertyType);
                        newValue = generic.Invoke(null, new object[] { (IronPython.Runtime.PythonDictionary)newValue });
                    }

                    someObjectType.GetProperty((string)item.Key).SetValue(someObject, newValue, null);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to set:" + item.Key);
                    Console.WriteLine(ex.Message);
                }
            }

            return someObject;
        }

        public static List<T> ToList<T>(this IronPython.Runtime.List source)
           where T : class
        {
            T someObject = New<T>.Instance();
            List<T> someList = new List<T>();
            Type someObjectType = someObject.GetType();

            foreach (var item in source)
            {
                T newValue = null;
                try
                {
                    if (item.GetType() == typeof(IronPython.Runtime.List))
                    {
                        MethodInfo method = typeof(ObjectExtensions).GetMethod("ToList");
                        MethodInfo generic = method.MakeGenericMethod(someObjectType.GenericTypeArguments[0]);
                        newValue = (T)generic.Invoke(null, new object[] { (IronPython.Runtime.PythonDictionary)item });
                    }
                    else if (item.GetType() == typeof(IronPython.Runtime.PythonDictionary))
                    {
                        MethodInfo method = typeof(ObjectExtensions).GetMethod("ToObject");
                        MethodInfo generic = method.MakeGenericMethod(someObjectType);
                        newValue = (T)generic.Invoke(null, new object[] { (IronPython.Runtime.PythonDictionary)item });
                    }
                    else
                    {
                        newValue = (T)item;
                    }

                    someList.Add(newValue);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to add:" + newValue.ToString());
                    Console.WriteLine(ex.Message);
                }
            }

            return someList;
        }

        public static IDictionary<string, object> AsDictionary(this object source, BindingFlags bindingAttr = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance)
        {
            return source.GetType().GetProperties(bindingAttr).ToDictionary
            (
                propInfo => propInfo.Name,
                propInfo => propInfo.GetValue(source, null)
            );

        }
    }
}