using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves.Runs;
using TrumpMod.Mechanics;

namespace TrumpMod.Models.Relics;

/// <summary>
/// Rare: at the end of combat, keep a quarter of the Wall and start the next combat with it. The kept height is a
/// saved property (registered in ModEntry), so it survives save and quit. It feeds back into itself: a taller Wall
/// next fight means more kept after it, with no ceiling.
/// </summary>
public sealed class PermanentStructure : RelicModel
{
	public const int KeepDivisor = 4;

	private int _storedHeight;

	public override RelicRarity Rarity => RelicRarity.Rare;

	public override bool ShowCounter => StoredHeight > 0;

	public override int DisplayAmount => StoredHeight;

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Wall };

	[SavedProperty]
	public int StoredHeight
	{
		get
		{
			return _storedHeight;
		}
		set
		{
			AssertMutable();
			_storedHeight = value;
			InvokeDisplayAmountChanged();
		}
	}

	public override Task AfterCombatEnd(CombatRoom room)
	{
		StoredHeight = WallCmd.GetHeight(Owner.Creature) / KeepDivisor;
		return Task.CompletedTask;
	}

	public override async Task AfterRoomEntered(AbstractRoom room)
	{
		if (room is CombatRoom && StoredHeight > 0)
		{
			Flash();
			await WallCmd.Raise(new ThrowingPlayerChoiceContext(), Owner.Creature, StoredHeight);
		}
	}
}
