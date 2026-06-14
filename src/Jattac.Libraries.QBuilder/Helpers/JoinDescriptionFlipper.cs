namespace Jattac.Libraries.QBuilder.Helpers
{
    public class JoinDescriptionFlipper
    {
        public void Flip(JoinDescription joinDescription)
        {
            (joinDescription.LeftTable, joinDescription.RightTable) = (joinDescription.RightTable, joinDescription.LeftTable);
            (joinDescription.LeftField, joinDescription.RightField) = (joinDescription.RightField, joinDescription.LeftField);
            (joinDescription.RegularLeftAlias, joinDescription.RegularRightAlias) = (joinDescription.RegularRightAlias, joinDescription.RegularLeftAlias);
        }
    }
}