using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScheduleBuilder.Core.Parsing
{
    internal class ProposedCoursesPager : IEquatable<ProposedCoursesPager>
    {
        public int PageNumber { get; }
        public string Pager { get; }
        public ProposedCoursesPager(int pageNumber, string pager)
        {
            if (pageNumber < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber), pageNumber, $"Argument {nameof(pageNumber)} must be between [0, {int.MaxValue}].");
            }
            Pager = pager ?? throw new ArgumentNullException(nameof(pager));
            PageNumber = pageNumber;

        }
        public override int GetHashCode() => HashCodeHelper.CombineHashCodes(PageNumber, Pager.GetHashCode());

        public override bool Equals(object obj)
        {
            if (obj is ProposedCoursesPager pager) { return Equals(pager); }
            return false;
        }
        public bool Equals(ProposedCoursesPager other)
        {
            if (other is null) { return false; }
            return PageNumber == other.PageNumber && Pager.Equals(other.Pager, StringComparison.OrdinalIgnoreCase);
        }
    }
}
