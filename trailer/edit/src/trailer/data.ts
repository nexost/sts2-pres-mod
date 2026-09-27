// The edit's data, read from timeline.json (see its _about), and small shared helpers.
import {createContext, useContext} from 'react';
import {staticFile, useVideoConfig} from 'remotion';
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
	/** The vertical cut's framing (scale 1 = the source's full height fills the frame); defaults to cam. */
	vcam?: Cam;
	vcamTo?: Cam;
	/** The vertical cut shows the whole frame letterboxed over a blurred copy (for things too wide to crop). */
	vfit?: boolean;
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

/** True inside the vertical (9:16) cut. */
export const VerticalContext = createContext(false);

/**
 * The frame and a size unit U for text and cards: the width in the landscape cut; in the vertical cut 1.45x its
 * width, so text reads about as big on a phone held upright as the landscape cut's does on a phone held sideways.
 */
export const useLayout = () => {
	const vertical = useContext(VerticalContext);
	const {width, height, fps} = useVideoConfig();
	return {vertical, width, height, fps, U: vertical ? width * 1.45 : width};
};

/** Deterministic pseudo-random in [0, 1) from an integer seed. */
export const rand = (seed: number) => {
	const x = Math.sin(seed * 12.9898 + 78.233) * 43758.5453;
	return x - Math.floor(x);
};
