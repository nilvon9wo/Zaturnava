// What the page needs from the browser beyond Blazor: sounds, made on the spot (no files to fetch),
// a memory that outlives the page (seats held, settings), and the colour theme. Each fails quietly:
// a browser without sound or storage still plays.
window.zaturnava = (() => {
    let audio = null;

    const tone = (frequency, seconds, type, volume, delay) => {
        const start = audio.currentTime + delay;
        const oscillator = audio.createOscillator();
        const gain = audio.createGain();
        oscillator.type = type;
        oscillator.frequency.setValueAtTime(frequency, start);
        gain.gain.setValueAtTime(volume, start);
        gain.gain.exponentialRampToValueAtTime(0.0001, start + seconds);
        oscillator.connect(gain).connect(audio.destination);
        oscillator.start(start);
        oscillator.stop(start + seconds);
    };

    // A tone gliding from one pitch to another and back, again and again: a siren.
    const siren = (low, high, sweeps, seconds, volume) => {
        const start = audio.currentTime;
        const oscillator = audio.createOscillator();
        const gain = audio.createGain();
        oscillator.type = "sawtooth";
        oscillator.frequency.setValueAtTime(low, start);
        for (let i = 0; i < sweeps; i++) {
            oscillator.frequency.linearRampToValueAtTime(high, start + (i + 0.5) * seconds / sweeps);
            oscillator.frequency.linearRampToValueAtTime(low, start + (i + 1) * seconds / sweeps);
        }
        gain.gain.setValueAtTime(volume, start);
        gain.gain.setValueAtTime(volume, start + seconds - 0.1);
        gain.gain.exponentialRampToValueAtTime(0.0001, start + seconds);
        oscillator.connect(gain).connect(audio.destination);
        oscillator.start(start);
        oscillator.stop(start + seconds);
    };

    const sounds = {
        rolled: () => [0, 1, 2, 3].forEach(i => tone(250 + Math.random() * 450, 0.05, "square", 0.04, i * 0.06)),
        moved: () => tone(520, 0.09, "triangle", 0.2, 0),
        captured: () => { tone(200, 0.2, "sawtooth", 0.12, 0); tone(110, 0.3, "sine", 0.25, 0.04); },
        turn: () => { tone(660, 0.14, "sine", 0.12, 0); tone(880, 0.22, "sine", 0.12, 0.13); },
        alarm: () => siren(520, 880, 3, 1.5, 0.06),
        rescued: () => {
            [523, 659, 784].forEach((frequency, i) => tone(frequency, 0.14, "triangle", 0.18, i * 0.11));
            tone(1047, 0.5, "triangle", 0.2, 0.33);
            tone(784, 0.5, "sine", 0.1, 0.33);
        },
    };

    return {
        play(sound) {
            try {
                audio = audio ?? new AudioContext();
                if (audio.state === "suspended") {
                    audio.resume();
                }
                (sounds[sound] || (() => { }))();
            } catch {
                // no sound, then
            }
        },
        load(key) {
            try {
                return localStorage.getItem(key);
            } catch {
                return null;
            }
        },
        save(key, value) {
            try {
                localStorage.setItem(key, value);
            } catch {
                // forgotten, then
            }
        },
        theme(name) {
            document.documentElement.dataset.theme = name;
        },
    };
})();
