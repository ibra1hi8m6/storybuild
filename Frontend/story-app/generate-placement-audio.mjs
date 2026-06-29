// One-time script: generates missing placement audio files.
// Run from story-app folder: node generate-placement-audio.mjs
import { writeFileSync, mkdirSync, existsSync } from 'fs';
import { join } from 'path';

const API_KEYS = [
  'AQ.Ab8RN6LmGNQzZLrOFUVQ0D1vi_edJ4jXbecAj60wWYJaXDjpcA',
  'AQ.Ab8RN6JmkUeZeYi2CTMCBAeWvEWK_sEW5gJZ1PJyFJxQrz0P4A',
];
let keyIdx = 0;

const SAMPLE_RATE = 24000;
const OUTPUT_DIR  = './public/audio/placement';

mkdirSync(OUTPUT_DIR, { recursive: true });

// Files to generate — add more here if needed
const clips = [
  { file: 'ayi-harf', text: 'أي حرف تسمعه؟' },

  // Regenerate Part 1 as a single shared file (all 5 questions say the same thing)
  { file: 'ma-hatha', text: 'ما هذا؟' },

  // Part 2 — letter sounds (keep in sync with p2-q*.wav if those already sound good)
  { file: 'p2-q1', text: 'ألف' },
  { file: 'p2-q2', text: 'باء' },
  { file: 'p2-q3', text: 'تاء' },
  { file: 'p2-q4', text: 'ثاء' },
  { file: 'p2-q5', text: 'جيم' },

  // Part 3
  { file: 'p3-q1', text: 'رتب الكلمات: يلعب، الولد، الكرة' },
  { file: 'p3-q2', text: 'رتب الكلمات: القطة، تشرب، الحليب' },
  { file: 'p3-q3', text: 'أكمل الجملة: الشمس في السماء' },
  { file: 'p3-q4', text: 'أكمل الجملة: الفراشة بين الزهور' },
  { file: 'p3-q5', text: 'أكمل الجملة: الولد القصة' },
];

function pcmToWav(base64Pcm) {
  const pcm       = Buffer.from(base64Pcm, 'base64');
  const channels  = 1, bitsPerSample = 16;
  const byteRate  = SAMPLE_RATE * channels * bitsPerSample / 8;
  const blockAlign = channels * bitsPerSample / 8;

  const buf = Buffer.alloc(44 + pcm.length);
  buf.write('RIFF', 0);                          buf.writeUInt32LE(36 + pcm.length, 4);
  buf.write('WAVE', 8);
  buf.write('fmt ', 12);                         buf.writeUInt32LE(16, 16);
  buf.writeUInt16LE(1, 20);                      buf.writeUInt16LE(channels, 22);
  buf.writeUInt32LE(SAMPLE_RATE, 24);            buf.writeUInt32LE(byteRate, 28);
  buf.writeUInt16LE(blockAlign, 32);             buf.writeUInt16LE(bitsPerSample, 34);
  buf.write('data', 36);                         buf.writeUInt32LE(pcm.length, 40);
  pcm.copy(buf, 44);
  return buf;
}

async function generateClip(file, text) {
  const outPath = join(OUTPUT_DIR, `${file}.wav`);

  for (let attempt = 0; attempt < API_KEYS.length; attempt++) {
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
      continue;
    }

    if (!res.ok) {
      const errText = await res.text();
      throw new Error(`HTTP ${res.status} for "${text}": ${errText}`);
    }

    const json = await res.json();
    const pcm  = json?.candidates?.[0]?.content?.parts?.[0]?.inlineData?.data;
    if (!pcm) throw new Error(`No audio data returned for "${text}"`);

    writeFileSync(outPath, pcmToWav(pcm));
    console.log(`✓  ${file}.wav  ("${text}")`);
    return;
  }

  throw new Error(`All API keys exhausted for "${text}"`);
}

for (const { file, text } of clips) {
  await generateClip(file, text);
  await new Promise(r => setTimeout(r, 600)); // small delay between calls
}

console.log('\nDone — all placement audio files generated.');
