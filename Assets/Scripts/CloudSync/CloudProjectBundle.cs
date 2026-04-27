using System.Collections.Generic;
using DLS.Description;

namespace DLS.CloudSync
{
	public sealed class CloudProjectBundle
	{
		public ProjectDescription ProjectDescription { get; }
		public IReadOnlyList<ChipDescription> Chips { get; }

		public CloudProjectBundle(ProjectDescription projectDescription, IReadOnlyList<ChipDescription> chips)
		{
			ProjectDescription = projectDescription;
			Chips = chips ?? new List<ChipDescription>();
		}
	}
}
