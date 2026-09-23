using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace TrumpMod.Models.Cards;

/// <summary>
/// Step 2 engine test for the Deport idea: removes a weakened enemy through the game's own escape path.
/// Not part of the final design.
/// </summary>
public sealed class DeportPrototype : CardModel
{
	private const string _thresholdVar = "Threshold";

	protected override IEnumerable<DynamicVar> CanonicalVars => new DynamicVar[]
	{
		new DamageVar(5m, ValueProp.Move),
		new DynamicVar(_thresholdVar, 12m)
	};

	public DeportPrototype()
		: base(1, CardType.Attack, CardRarity.Basic, TargetType.AnyEnemy)
	{
	}

	protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
	{
		ArgumentNullException.ThrowIfNull(cardPlay.Target, "cardPlay.Target");
		await DamageCmd.Attack(DynamicVars.Damage.BaseValue).FromCard(this).Targeting(cardPlay.Target)
			.WithHitFx("vfx/vfx_attack_blunt")
			.Execute(choiceContext);
		if (cardPlay.Target.IsAlive && cardPlay.Target.CurrentHp <= DynamicVars[_thresholdVar].BaseValue)
		{
			await CreatureCmd.Escape(cardPlay.Target);
			await CombatManager.Instance.CheckWinCondition();
		}
	}

	protected override void OnUpgrade()
	{
		DynamicVars[_thresholdVar].UpgradeValueBy(4m);
	}
}
