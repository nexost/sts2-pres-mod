namespace PresMod.Dev;

/// <summary>
/// {{Name}}'s test hooks (see Dev/CharacterTests.cs). Every hook is optional: with none, the ui, cards and balance
/// modes already run this character with the shared checks. Add the character's own mechanics here as they are built
/// (Characters/Trump/Dev/DevHarness.Trump.cs is the full example).
/// </summary>
public static partial class DevHarness
{
	[CharacterTestKit("{{ID}}")]
	private static CharacterTests {{Class}}Tests() => new CharacterTests
	{
	};
}
