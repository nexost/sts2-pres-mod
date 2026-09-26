// Render stills of a composition at chosen times (one bundle for all), for checking framing and graphics.
//   node stills.mjs Trailer 12.9 15.2 24.9 [--scale 0.5] [--out out/stills]
import {bundle} from '@remotion/bundler';
import {renderStill, selectComposition} from '@remotion/renderer';
import fs from 'node:fs';
import path from 'node:path';
import {fileURLToPath} from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const args = process.argv.slice(2);
const opt = (name, fallback) => {
	const i = args.indexOf(name);
	if (i < 0) return fallback;
	const v = args[i + 1];
	args.splice(i, 2);
	return v;
};
const scale = Number(opt('--scale', '0.5'));
const out = path.resolve(here, opt('--out', 'out/stills'));
const [id, ...times] = args;
fs.mkdirSync(out, {recursive: true});

const serveUrl = await bundle({entryPoint: path.join(here, 'src/index.ts'), publicDir: path.resolve(here, '../../build/trailer/media')});
const composition = await selectComposition({serveUrl, id, inputProps: {}});
for (const t of times) {
	const frame = Math.min(composition.durationInFrames - 1, Math.round(Number(t) * composition.fps));
	const file = path.join(out, `${id}_${Number(t).toFixed(2)}.jpg`);
	await renderStill({serveUrl, composition, frame, output: file, scale, imageFormat: 'jpeg', jpegQuality: 88, inputProps: {}});
	console.log(file);
}
