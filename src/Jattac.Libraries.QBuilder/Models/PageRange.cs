namespace Jattac.Libraries.QBuilder.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public class PageRange
    {
        public long Start { get; set; }

        public long End { get; set; }

        public ushort PageSize { get; set; }

        public override bool Equals(object obj)
        {
            var inputObject = obj as PageRange;
            if (inputObject == null)
            {
                return false;
            }

            return inputObject.Start == Start
                && inputObject.End == End
                && inputObject.PageSize == PageSize;
        }

        public override int GetHashCode() => HashCode.Combine(Start, End, PageSize);
    }
}