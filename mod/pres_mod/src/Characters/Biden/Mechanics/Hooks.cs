using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace PresMod.Characters.Biden.Mechanics;

/// <summary>Lets a relic or power change how much a Doze adds (e.g. Travel Pillow: +1).</summary>
public interface IDozeModifier
{
	decimal ModifyDoze(Creature owner, decimal amount);
}

/// <summary>Lets a power move the nod-off line (e.g. Heavy Sleeper: +5).</summary>
public interface INodOffLineModifier
{
	decimal ModifyNodOffLine(Creature owner, decimal line);
}

/// <summary>Lets a relic or power change Laser Eyes: the damage per hit (Laser Focus, the Mug) and the number of hits (Double Vision).</summary>
public interface ILaserEyesModifier
{
	decimal ModifyLaserDamage(Creature owner, decimal damage) => damage;

	int ModifyLaserHits(Creature owner, int hits) => hits;
}

/// <summary>Called after the owner nods off (Aviator Shades' Block, Power Nap, Snoring, Well Rested).</summary>
public interface IAfterNodOff
{
	Task AfterNodOff(PlayerChoiceContext choiceContext, Creature owner);
}

/// <summary>Called after the owner wakes up as Dark Brandon, once Laser Eyes have fired (Wide Awake, No Malarkey, '67 Corvette).</summary>
public interface IAfterWakeUp
{
	Task AfterWakeUp(PlayerChoiceContext choiceContext, Creature owner);
}

/// <summary>Called after a Tangent card in the owner's hand moves to its next line (Stream of Consciousness, Tall Tales).</summary>
public interface IAfterTangentLineChanged
{
	Task AfterTangentLineChanged(PlayerChoiceContext choiceContext, CardModel card, int line);
}

/// <summary>Makes a Tangent card do all its lines this time, like Dark Brandon does (Moment of Clarity, Off Script).</summary>
public interface ITangentAllLines
{
	bool DoesAllLines(CardModel card);
}

/// <summary>Our hooks are interfaces on the player's own relics and powers (the game's Hook system only knows its own events).</summary>
internal static class BidenHooks
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
