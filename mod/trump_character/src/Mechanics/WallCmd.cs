using System;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using TrumpMod.Models.Powers;

namespace TrumpMod.Mechanics;

/// <summary>Everything that changes the Wall goes through here so modifiers, stages and the display stay in sync.</summary>
public static class WallCmd
{
	/// <summary>Raised after a new stage is reached: (owner, stage). Used by the on-screen Wall display.</summary>
	public static event Action<Creature, int>? StageReached;

	public static int GetHeight(Creature creature) => creature.GetPowerAmount<WallPower>();

	public static int GetSections(Creature creature) => WallRules.SectionsFor(GetHeight(creature));

	public static int GetStage(Creature creature) => creature.GetPowerAmount<WallStagePower>();

	public static async Task Build(PlayerChoiceContext choiceContext, Creature builder, decimal amount, CardModel? cardSource = null)
	{
		if (builder.CombatState == null || builder.IsDead)
		{
			return;
		}
		foreach (IBuildModifier modifier in ModHooks.ListenersOf<IBuildModifier>(builder))
		{
			amount = modifier.ModifyBuild(builder, amount);
		}
		await Raise(choiceContext, builder, amount, cardSource);
	}

	/// <summary>
	/// Add height without counting as a Build (no Build modifiers): height carried over from the last combat (Permanent Structure).
	/// </summary>
	public static async Task Raise(PlayerChoiceContext choiceContext, Creature builder, decimal amount, CardModel? cardSource = null)
	{
		if (builder.CombatState == null || builder.IsDead || amount <= 0m)
		{
			return;
		}
		await PowerCmd.Apply<WallPower>(choiceContext, builder, amount, builder, cardSource);
		await RefreshStage(choiceContext, builder);
	}

	/// <summary>Spend Wall height (Demolition, Wrecking Ball). Stages already reached stay unlocked.</summary>
	public static async Task LoseHeight(PlayerChoiceContext choiceContext, Creature builder, int amount)
	{
		WallPower? wall = builder.GetPower<WallPower>();
		if (wall == null || amount <= 0)
		{
			return;
		}
		await PowerCmd.ModifyAmount(choiceContext, wall, -Math.Min(amount, wall.Amount), builder, null);
	}

	/// <summary>Demolition and Wrecking Ball: lose half the Wall, rounded down.</summary>
	public static async Task LoseHalf(PlayerChoiceContext choiceContext, Creature builder)
	{
		await LoseHeight(choiceContext, builder, GetHeight(builder) / 2);
	}

	private static async Task RefreshStage(PlayerChoiceContext choiceContext, Creature builder)
	{
		int reached = WallRules.StageFor(GetHeight(builder));
		int current = GetStage(builder);
		if (reached <= current)
		{
			return;
		}
		await PowerCmd.Apply<WallStagePower>(choiceContext, builder, reached - current, builder, null);
		for (int stage = current + 1; stage <= reached; stage++)
		{
			StageReached?.Invoke(builder, stage);
			foreach (IAfterWallStage listener in ModHooks.ListenersOf<IAfterWallStage>(builder))
			{
				await listener.AfterWallStageReached(choiceContext, builder, stage);
			}
		}
	}
}
