namespace Jattac.Libraries.QBuilder.Builders.Paging
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Jattac.Libraries.QBuilder.Models;
    public static class PageRangeCalculator
    {
        public static PageRange GetPageRange(byte absoluteFirstRecordIndex, uint page, ushort pageSize)
        {
            const byte pagesAdvanceRate = 1;
            var previousPage = page - pagesAdvanceRate;
            var relativeStartRecord = pagesAdvanceRate + (pageSize * previousPage);
            var range = new PageRange
            {
                Start = GetAbsoluteStartRecord(absoluteFirstRecordIndex, relativeStartRecord),
                PageSize = pageSize,
            };
            range.End = range.Start + pageSize - pagesAdvanceRate;
            return range;
        }

        /// <summary>
        /// This method caters for differences in absolute starting position of pages
        /// e.g first record in MS SQL Server is 1 while MySQL's first record is 0
        /// </summary>
        /// <param name="absoluteFirstRecordIndex">The index of the first record</param>
        /// <param name="page">The page we're interested in</param>
        /// <returns>Relative start position adjusted to absolute start</returns>
        private static long GetAbsoluteStartRecord(byte absoluteFirstRecordIndex, uint page)
        {
            const sbyte offSet = -1;
            return offSet + absoluteFirstRecordIndex + page;
        }
    }
}