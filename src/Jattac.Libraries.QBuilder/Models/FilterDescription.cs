namespace Jattac.Libraries.QBuilder.Models
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Jattac.Libraries.QBuilder.Helpers;

    public class FilterDescription<TTable>
    {
        public string FieldName { get; private set; }

        public string Filter { get; private set; }

        public bool FilterSet => !string.IsNullOrEmpty(FieldName) && !string.IsNullOrEmpty(Filter);

        public FilterDescription<TTable> SetConditionalFilter<TField>(Expression<Func<TTable, TField>> fieldNameDescriptor, Func<string> fnConditionalFilterResolver)
        {
            return SetFilter(fieldNameDescriptor, fnConditionalFilterResolver());
        }

        public FilterDescription<TTable> SetFilter<TField>(Expression<Func<TTable, TField>> fieldNameDescriptor, string filter)
        {
            FieldName = new FieldNameResolver().GetFieldName(fieldNameDescriptor);
            Filter = filter;
            return this;
        }

        public FilterDescription<TTable> SetWhereEqualToFilter<TField>(Expression<Func<TTable, TField>> fieldNameDescriptor, object value)
        {
            return SetFilter(fieldNameDescriptor, $"='{value}'");
        }

        public FilterDescription<TTable> SetWhereInFilter<TField, TValueType>(Expression<Func<TTable, TField>> fieldNameDescriptor, List<TValueType> listItems)
        {
            var noFilterValues = listItems == null || listItems.Count == 0;
            if (noFilterValues)
            {
                return this;
            }
            else
            {
                var bracketed = WhereInFilterMaker.GetWhereInSectionArguments(listItems);
                return SetFilter(fieldNameDescriptor, $" in {bracketed}");
            }
        }
    }
}