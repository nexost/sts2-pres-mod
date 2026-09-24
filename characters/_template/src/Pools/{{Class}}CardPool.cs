using Godot;
using MegaCrit.Sts2.Core.Models;
using PresMod.Characters.{{Class}}.Cards;

namespace PresMod.Characters.{{Class}}.Pools;

public sealed class {{Class}}CardPool : CardPoolModel
{
	/// <summary>Also the portrait folder: images/packed/card_portraits/{{id}}/&lt;card&gt;.png</summary>
	public override string Title => "{{id}}";

	public override string EnergyColorName => {{Class}}.energyColorName;

	/// <summary>res://materials/cards/frames/card_frame_{{id}}_mat.tres (a hue shift of the shared frame, character.json frame_hsv).</summary>
	public override string CardFrameMaterialPath => "card_frame_{{id}}";

	public override Color DeckEntryCardColor => new Color("{{deck}}");

	public override Color EnergyOutlineColor => new Color("{{dark}}");

	public override bool IsColorless => false;

	/// <summary>Every card from characters/{{id}}/design/cards.json (tokens that only borrow the frame stay out).</summary>
	protected override CardModel[] GenerateAllCards()
	{
		return new CardModel[]
		{
			ModelDb.Card<Strike{{Class}}>(),
			ModelDb.Card<Defend{{Class}}>(),
			// Stubs so card rewards (3 different cards) and the shop (Attacks, Skills, a Power) work from day one.
			// Remove them once the real cards are in.
			ModelDb.Card<Jab{{Class}}>(),
			ModelDb.Card<OneTwo{{Class}}>(),
			ModelDb.Card<Brace{{Class}}>(),
			ModelDb.Card<Regroup{{Class}}>(),
			ModelDb.Card<Resolve{{Class}}>()
		};
	}
}
