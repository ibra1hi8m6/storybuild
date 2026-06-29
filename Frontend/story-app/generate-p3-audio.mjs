// Generates p3-q1..5.wav for Part 3 of the placement test.
// Run from story-app folder: node generate-p3-audio.mjs
import { writeFileSync, mkdirSync, existsSync } from 'fs';
import { join } from 'path';

const API_KEYS = [
  'AQ.Ab8RN6LmGNQzZLrOFUVQ0D1vi_edJ4jXbecAj60wWYJaXDjpcA',
  'AQ.Ab8RN6JmkUeZeYi2CTMCBAeWvEWK_sEW5gJZ1PJyFJxQrz0P4A',
];
let keyIdx = 0;

const SAMPLE_RATE = 24000;
const OUTPUT_DIR  = './public/audio/placement';
const sleep = ms => new Promise(r => setTimeout(r, ms));

mkdirSync(OUTPUT_DIR, { recursive: true });

// ___ replaced with ... so TTS produces a natural short pause
const clips = [
  { file: 'p3-q1', text: 'رتب الكلمات: يلعب – الولد – الكرة' },
  { file: 'p3-q2', text: 'رتب الكلمات: القطة – تشرب – الحليب' },
  { file: 'p3-q3', text: 'أكمل الجملة: الشمس ... في السماء.' },
  { file: 'p3-q4', text: 'أكمل الجملة: الفراشة ... بين الزهور.' },
  { file: 'p3-q5', text: 'أكمل الجملة: الولد ... القصة.' },
];

function pcmToWav(base64Pcm) {
  const pcm        = Buffer.from(base64Pcm, 'base64');
  const channels   = 1, bitsPerSample = 16;
  const byteRate   = SAMPLE_RATE * channels * bitsPerSample / 8;
  const blockAlign = channels * bitsPerSample / 8;

  const buf = Buffer.alloc(44 + pcm.length);
  buf.write('RIFF', 0);               buf.writeUInt32LE(36 + pcm.length, 4);
  buf.write('WAVE', 8);
  buf.write('fmt ', 12);              buf.writeUInt32LE(16, 16);
  buf.writeUInt16LE(1, 20);           buf.writeUInt16LE(channels, 22);
  buf.writeUInt32LE(SAMPLE_RATE, 24); buf.writeUInt32LE(byteRate, 28);
  buf.writeUInt16LE(blockAlign, 32);  buf.writeUInt16LE(bitsPerSample, 34);
  buf.write('data', 36);              buf.writeUInt32LE(pcm.length, 40);
  pcm.copy(buf, 44);
  return buf;
}

async function generateClip(file, text, retries = 5) {
  for (let attempt = 0; attempt < retries; attempt++) {
    const key = API_KEYS[keyIdx];
    const url = `https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-preview-tts:generateContent?key=${key}`;
    const body = {
      contents: [{ parts: [{ text: `اقرأ هذا النص العربي بصوت واضح وودي للأطفال: ${text}` }] }],
      generationConfig: {
        responseModalities: ['AUDIO'],
        speechConfig: { voiceConfig: { prebuiltVoiceConfig: { voiceName: 'Kore' } } },
      },
    };

    const res = await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(body),
    });

    if (res.status === 429) {
      keyIdx = (keyIdx + 1) % API_KEYS.length;
      const wait = 3000 * (attempt + 1);
      console.log(`  rate-limited — waiting ${wait / 1000}s…`);
      await sleep(wait);
      continue;
    }

    if (!res.ok) {
      const t = await res.text();
      throw new Error(`HTTP ${res.status}: ${t}`);
    }

    const json = await res.json();
    const pcm  = json?.candidates?.[0]?.content?.parts?.[0]?.inlineData?.data;

    if (!pcm) {
      const wait = 4000 * (attempt + 1);
      console.log(`  no audio for "${text}" (attempt ${attempt + 1}) — waiting ${wait / 1000}s…`);
      await sleep(wait);
      continue;
    }

    writeFileSync(join(OUTPUT_DIR, `${file}.wav`), pcmToWav(pcm));
    console.log(`✓  ${file}.wav`);
    return;
  }

  throw new Error(`Failed after ${retries} attempts: "${text}"`);
}

for (const { file, text } of clips) {
  if (existsSync(join(OUTPUT_DIR, `${file}.wav`))) {
    console.log(`—  ${file}.wav already exists, skipping`);
    continue;
  }
  await generateClip(file, text);
  await sleep(1000);
}

console.log('\nDone.');
