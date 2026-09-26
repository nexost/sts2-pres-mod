// The edit's data, read from timeline.json (see its _about), and small shared helpers.
import {staticFile} from 'remotion';
import timeline from '../timeline.json';

/** [scale, x, y]: zoom into the source, x and y the visible centre (0..1 of the source frame). */
export type Cam = [number, number, number];

export type Shot = {
	at: number;
	dur: number;
	src: string;
	from: number;
	cam?: Cam;
	camTo?: Cam;
	rate?: number;
	look?: string;
	freeze?: boolean;
	fadeIn?: number;
	left?: string;
	right?: string;
};

export type Graphic = {type: string; at: number; dur?: number} & Record<string, any>;

export const SHOTS = timeline.shots as Shot[];
export const GRAPHICS = timeline.graphics as Graphic[];
export const SFX = timeline.sfx as {name: string; at: number; gain: number}[];
export const BUTTON = timeline.button;
export const DURATION = timeline.duration;

export const toFrames = (s: number, fps: number) => Math.round(s * fps);

/** A shot's video: a captured clip (media/clips) or a painted shot (media/ai). */
export const videoUrl = (src: string) => staticFile(src.startsWith('ai/') ? `${src}.mp4` : `clips/${src}.mp4`);

/** A card render, "trump/make_it_rain" -> media/cards/trump/make_it_rain.png. */
export const cardUrl = (card: string) => staticFile(`cards/${card}.png`);

export const clamp01 = {extrapolateLeft: 'clamp', extrapolateRight: 'clamp'} as const;

/** Deterministic pseudo-random in [0, 1) from an integer seed. */
export const rand = (seed: number) => {
	const x = Math.sin(seed * 12.9898 + 78.233) * 43758.5453;
	return x - Math.floor(x);
};
