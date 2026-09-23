using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace TrumpMod.Mechanics;

/// <summary>Lets a relic or power change how much a Build adds (e.g. Hard Hat: +1).</summary>
public interface IBuildModifier
{
	decimal ModifyBuild(Creature builder, decimal amount);
}

/// <summary>Lets a relic or power change the Block each Section gives (e.g. Reinforced Concrete).</summary>
public interface ISectionBlockModifier
{
	decimal ModifySectionBlock(Creature owner, decimal blockPerSection);
}

/// <summary>Lets a relic or power raise a player's Deport line (e.g. Border Control: +10%).</summary>
public interface IDeportLineModifier
{
	decimal ModifyDeportLine(Player player, decimal line);
}

public interface IAfterDeport
{
	Task AfterDeport(PlayerChoiceContext choiceContext, Player deporter, Creature deported);
}

public interface IAfterPayGold
{
	Task AfterPayGold(Player player, int amount);
}

public interface IAfterWallStage
{
	Task AfterWallStageReached(PlayerChoiceContext choiceContext, Creature owner, int stage);
}

/// <summary>Our hooks are interfaces on the player's own relics and powers (the game's Hook system only knows its own events).</summary>
internal static class ModHooks
{
	public static IEnumerable<T> ListenersOf<T>(Creature creature)
	{
		IEnumerable<AbstractModel> models = creature.Powers.Cast<AbstractModel>();
		if (creature.Player != null)
		{
			models = creature.Player.Relics.Cast<AbstractModel>().Concat(models);
		}
		return models.OfType<T>().ToList();
	}
}
