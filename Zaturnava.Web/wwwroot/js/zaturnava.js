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
        // A fanfare for an enemy Raja taken: a rising call, answered by a held major chord.
        triumph: () => {
            [392, 523, 659, 784].forEach((frequency, i) => tone(frequency, 0.16, "sawtooth", 0.07, i * 0.12));
            [523, 659, 784, 1047].forEach(frequency => tone(frequency, 0.9, "triangle", 0.12, 0.5));
        },
        // A lament for one of the team's Rajas taken: three slow falling notes, the last held and sinking.
        lament: () => {
            [392, 370, 349].forEach((frequency, i) => tone(frequency, 0.4, "sine", 0.16, i * 0.38));
            tone(330, 1.2, "sine", 0.16, 1.14);
            tone(165, 1.2, "triangle", 0.08, 1.14);
        },
        rescued: () => {
            [523, 659, 784].forEach((frequency, i) => tone(frequency, 0.14, "triangle", 0.18, i * 0.11));
            tone(1047, 0.5, "triangle", 0.2, 0.33);
            tone(784, 0.5, "sine", 0.1, 0.33);
        },
        // One of the team's Rajas back on the board: a rising arpeggio, ringing out like a bell.
        restored: () => {
            [440, 554, 659, 880].forEach((frequency, i) => tone(frequency, 0.18, "sine", 0.16, i * 0.1));
            [880, 1109, 1319].forEach(frequency => tone(frequency, 1.1, "sine", 0.08, 0.42));
        },
        // An enemy Raja back on the board: two low notes a semitone apart, beating against each other.
        dread: () => {
            tone(147, 1.3, "sawtooth", 0.07, 0);
            tone(156, 1.3, "sawtooth", 0.07, 0);
            tone(98, 1.4, "sine", 0.18, 0.2);
        },
        // The team gaining command of an army, or an enemy losing it: a horn call, up a fourth and a third.
        rally: () => {
            tone(392, 0.16, "triangle", 0.16, 0);
            tone(523, 0.16, "triangle", 0.16, 0.18);
            tone(659, 0.55, "triangle", 0.18, 0.36);
        },
        // An enemy gaining command of an army, or the team losing it: three heavy low beats.
        menace: () => [0, 0.32, 0.64].forEach(delay => {
            tone(82, 0.28, "sawtooth", 0.1, delay);
            tone(55, 0.3, "sine", 0.25, delay);
        }),
        // The team's empire begun, or an enemy's over: a cadence, from the fourth to the fifth to home.
        glory: () => [[349, 440, 523], [392, 494, 587], [523, 659, 784, 1047]].forEach((chord, i) =>
            chord.forEach(frequency => tone(frequency, i === 2 ? 1.2 : 0.3, "triangle", 0.08, i * 0.32))
        ),
        // An enemy empire begun, or the team's over: a slow minor chord sinking over a low drone.
        doom: () => {
            [[220, 262, 330], [208, 247, 311]].forEach((chord, i) =>
                chord.forEach(frequency => tone(frequency, 0.9, "sine", 0.08, i * 0.6))
            );
            tone(55, 1.6, "sawtooth", 0.05, 0);
        },
    };

    // How long apart sounds told together are played, so that each is heard.
    const gap = 1.6;

    return {
        play(...names) {
            try {
                audio = audio ?? new AudioContext();
                if (audio.state === "suspended") {
                    audio.resume();
                }
                names.forEach((name, i) => setTimeout(() => {
                    try {
                        (sounds[name] || (() => { }))();
                    } catch {
                        // no sound, then
                    }
                }, i * gap * 1000));
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
