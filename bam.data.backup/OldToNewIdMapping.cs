/*
	Copyright © Bryan Apellanes 2015  
*/

namespace Bam.Data.Repositories
{
	public class OldToNewIdMapping
	{
		public OldToNewIdMapping() { }

		public Type PocoType { get; set; } = null!;
		public Type DaoType { get; set; } = null!;
		public ulong OldId { get; set; }
		public ulong NewId { get; set; }
		public string Uuid { get; set; } = null!;

		public override int GetHashCode()
		{
			return Uuid.GetHashCode();
		}
		public override bool Equals(object? obj)
		{
            if (obj is OldToNewIdMapping o)
            {
                return o.Uuid == this.Uuid;
            }

            return false;
		}
	}
}
