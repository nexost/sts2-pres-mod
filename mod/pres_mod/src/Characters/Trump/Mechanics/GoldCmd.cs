using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace PresMod.Characters.Trump.Mechanics;

/// <summary>"Pay X Gold": can only be played with at least X Gold; lose X Gold.</summary>
public static class GoldCmd
{
	public static bool CanPay(Player player, int amount) => player.Gold >= amount;

	public static async Task Pay(PlayerChoiceContext choiceContext, Player player, int amount)
	{
		if (amount <= 0)
		{
			return;
		}
		await PlayerCmd.LoseGold(amount, player);
		foreach (IAfterPayGold listener in ModHooks.ListenersOf<IAfterPayGold>(player.Creature))
		{
			await listener.AfterPayGold(choiceContext, player, amount);
		}
	}
}
