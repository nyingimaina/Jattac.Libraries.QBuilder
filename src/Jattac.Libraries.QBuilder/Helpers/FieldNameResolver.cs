namespace Jattac.Libraries.QBuilder.Helpers
{
    using System;
    using System.Linq.Expressions;
    using System.Reflection;

    public class FieldNameResolver
    {
        public string GetFieldName<TTable, TField>(Expression<Func<TTable, TField>> fieldResolver)
        {
            var propertyInfo = GetPropertyInfo(fieldResolver);
            return propertyInfo.Name;
        }

        private PropertyInfo GetPropertyInfo<TTable, TField>(Expression<Func<TTable, TField>> propertyLambda)
        {
            Type type = typeof(TTable);

            MemberExpression member = propertyLambda.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException(string.Format(
                        "Expression '{0}' refers to a method, not a property.",
                        propertyLambda.ToString()));

            PropertyInfo propInfo = member.Member as PropertyInfo;
            if (propInfo == null)
                throw new ArgumentException(string.Format(
                        "Expression '{0}' refers to a field, not a property.",
                        propertyLambda.ToString()));

            if (type != propInfo.ReflectedType &&
                 !type.IsSubclassOf(propInfo.ReflectedType))
                throw new ArgumentException(string.Format(
                        "Expresion '{0}' refers to a property that is not from type {1}.",
                        propertyLambda.ToString(),
                        type));

            return propInfo;
        }
    }
}