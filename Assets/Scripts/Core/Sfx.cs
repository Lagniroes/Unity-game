using System;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// Every sound in the game, synthesized at startup from noise and simple waveforms
/// (gunshots, explosions, monster growls, doors, pickups), plus helpers to play them.
/// </summary>
public static class Sfx
{
    const int Rate = 22050;

    public static AudioClip Pistol, Shotgun, ShotgunPump, Chaingun, RocketLaunch, Explosion, Punch, PunchHit;
    public static AudioClip FireballThrow, FireballHit, Claw, Bite;
    public static AudioClip ZombieSight, ImpSight, DemonSight, CacoSight;
    public static AudioClip ZombieDeath, ImpDeath, DemonDeath, CacoDeath, MonsterPain, MonsterActive;
    public static AudioClip PlayerPain, PlayerDeath, NoWay, ItemPickup, WeaponPickup, PowerUp;
    public static AudioClip DoorOpen, DoorClose, SwitchClick, Splash;

    static AudioSource uiSource;
    static bool built;

    public static void Build()
    {
        if (built) return;
        built = true;

        Pistol = Make("Pistol", 0.35f, Gunshot(18f, 0.45f, 110f));
        Chaingun = Make("Chaingun", 0.25f, Gunshot(24f, 0.55f, 130f));
        Shotgun = Make("Shotgun", 0.9f, Gunshot(5.5f, 0.22f, 70f));
        ShotgunPump = Make("Pump", 0.45f, Clicks(0.05f, 0.25f));
        RocketLaunch = Make("Rocket", 0.7f, Whoosh(0.6f, 3.5f));
        Explosion = Make("Explosion", 1.6f, Boom(2.6f));
        FireballHit = Make("FireballHit", 0.7f, Boom(5f));
        Punch = Make("Punch", 0.2f, Whoosh(0.15f, 18f));
        PunchHit = Make("PunchHit", 0.25f, Gunshot(25f, 0.12f, 60f));
        FireballThrow = Make("Throw", 0.55f, Whoosh(0.4f, 5f));
        Claw = Make("Claw", 0.3f, Whoosh(0.7f, 12f));
        Bite = Make("Bite", 0.35f, Growl(220f, 120f, 0.35f, 30f, 0.6f));
        SwitchClick = Make("Switch", 0.25f, Clicks(0f, 0.08f));
        Splash = Make("Splash", 0.4f, Whoosh(0.3f, 9f));

        ZombieSight = Make("ZombieSight", 0.6f, Growl(110f, 75f, 0.6f, 9f, 0.25f));
        ImpSight = Make("ImpSight", 0.8f, Growl(150f, 80f, 0.8f, 17f, 0.5f));
        DemonSight = Make("DemonSight", 0.9f, Growl(75f, 50f, 0.9f, 6f, 0.7f));
        CacoSight = Make("CacoSight", 0.9f, Growl(170f, 95f, 0.9f, 23f, 0.35f));
        ZombieDeath = Make("ZombieDeath", 0.8f, Growl(170f, 40f, 0.8f, 8f, 0.3f));
        ImpDeath = Make("ImpDeath", 1f, Growl(260f, 50f, 1f, 14f, 0.45f));
        DemonDeath = Make("DemonDeath", 1.1f, Growl(110f, 30f, 1.1f, 5f, 0.6f));
        CacoDeath = Make("CacoDeath", 1.1f, Growl(200f, 40f, 1.1f, 20f, 0.4f));
        MonsterPain = Make("MonsterPain", 0.35f, Growl(220f, 140f, 0.35f, 25f, 0.3f));
        MonsterActive = Make("MonsterActive", 0.6f, Growl(90f, 85f, 0.6f, 4f, 0.5f));

        PlayerPain = Make("PlayerPain", 0.35f, Growl(150f, 95f, 0.35f, 6f, 0.15f));
        PlayerDeath = Make("PlayerDeath", 1.2f, Growl(180f, 45f, 1.2f, 7f, 0.2f));
        NoWay = Make("NoWay", 0.25f, Growl(105f, 85f, 0.25f, 3f, 0.1f));
        ItemPickup = Make("Item", 0.14f, Tones(new[] { 880f, 1320f }, 0.07f));
        WeaponPickup = Make("Weapon", 0.45f, Tones(new[] { 440f, 660f, 880f, 1100f }, 0.1f));
        PowerUp = Make("PowerUp", 0.8f, Sweep(300f, 1400f, 0.8f));
        DoorOpen = Make("DoorOpen", 1f, Rumble(1f, false));
        DoorClose = Make("DoorClose", 1f, Rumble(1f, true));
    }

    /// <summary>Plays a non-positional sound (your own gun, pickups).</summary>
    public static void Play2D(AudioClip clip, float volume = 1f, float pitchVariation = 0.04f)
    {
        if (!clip) return;
        if (!uiSource)
        {
            var go = new GameObject("UI Audio");
            UnityEngine.Object.DontDestroyOnLoad(go);
            uiSource = go.AddComponent<AudioSource>();
            uiSource.spatialBlend = 0f;
        }
        uiSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        uiSource.PlayOneShot(clip, volume);
    }

    /// <summary>Plays a 3D sound at a position (monsters, explosions, doors).</summary>
    public static void PlayAt(AudioClip clip, Vector3 position, float volume = 1f, float pitchVariation = 0.08f)
    {
        if (!clip) return;
        var go = new GameObject("Sound " + clip.name);
        go.transform.position = position;
        var src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.volume = volume;
        src.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        src.spatialBlend = 1f;
        src.rolloffMode = AudioRolloffMode.Linear;
        src.minDistance = 4f;
        src.maxDistance = 45f;
        src.dopplerLevel = 0f;
        src.Play();
        UnityEngine.Object.Destroy(go, clip.length / src.pitch + 0.1f);
    }

    // ---------------------------------------------------------------------
    // Synthesis
    // ---------------------------------------------------------------------

    static AudioClip Make(string name, float duration, Func<float, float> sample)
    {
        int n = Mathf.CeilToInt(duration * Rate);
        var data = new float[n];
        float peak = 0.0001f;
        for (int i = 0; i < n; i++)
        {
            data[i] = sample(i / (float)Rate);
            peak = Mathf.Max(peak, Mathf.Abs(data[i]));
        }
        float gain = 0.9f / peak;
        int fade = Mathf.Min(n, 200);
        for (int i = 0; i < n; i++)
        {
            data[i] *= gain;
            if (i >= n - fade) data[i] *= (n - i) / (float)fade; // no click at the end
        }
        var clip = AudioClip.Create(name, n, 1, Rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    static Func<float> NoiseSource(int seed)
    {
        var rng = new System.Random(seed);
        return () => (float)rng.NextDouble() * 2f - 1f;
    }

    /// <summary>A one-pole low-pass filter: smaller k = darker sound.</summary>
    class LowPass
    {
        float y;
        public float Step(float x, float k) => y += (x - y) * Mathf.Clamp01(k);
    }

    static Func<float, float> Gunshot(float decay, float brightness, float thumpHz)
    {
        var noise = NoiseSource(1);
        var lp = new LowPass();
        return t =>
        {
            float env = Mathf.Exp(-t * decay);
            float crack = lp.Step(noise(), brightness * (0.4f + env)) * env;
            float thump = Mathf.Sin(2f * Mathf.PI * thumpHz * t * (1f - t)) * Mathf.Exp(-t * decay * 1.5f) * 0.8f;
            return crack + thump;
        };
    }

    static Func<float, float> Boom(float decay)
    {
        var noise = NoiseSource(2);
        var lp = new LowPass();
        var lp2 = new LowPass();
        return t =>
        {
            float env = Mathf.Exp(-t * decay) * Mathf.Clamp01(t * 200f);
            float rumble = lp2.Step(lp.Step(noise(), 0.08f + 0.3f * Mathf.Exp(-t * 12f)), 0.3f);
            return rumble * env * 3f + Mathf.Sin(2f * Mathf.PI * 45f * t) * env * 0.4f;
        };
    }

    static Func<float, float> Whoosh(float length, float decay)
    {
        var noise = NoiseSource(3);
        var lp = new LowPass();
        return t =>
        {
            float env = Mathf.Clamp01(t * 30f) * Mathf.Exp(-t * decay) * (t < length ? 1f : Mathf.Exp(-(t - length) * 20f));
            return lp.Step(noise(), 0.05f + 0.25f * Mathf.Clamp01(t / length)) * env;
        };
    }

    static Func<float, float> Clicks(float first, float second)
    {
        var noise = NoiseSource(4);
        var lp = new LowPass();
        return t =>
        {
            float a = t >= first ? Mathf.Exp(-(t - first) * 70f) : 0f;
            float b = t >= second ? Mathf.Exp(-(t - second) * 60f) : 0f;
            return lp.Step(noise(), 0.5f) * (a + b) + Mathf.Sin(2f * Mathf.PI * 900f * t) * (a + b) * 0.3f;
        };
    }

    /// <summary>A gravelly monster voice: a sawtooth sliding from one pitch to another with vibrato and breath.</summary>
    static Func<float, float> Growl(float fromHz, float toHz, float length, float vibratoHz, float breath)
    {
        var noise = NoiseSource(Mathf.RoundToInt(fromHz * 7 + toHz));
        var lp = new LowPass();
        var lpNoise = new LowPass();
        float phase = 0f;
        return t =>
        {
            float u = Mathf.Clamp01(t / length);
            float hz = Mathf.Lerp(fromHz, toHz, u) * (1f + 0.06f * Mathf.Sin(2f * Mathf.PI * vibratoHz * t));
            phase += hz / Rate;
            float saw = (phase % 1f) * 2f - 1f;
            float env = Mathf.Clamp01(t * 25f) * Mathf.Pow(1f - u, 0.7f);
            float voice = lp.Step(saw + noise() * breath, 0.18f);
            return (voice + lpNoise.Step(noise(), 0.3f) * breath * 0.5f) * env;
        };
    }

    static Func<float, float> Tones(float[] notes, float noteLength)
    {
        return t =>
        {
            int i = Mathf.Min(notes.Length - 1, (int)(t / noteLength));
            float local = t - i * noteLength;
            float sq = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * notes[i] * t));
            return sq * 0.4f * Mathf.Exp(-local * 12f);
        };
    }

    static Func<float, float> Sweep(float fromHz, float toHz, float length)
    {
        float phase = 0f;
        return t =>
        {
            float u = Mathf.Clamp01(t / length);
            phase += Mathf.Lerp(fromHz, toHz, u) / Rate;
            return Mathf.Sin(2f * Mathf.PI * phase) * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 14f * t)) * (1f - u);
        };
    }

    static Func<float, float> Rumble(float length, bool slamAtEnd)
    {
        var noise = NoiseSource(5);
        var lp = new LowPass();
        float phase = 0f;
        return t =>
        {
            phase += 55f / Rate;
            float grind = ((phase % 1f) * 2f - 1f) * 0.25f * (0.6f + 0.4f * Mathf.Sin(2f * Mathf.PI * 13f * t));
            float env = Mathf.Clamp01(t * 10f) * Mathf.Clamp01((length - t) * 6f);
            float body = (lp.Step(noise(), 0.06f) * 2.5f + grind) * env;
            if (slamAtEnd && t > length * 0.8f) body += lp.Step(noise(), 0.2f) * Mathf.Exp(-(t - length * 0.8f) * 25f) * 3f;
            return body;
        };
    }
}
