using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Inspiring.Contracts {
    internal static class ReflectionExtensions {
        public static string? GetDiscriminatorName<TAttribute>(this Type type) where TAttribute : Attribute
            => type.GetCustomAttribute<TAttribute>()?.GetDiscriminatorName();

        public static string? GetDiscriminatorValue<TAttribute>(this Type type) where TAttribute : Attribute
            => type.GetCustomAttribute<TAttribute>()?.GetDiscriminatorValue();

        private static string? GetDiscriminatorName<T>(this T instance)
            => Getters<T>.DiscriminatorName(instance);

        private static string? GetDiscriminatorValue<T>(this T instance)
            => Getters<T>.DiscriminatorValue(instance);

        private struct Getters<T> {
            public static readonly Func<T, string?> DiscriminatorName = CreateGetter(nameof(ContractAttribute.DiscriminatorName));
            public static readonly Func<T, string?> DiscriminatorValue = CreateGetter(nameof(ContractAttribute.DiscriminatorValue));

            private static Func<T, string?> CreateGetter(string propertyName) {
                ParameterExpression param = Expression.Parameter(typeof(T), "instance");
                return Expression.Lambda<Func<T, string?>>(
                    Expression.MakeMemberAccess(param, GetProperty(propertyName)),
                    param
                ).Compile();
            }

            private static PropertyInfo GetProperty(string name) =>
                typeof(T).GetProperty(name, typeof(string)) ??
                throw new ArgumentException($"The type '{typeof(T).Name}' must have a public property '{name}' of type 'string'.");
        }
    }
}
