namespace TrumpMod.Mechanics;

/// <summary>
/// The Wall as a construction project (design v2, docs/03_design.md §3).
/// Height only grows from Build and only drops when spent; every 10 height is a Section; stages are one-way milestones.
/// </summary>
public static class WallRules
{
	public const int SectionSize = 10;

	public const decimal BlockPerSection = 3m;

	/// <summary>Stage-4 perk: end-of-turn damage to ALL enemies per Section.</summary>
	public const decimal StageDamagePerSection = 1m;

	/// <summary>Height needed for stages 1-4: chain-link fence, brick, concrete, Big Beautiful Wall.</summary>
	public static readonly int[] StageHeights = { 10, 25, 45, 70 };

	public const int FenceStage = 1;

	public const int BrickStage = 2;

	public const int ConcreteStage = 3;

	public const int BigBeautifulStage = 4;

	public static int SectionsFor(int height) => height <= 0 ? 0 : height / SectionSize;

	public static int StageFor(int height)
	{
		int stage = 0;
		foreach (int threshold in StageHeights)
		{
			if (height >= threshold)
			{
				stage++;
			}
		}
		return stage;
	}

	/// <summary>Height of the next stage, or null once the Big Beautiful Wall is up.</summary>
	public static int? NextStageHeight(int stage) => stage < StageHeights.Length ? StageHeights[stage] : null;
}
