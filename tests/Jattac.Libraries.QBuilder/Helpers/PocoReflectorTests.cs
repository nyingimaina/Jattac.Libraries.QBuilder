namespace Jattac.QBuilderTests.Helpers
{
    using System;
    using System.Linq;
    using Jattac.Libraries.QBuilder.Attributes;
    using Jattac.Libraries.QBuilder.Helpers;
    using Jattac.QBuilderTests.Models;
    using Xunit;

    public class PocoReflectorTests
    {
        [Fact]
        public void GetProperties_NoAttributes_ColumnNameEqualsPropertyName()
        {
            var instance = new UserNoKey { Id = Guid.NewGuid(), Name = "Alice" };
            var props = PocoReflector.GetProperties(instance);

            foreach (var p in props)
                Assert.Equal(p.PropertyName, p.ColumnName);
        }

        [Fact]
        public void GetProperties_QKey_IsKeyTrue()
        {
            var instance = new UserWithKey { Id = Guid.NewGuid(), Name = "Alice" };
            var props = PocoReflector.GetProperties(instance);
            var idProp = props.Single(p => p.PropertyName == "Id");
            Assert.True(idProp.IsKey);
        }

        [Fact]
        public void GetProperties_NonKeyProperty_IsKeyFalse()
        {
            var instance = new UserWithKey { Id = Guid.NewGuid(), Name = "Alice" };
            var props = PocoReflector.GetProperties(instance);
            var nameProp = props.Single(p => p.PropertyName == "Name");
            Assert.False(nameProp.IsKey);
        }

        [Fact]
        public void GetProperties_QIgnore_IsIgnoredTrue()
        {
            var instance = new UserWithIgnore { Id = Guid.NewGuid(), Name = "Alice", IsActive = false };
            var props = PocoReflector.GetProperties(instance);
            var isActiveProp = props.Single(p => p.PropertyName == "IsActive");
            Assert.True(isActiveProp.IsIgnored);
        }

        [Fact]
        public void GetProperties_QColumn_ColumnNameOverridden()
        {
            var instance = new UserWithColumnAlias { Id = Guid.NewGuid(), Name = "Alice" };
            var props = PocoReflector.GetProperties(instance);
            var nameProp = props.Single(p => p.PropertyName == "Name");
            Assert.Equal("user_name", nameProp.ColumnName);
            Assert.Equal("Name", nameProp.PropertyName);
        }

        [Fact]
        public void GetProperties_QKeyAndQIgnore_BothFlagsSet()
        {
            var instance = new UserWithKeyAndIgnore { Id = Guid.NewGuid(), Name = "Alice" };
            var props = PocoReflector.GetProperties(instance);
            var idProp = props.Single(p => p.PropertyName == "Id");
            Assert.True(idProp.IsKey);
            Assert.True(idProp.IsIgnored);
        }

        [Fact]
        public void GetProperties_CalledTwiceForSameType_ReturnsSameDescriptors()
        {
            var a = new UserWithKey { Id = Guid.NewGuid(), Name = "Alice" };
            var b = new UserWithKey { Id = Guid.NewGuid(), Name = "Bob" };

            var props1 = PocoReflector.GetProperties(a);
            var props2 = PocoReflector.GetProperties(b);

            // Same count and same property names — the descriptor list was cached.
            Assert.Equal(props1.Count, props2.Count);
            for (var i = 0; i < props1.Count; i++)
            {
                Assert.Equal(props1[i].PropertyName, props2[i].PropertyName);
                Assert.Equal(props1[i].ColumnName, props2[i].ColumnName);
                Assert.Equal(props1[i].IsKey, props2[i].IsKey);
                Assert.Equal(props1[i].IsIgnored, props2[i].IsIgnored);
            }
        }

        [Fact]
        public void IsAnonymousType_AnonymousObject_ReturnsTrue()
        {
            var anon = new { Id = 1, Name = "x" };
            Assert.True(PocoReflector.IsAnonymousType(anon.GetType()));
        }

        [Fact]
        public void IsAnonymousType_NamedClass_ReturnsFalse()
        {
            Assert.False(PocoReflector.IsAnonymousType(typeof(UserWithKey)));
        }

        [Fact]
        public void GetProperties_AnonymousObject_AllPropsNotKeyNotIgnored()
        {
            var anon = new { Name = "Alice", IsActive = true };
            var props = PocoReflector.GetProperties(anon);

            Assert.All(props, p =>
            {
                Assert.False(p.IsKey);
                Assert.False(p.IsIgnored);
            });
        }

        [Fact]
        public void GetProperties_ValueTypeProp_ValueBoxedCorrectly()
        {
            var instance = new UserWithKey { Id = Guid.NewGuid(), IsActive = true };
            var props = PocoReflector.GetProperties(instance);
            var isActiveProp = props.Single(p => p.PropertyName == "IsActive");

            // Value type bool should be boxed to object but compare equal to true.
            Assert.Equal(true, isActiveProp.Value);
        }

        [Fact]
        public void GetProperties_ReturnsCorrectValuesFromInstance()
        {
            var id = Guid.NewGuid();
            var instance = new UserWithKey { Id = id, Name = "Test", IsActive = false };
            var props = PocoReflector.GetProperties(instance);

            Assert.Equal(id, props.Single(p => p.PropertyName == "Id").Value);
            Assert.Equal("Test", props.Single(p => p.PropertyName == "Name").Value);
            Assert.Equal(false, props.Single(p => p.PropertyName == "IsActive").Value);
        }

        [Fact]
        public void GetProperties_NullValue_ReturnsNull()
        {
            var instance = new UserWithKey { Id = Guid.NewGuid(), Name = null };
            var props = PocoReflector.GetProperties(instance);
            Assert.Null(props.Single(p => p.PropertyName == "Name").Value);
        }
    }
}
