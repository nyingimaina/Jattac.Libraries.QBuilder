namespace Rocket.Libraries.Qurious.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Linq.Expressions;
    using Rocket.Libraries.Qurious.Models;
    using Rocket.Libraries.Validation.Services;

    public class AggregateRowQuerierBuilder<THistoricalTable>
    {
        public const string LatestVersion = "LatestVersion";

        private string _foreignKeyName;

        private string _incrementingFieldName;

        private QBuilder _innerQBuilder;

        private QBuilder _resultQbuilder;

        private Action _doDerivedTableJoin;

        private string _aggregationFunction;

        private Func<Type, string> _tableNameResolver;

        private string _aliasTablename;

        public AggregateRowQuerierBuilder()
            : this("t")

        {
        }

        public AggregateRowQuerierBuilder(string aliasTablename)
            : this(t => t.Name, aliasTablename)

        {
        }

        public AggregateRowQuerierBuilder(Func<Type, string> tableNameResolver, string aliasTablename)
        {
            _tableNameResolver = tableNameResolver;
            _aliasTablename = aliasTablename;
        }

        private QBuilder InnerQBuilder
        {
            get
            {
                if (_innerQBuilder == null)
                {
                    _innerQBuilder = new QBuilder(_tableNameResolver, _aliasTablename, false);
                }

                return _innerQBuilder;
            }
        }

        private QBuilder ResultQbuilder
        {
            get
            {
                if (_resultQbuilder == null)
                {
                    _resultQbuilder = new QBuilder(_tableNameResolver, _aliasTablename, false);
                }

                return _resultQbuilder;
            }
        }

        public AggregateRowQuerierBuilder<THistoricalTable> SetAggregationFunction(string aggregationFunction)
        {
            _aggregationFunction = aggregationFunction;
            return this;
        }

        public AggregateRowQuerierBuilder<THistoricalTable> AddWhereEqualsToFilter<TField>(Expression<Func<THistoricalTable, TField>> fieldNameDescriptor, object value)
        {
            var filterDescription = new FilterDescription<THistoricalTable>()
                .SetWhereEqualToFilter(fieldNameDescriptor, value);
            return AddArbitraryFilter(filterDescription);
        }

        public AggregateRowQuerierBuilder<THistoricalTable> AddWhereInFilter<TField, TValueType>(Expression<Func<THistoricalTable, TField>> fieldNameDescriptor, List<TValueType> listItems)
        {
            var filterDescription = new FilterDescription<THistoricalTable>()
                .SetWhereInFilter(fieldNameDescriptor, listItems);
            return AddArbitraryFilter(filterDescription);
        }

        public AggregateRowQuerierBuilder<THistoricalTable> AddArbitraryFilter(FilterDescription<THistoricalTable> filterDescription)
        {
            InnerQBuilder.UseTableBoundFilter<THistoricalTable>()
                .Where(filterDescription);

            ResultQbuilder.UseTableBoundFilter<THistoricalTable>()
                .Where(filterDescription);
            return this;
        }

        public AggregateRowQuerierBuilder<THistoricalTable> SetForeignKeyResolver<TField>(Expression<Func<THistoricalTable, TField>> foreignKeyResolver)
        {
            _foreignKeyName = new FieldNameResolver().GetFieldName(foreignKeyResolver);
            return this;
        }

        public AggregateRowQuerierBuilder<THistoricalTable> SetIncrementingFieldName<TField>(Expression<Func<THistoricalTable, TField>> incrementingFieldNameResolver)
        {
            _incrementingFieldName = new FieldNameResolver().GetFieldName(incrementingFieldNameResolver);
            CacheDerivedTableJoinCall(incrementingFieldNameResolver);
            return this;
        }

        public QBuilder Build()
        {
            FailIfInvalid();
            InnerQBuilder
             .UseSelector()
             .Select<THistoricalTable>(_foreignKeyName)
             .SelectAggregated<THistoricalTable>(_incrementingFieldName, LatestVersion, _aggregationFunction)
             .Then()
             .UseGrouper()
             .GroupBy<THistoricalTable>(_foreignKeyName);

            ResultQbuilder
                .UseSelector()
                .Select<THistoricalTable>("*")
                .Then();

            _doDerivedTableJoin();

            _doDerivedTableJoin = null;
            return ResultQbuilder;
        }

        private void FailIfInvalid()
        {
            new DataValidator()
                .AddFailureCondition(string.IsNullOrEmpty(_foreignKeyName), $"Foreign key was not specified, cannot determine how to identify records to compare.", false)
                .AddFailureCondition(string.IsNullOrEmpty(_incrementingFieldName), $"Cannot determine which field is being incremented. No way to tell which record is the latest.", false)
                .AddFailureCondition(string.IsNullOrEmpty(_aggregationFunction), $"No aggregate function specified. Cannot query database", false)
                .ThrowExceptionOnInvalidRules();
        }

        private void CacheDerivedTableJoinCall<TField>(Expression<Func<THistoricalTable, TField>> incrementingFieldNameResolver)
        {
            _doDerivedTableJoin = () =>
            {
                ResultQbuilder
                .UseJoiner()
                .UseDerivedTableJoiner<THistoricalTable>()
                .InnerJoin(incrementingFieldNameResolver, InnerQBuilder, LatestVersion);
            };
        }
    }
}