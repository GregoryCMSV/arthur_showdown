window.arthurAudio = (function () {
    let ctx = null;
    let bgmGain = null;
    let sfxGain = null;
    let bgmTimer = null;
    let bgmPlaying = false;
    let musicEnabled = true;
    let sfxEnabled = true;
    let musicVolume = 0.28;
    let sfxVolume = 0.65;

    let customHitBuffer = null;
    let customMoveBuffer = null;
    let customBgmAudio = null;
    let buffersChecked = false;

    function ensureContext() {
        if (!ctx) {
            const AudioCtx = window.AudioContext || window.webkitAudioContext;
            if (!AudioCtx) return null;
            ctx = new AudioCtx();
            bgmGain = ctx.createGain();
            bgmGain.gain.value = musicEnabled ? musicVolume : 0;
            bgmGain.connect(ctx.destination);

            sfxGain = ctx.createGain();
            sfxGain.gain.value = sfxEnabled ? sfxVolume : 0;
            sfxGain.connect(ctx.destination);

            tryLoadCustomFiles();
        }
        if (ctx.state === "suspended") {
            ctx.resume().catch(() => { });
        }
        return ctx;
    }

    async function tryFetchBuffer(url) {
        try {
            const resp = await fetch(url, { method: "GET" });
            if (!resp.ok) return null;
            const contentType = resp.headers.get("content-type") || "";
            if (contentType.includes("text/html")) return null;
            const arr = await resp.arrayBuffer();
            if (!arr || arr.byteLength < 100) return null;
            return await ctx.decodeAudioData(arr);
        } catch {
            return null;
        }
    }

    async function tryLoadCustomFiles() {
        if (buffersChecked || !ctx) return;
        buffersChecked = true;

        customHitBuffer = await tryFetchBuffer("audio/hit.mp3") || await tryFetchBuffer("audio/hit.wav");
        customMoveBuffer = await tryFetchBuffer("audio/move.mp3") || await tryFetchBuffer("audio/move.wav");

        try {
            const resp = await fetch("audio/bgm.mp3", { method: "HEAD" });
            const contentType = resp.headers.get("content-type") || "";
            if (resp.ok && !contentType.includes("text/html")) {
                customBgmAudio = new Audio("audio/bgm.mp3");
                customBgmAudio.loop = true;
                customBgmAudio.volume = musicEnabled ? musicVolume : 0;
                if (bgmPlaying && musicEnabled) {
                    stopProceduralBgm();
                    customBgmAudio.play().catch(() => { });
                }
            }
        } catch { }
    }

    function unlockOnInteraction() {
        const c = ensureContext();
        if (c && bgmPlaying && musicEnabled) {
            if (customBgmAudio) {
                customBgmAudio.play().catch(() => { });
            } else if (!bgmTimer) {
                startProceduralBgm();
            }
        }
    }

    window.addEventListener("pointerdown", unlockOnInteraction, { passive: true });
    window.addEventListener("keydown", unlockOnInteraction, { passive: true });

    function startProceduralBgm() {
        if (bgmTimer) return;
        const c = ensureContext();
        if (!c) return;

        // Progressao medieval sombria em La menor (Am - F - G - Em)
        const bassNotes = [110.00, 110.00, 87.31, 87.31, 98.00, 98.00, 82.41, 98.00];
        const arpNotes = [
            220.00, 261.63, 329.63, 261.63,
            174.61, 220.00, 261.63, 220.00,
            196.00, 246.94, 293.66, 246.94,
            164.81, 196.00, 246.94, 196.00
        ];
        let step = 0;

        const tick = () => {
            if (!bgmPlaying || !musicEnabled || !ctx) return;
            if (ctx.state === "suspended") return;

            const now = ctx.currentTime;
            const arpFreq = arpNotes[step % arpNotes.length];

            // Nota de harpa/laute medieval
            const osc = ctx.createOscillator();
            const gain = ctx.createGain();
            osc.type = "triangle";
            osc.frequency.setValueAtTime(arpFreq, now);

            gain.gain.setValueAtTime(0.001, now);
            gain.gain.linearRampToValueAtTime(0.18, now + 0.03);
            gain.gain.exponentialRampToValueAtTime(0.001, now + 0.36);

            osc.connect(gain);
            gain.connect(bgmGain);
            osc.start(now);
            osc.stop(now + 0.37);

            // Nota grave a cada 2 passos
            if (step % 2 === 0) {
                const bassFreq = bassNotes[(step / 2) % bassNotes.length];
                const bOsc = ctx.createOscillator();
                const bGain = ctx.createGain();
                bOsc.type = "sine";
                bOsc.frequency.setValueAtTime(bassFreq, now);

                bGain.gain.setValueAtTime(0.001, now);
                bGain.gain.linearRampToValueAtTime(0.24, now + 0.05);
                bGain.gain.exponentialRampToValueAtTime(0.001, now + 0.72);

                bOsc.connect(bGain);
                bGain.connect(bgmGain);
                bOsc.start(now);
                bOsc.stop(now + 0.74);
            }

            step++;
        };

        tick();
        bgmTimer = setInterval(tick, 380);
    }

    function stopProceduralBgm() {
        if (bgmTimer) {
            clearInterval(bgmTimer);
            bgmTimer = null;
        }
    }

    function startBgm() {
        bgmPlaying = true;
        if (!musicEnabled) return;
        ensureContext();
        if (customBgmAudio) {
            stopProceduralBgm();
            customBgmAudio.volume = musicVolume;
            customBgmAudio.play().catch(() => { });
        } else {
            startProceduralBgm();
        }
    }

    function stopBgm() {
        bgmPlaying = false;
        stopProceduralBgm();
        if (customBgmAudio) {
            customBgmAudio.pause();
        }
    }

    function playBufferWithPitch(buffer, pitch, volumeMult) {
        const src = ctx.createBufferSource();
        const g = ctx.createGain();
        src.buffer = buffer;
        src.playbackRate.value = pitch;
        g.gain.value = volumeMult || 1.0;
        src.connect(g);
        g.connect(sfxGain);
        src.start(0);
    }

    function playHit(pitch, isArrow) {
        if (!sfxEnabled) return;
        const c = ensureContext();
        if (!c) return;

        const p = Math.max(0.5, Math.min(2.0, pitch || 1.0));

        if (customHitBuffer) {
            playBufferWithPitch(customHitBuffer, p, 1.0);
            return;
        }

        const now = c.currentTime;

        // 1. Ruido de corte / impacto com filtro variando pelo pitch
        const bufferSize = Math.floor(c.sampleRate * 0.14);
        const noiseBuffer = c.createBuffer(1, bufferSize, c.sampleRate);
        const data = noiseBuffer.getChannelData(0);
        for (let i = 0; i < bufferSize; i++) {
            data[i] = (Math.random() * 2 - 1) * Math.exp(-i / (bufferSize * 0.35));
        }

        const noise = c.createBufferSource();
        noise.buffer = noiseBuffer;
        noise.playbackRate.value = p;

        const filter = c.createBiquadFilter();
        filter.type = "bandpass";
        filter.frequency.setValueAtTime((isArrow ? 1800 : 1100) * p, now);
        filter.Q.value = 1.4;

        const nGain = c.createGain();
        nGain.gain.setValueAtTime(0.55, now);
        nGain.gain.exponentialRampToValueAtTime(0.01, now + 0.13);

        noise.connect(filter);
        filter.connect(nGain);
        nGain.connect(sfxGain);
        noise.start(now);

        // 2. Corpo do impacto (grave/medio com sweep descendente ajustado pelo pitch)
        const osc = c.createOscillator();
        const oGain = c.createGain();
        osc.type = isArrow ? "triangle" : "sawtooth";
        const startFreq = (isArrow ? 520 : 260) * p;
        const endFreq = (isArrow ? 110 : 55) * p;
        osc.frequency.setValueAtTime(startFreq, now);
        osc.frequency.exponentialRampToValueAtTime(endFreq, now + 0.14);

        oGain.gain.setValueAtTime(0.45, now);
        oGain.gain.exponentialRampToValueAtTime(0.008, now + 0.15);

        osc.connect(oGain);
        oGain.connect(sfxGain);
        osc.start(now);
        osc.stop(now + 0.16);
    }

    function playMove(pitch) {
        if (!sfxEnabled) return;
        const c = ensureContext();
        if (!c) return;

        const p = Math.max(0.6, Math.min(1.6, pitch || 1.0));

        if (customMoveBuffer) {
            playBufferWithPitch(customMoveBuffer, p, 0.7);
            return;
        }

        const now = c.currentTime;
        const osc = c.createOscillator();
        const gain = c.createGain();
        osc.type = "sine";
        osc.frequency.setValueAtTime(145 * p, now);
        osc.frequency.exponentialRampToValueAtTime(75 * p, now + 0.065);

        gain.gain.setValueAtTime(0.22, now);
        gain.gain.exponentialRampToValueAtTime(0.008, now + 0.07);

        osc.connect(gain);
        gain.connect(sfxGain);
        osc.start(now);
        osc.stop(now + 0.075);
    }

    function setConfig(enableMusic, enableSfx) {
        musicEnabled = !!enableMusic;
        sfxEnabled = !!enableSfx;

        if (bgmGain && ctx) {
            bgmGain.gain.setValueAtTime(musicEnabled ? musicVolume : 0, ctx.currentTime);
        }
        if (sfxGain && ctx) {
            sfxGain.gain.setValueAtTime(sfxEnabled ? sfxVolume : 0, ctx.currentTime);
        }
        if (customBgmAudio) {
            customBgmAudio.volume = musicEnabled ? musicVolume : 0;
        }

        if (musicEnabled && bgmPlaying) {
            startBgm();
        } else if (!musicEnabled) {
            stopProceduralBgm();
            if (customBgmAudio) customBgmAudio.pause();
        }
    }

    return {
        startBgm,
        stopBgm,
        playHit,
        playMove,
        setConfig
    };
})();
