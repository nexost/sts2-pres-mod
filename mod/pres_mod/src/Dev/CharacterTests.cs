using System.Reflection;
using MegaCrit.Sts2.Core.Helpers;

namespace PresMod.Dev;

/// <summary>
/// Marks the static method of DevHarness that returns one character's test hooks, e.g.
/// <c>[CharacterTestKit("TRUMP")] private static CharacterTests TrumpTests() => new CharacterTests { ... };</c>
/// Each character keeps its method in its own partial file (Characters/&lt;Name&gt;/Dev/DevHarness.&lt;Name&gt;.cs).
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class CharacterTestKitAttribute : Attribute
{
	public CharacterTestKitAttribute(string characterId)
	{
		CharacterId = characterId;
	}

	public string CharacterId { get; }
}

public static partial class DevHarness
{
	/// <summary>
	/// What one character adds to the shared test modes. Every hook is optional: with none, the ui test still starts a
	/// run, opens the card library, checks the combat poses, the shop and the rest site; the cards test still plays
	/// every card base and upgraded and renders all texts; the balance bot still plays with the generic card values.
	/// </summary>
	internal sealed class CharacterTests
	{
		// ---- ui test
		/// <summary>The character's own mechanics, run in the ui test's first fight (after the pose checks). May start more fights.</summary>
		public Func<Task>? UiChecks;

		/// <summary>Extra test modes only this character has (Trump: "deportsweep"), run with test.py &lt;mode&gt;.</summary>
		public Dictionary<string, Func<Task>> ExtraModes = new Dictionary<string, Func<Task>>();

		// ---- cards test
		/// <summary>Relic, potion and turn-power checks, run before the cards (test.py cards SEED relics runs only these).</summary>
		public Func<Task>? RelicChecks;

		public Func<Task>? PotionChecks;

		public Func<Task>? PowerChecks;

		/// <summary>Cards outside the card pool that should be played too (tokens like Trump's Tweet).</summary>
		public Func<IEnumerable<CardModel>>? ExtraCards;

		/// <summary>Puts a card the dev console can't add into the hand; returns false to let the console add it.</summary>
		public Func<CardModel, ICombatState, Task<bool>>? AddCardToHand;

		/// <summary>Character state for every card, after the shared setup (full HP, 300 Gold, 3 Energy): e.g. a 30 Wall.</summary>
		public Func<PlayerChoiceContext, Task>? PrepareCardTurn;

		/// <summary>Like PrepareCardTurn, knowing the card and whether it will be upgraded (Biden: base as Sleepy Joe, upgraded as Dark Brandon).</summary>
		public Func<PlayerChoiceContext, CardModel, bool, Task>? PrepareCardTurnFor;

		/// <summary>Adds the character's own counters to each before/after snapshot (Wall height, Tweets in hand, ...).</summary>
		public Action<Player, Dictionary<string, decimal>>? Snapshot;

		/// <summary>Checks a card's key effect from its before/after snapshots: (card id, upgraded, before, after, expect).</summary>
		public Action<string, bool, CardSnapshot, CardSnapshot, Action<bool, string>>? CheckCardEffect;

		// ---- coop test
		/// <summary>The character's co-op plays in the first turn of the co-op fight: (is host, combat). Trump: Coalition Wall, Trickle Down.</summary>
		public Func<bool, ICombatState, Task>? CoopTurn;

		// ---- balance bot
		/// <summary>Once, when a balance run of this character starts (subscribe to the character's events).</summary>
		public Action? BalanceInit;

		/// <summary>Adds per-combat numbers to balance.json after each card and turn (Trump: maxWall, deports).</summary>
		public Action<Player, Dictionary<string, object>>? BalanceCombatStats;

		/// <summary>Called when each combat starts (reset per-combat counters).</summary>
		public Action? BalanceCombatStart;

		/// <summary>Whether this much damage from this card removes the enemy another way than killing it (Trump: Deport).</summary>
		public Func<FightView, Creature, double, CardModel?, bool>? RemovesEnemy;

		/// <summary>Value in "HP saved" of the character's own card effects, added to the generic value (Build, Tariff, Gold, ...).</summary>
		public Func<CardModel, FightView, Func<string, double>, double>? CardValue;
	}

	private static Dictionary<string, CharacterTests>? _kits;

	/// <summary>The character the tests play: --pres-character ID, else the first mod character.</summary>
	internal static string TestCharacterId =>
		(CommandLineHelper.GetValue("pres-character") ?? ModContent.Characters.Select(c => c.Id.Entry).OrderBy(e => e, StringComparer.Ordinal).FirstOrDefault() ?? "").ToUpperInvariant();

	internal static CharacterModel TestCharacter => ModelDb.AllCharacters.First(c => c.Id.Entry == TestCharacterId);

	/// <summary>The hooks of the character under test (empty for base-game characters in balance runs).</summary>
	private static CharacterTests Kit => KitFor(TestCharacterId);

	private static CharacterTests KitFor(string characterId)
	{
		if (_kits == null)
		{
			_kits = new Dictionary<string, CharacterTests>();
			foreach (MethodInfo method in typeof(DevHarness).GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static))
			{
				if (method.GetCustomAttribute<CharacterTestKitAttribute>() is { } attribute)
				{
					_kits[attribute.CharacterId] = (CharacterTests)method.Invoke(null, null)!;
				}
			}
		}
		return _kits.TryGetValue(characterId, out CharacterTests? kit) ? kit : new CharacterTests();
	}
}
