using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Relics;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using TrumpMod.Mechanics;

namespace TrumpMod.Models.Relics;

/// <summary>Starter: at the start of each combat, Build 6. With Build the Wall that's the first Section on turn 1.</summary>
public sealed class GoldenShovel : RelicModel
{
	public override RelicRarity Rarity => RelicRarity.Starter;

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[] { new DynamicVar("Build", 6m) };

	protected override IEnumerable<IHoverTip> ExtraHoverTips => new[] { TrumpHoverTips.Build, TrumpHoverTips.Wall };

	public override async Task AfterRoomEntered(AbstractRoom room)
	{
		if (room is CombatRoom)
		{
			Flash();
			await WallCmd.Build(new ThrowingPlayerChoiceContext(), Owner.Creature, DynamicVars["Build"].BaseValue);
		}
	}
}
