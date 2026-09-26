// The "holy shit" hits: every big sound effect in the timeline kicks the picture (zoom punch, shake, a flash and a few
// deep-fried frames), decaying over ~0.3 s. Strength follows the effect and its level in the mix.
import {SFX} from './data';

const KICK: Record<string, number> = {impact: 1, braam: 1, logo_hit: 1.25, stamp: 0.8, sub_drop: 1.1, cash_register: 0.55, laser_beam: 0.7};

const level = (gain: number) => (gain >= -3 ? 1 : gain >= -7 ? 0.65 : 0.4);

/** 0..~1.25 at time t (seconds). */
export const hitAt = (t: number) => {
	let h = 0;
	for (const s of SFX) {
		const k = KICK[s.name];
		const d = t - s.at;
		if (k && d >= 0 && d < 0.35) {
			h = Math.max(h, Math.exp(-d / 0.07) * k * level(s.gain));
		}
	}
	return h;
};

export const kick = (t: number, frame: number) => {
	const h = hitAt(t);
	return {
		h,
		scale: 1 + h * 0.055,
		x: Math.sin(frame * 2.7) * 20 * h,
		y: Math.cos(frame * 3.3) * 14 * h,
		flash: h > 0.45 ? (h - 0.45) * 0.45 : 0,
		filter: h > 0.3 ? `saturate(${1 + h * 1.1}) contrast(${1 + h * 0.35}) brightness(${1 + h * 0.08})` : 'none',
	};
};
