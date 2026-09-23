using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TrumpMod.Mechanics;

namespace TrumpMod.Models.Relics;

/// <summary>
/// Ancient version of the Golden Shovel, given by Touch of Orobas (TouchOfOrobasPatch).
/// Starter rarity like Black Blood, so it's never offered as a random reward.
/// </summary>
public sealed class DiamondShovel : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Starter;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DynamicVar("StartOfCombat", 10m),
		new DynamicVar("StartOfTurn", 2m)
	};

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Build, TrumpHoverTips.Wall };

	public override async Task AfterRoomEntered(AbstractRoom room)
	{
		if (room is CombatRoom)
		{
			Flash();
			await WallCmd.Build(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["StartOfCombat"].BaseValue);
		}
	}

	public override async Task AfterPlayerTurnStart(PlayerChoiceContext choiceContext, Player player)
	{
		if (player == Owner)
		{
			Flash();
			await WallCmd.Build(choiceContext, Owner.Creature, DynamicVars["StartOfTurn"].BaseValue);
		}
	}
}
