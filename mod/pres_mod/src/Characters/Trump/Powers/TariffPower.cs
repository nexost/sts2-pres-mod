using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

using PresMod.Characters.Trump.Mechanics;

namespace PresMod.Characters.Trump.Powers;

/// <summary>Enemy debuff. Whenever this enemy attacks, the player who applied the Tariff gains Gold equal to it. "They'll pay for it."</summary>
public sealed class TariffPower : PowerModel
{
	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override async Task AfterAttack(PlayerChoiceContext choiceContext, AttackCommand command)
	{
		if (command.Attacker != Owner || Applier?.Player == null || Amount <= 0)
		{
			return;
		}
		Flash();
		await PlayerCmd.GainGold(Amount, Applier.Player);
	}
}
