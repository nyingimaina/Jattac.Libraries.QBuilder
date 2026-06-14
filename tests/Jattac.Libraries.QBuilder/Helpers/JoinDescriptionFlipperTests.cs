using Jattac.Libraries.QBuilder;
using Jattac.Libraries.QBuilder.Helpers;
using Xunit;

namespace Jattac.QBuilderTests.Helpers
{
    public class JoinDescriptionFlipperTests
    {
        [Fact]
        public void JoinDescriptionFlipIsDoneCorrectly()
        {
            const string leftTable = "leftTable";
            const string rightTable = "RightTable";
            const string leftField = "leftField";
            const string rightField = "rightField";

            var joinDescription = new JoinDescription
            {
                LeftField = rightField,
                LeftTable = rightTable,
                RightField = leftField,
                RightTable = leftTable
            };

            new JoinDescriptionFlipper().Flip(joinDescription);

            Assert.Equal(leftTable, joinDescription.LeftTable);
            Assert.Equal(leftField, joinDescription.LeftField);
            Assert.Equal(rightField, joinDescription.RightField);
            Assert.Equal(rightTable, joinDescription.RightTable);
        }
    }
}