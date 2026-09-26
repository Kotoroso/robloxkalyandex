using UnityEngine;

namespace DragonHeist
{
    /// <summary>Лёгкие эффекты частиц, создаваемые кодом (вспышки, искры, пыль, линии скорости).</summary>
    public static class Fx
    {
        static ParticleSystem Make(string name, Vector3 pos, Transform parent, bool additive)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.Particle(null, additive);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            var main = ps.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            return ps;
        }

        static void FadeAndShrink(ParticleSystem ps)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = new ParticleSystem.MinMaxGradient(g);
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));
        }

        /// <summary>Разовая вспышка частиц (вылупление, покупка, поимка, перерождение).</summary>
        public static void Burst(Vector3 pos, Color color, int count, float speed, float size, float life, float gravity = 0.6f, bool additive = true)
        {
            var ps = Make("FxBurst", pos, null, additive);
            var main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.6f, life);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.5f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.6f, size);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.5f));
            main.gravityModifier = gravity;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.4f;
            FadeAndShrink(ps);
            ps.Play();
        }

        /// <summary>Конфетти (разноцветные квадратики) — для вылупления и перерождения.</summary>
        public static void Confetti(Vector3 pos, int count)
        {
            var ps = Make("FxConfetti", pos, null, false);
            var main = ps.main;
            main.duration = 0.2f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6f, 12f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.45f);
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.3f, 0.3f), 0f), new GradientColorKey(new Color(1f, 0.9f, 0.2f), 0.33f),
                              new GradientColorKey(new Color(0.3f, 0.9f, 0.4f), 0.66f), new GradientColorKey(new Color(0.3f, 0.6f, 1f), 1f) },
                      new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(g) { mode = ParticleSystemGradientMode.RandomColor };
            main.gravityModifier = 1.2f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);
            main.stopAction = ParticleSystemStopAction.Destroy;
            var em = ps.emission;
            em.rateOverTime = 0;
            em.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)count) });
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 35f;
            sh.radius = 0.5f;
            sh.rotation = new Vector3(-90f, 0, 0);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = Mats.Particle(Mats.SquareTexture, false);
            ps.Play();
        }

        /// <summary>Постоянные искорки (яйца на пьедесталах, редкие драконы).</summary>
        public static ParticleSystem Sparkles(Transform parent, Vector3 localPos, Color color, float radius, float rate)
        {
            var ps = Make("FxSparkles", parent.position, parent, true);
            ps.transform.localPosition = localPos;
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.gravityModifier = -0.15f;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            var em = ps.emission;
            em.rateOverTime = rate;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = radius;
            FadeAndShrink(ps);
            ps.Play();
            return ps;
        }

        /// <summary>Языки пламени (огненные драконы).</summary>
        public static ParticleSystem Flames(Transform parent, Vector3 localPos, float radius)
        {
            var ps = Make("FxFlames", parent.position, parent, true);
            ps.transform.localPosition = localPos;
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.4f * radius, 0.8f * radius);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.1f), new Color(1f, 0.85f, 0.2f));
            main.gravityModifier = -0.4f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 14f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = radius;
            FadeAndShrink(ps);
            ps.Play();
            return ps;
        }

        /// <summary>Линии скорости вокруг игрока на беговой дорожке. Включать/выключать через emission.</summary>
        public static ParticleSystem SpeedLines(Transform parent)
        {
            var ps = Make("FxSpeedLines", parent.position, parent, true);
            ps.transform.localPosition = new Vector3(0, 1.5f, 1.5f);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 0.35f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(14f, 20f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.14f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.7f, 1f, 1f, 0.8f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission;
            em.rateOverTime = 0;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(2.4f, 2.6f, 0.2f);
            sh.rotation = new Vector3(0, 180f, 0);
            var r = ps.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Stretch;
            r.velocityScale = 0.08f;
            r.lengthScale = 2f;
            FadeAndShrink(ps);
            ps.Play();
            return ps;
        }

        /// <summary>Пыль из-под ног при беге.</summary>
        public static ParticleSystem Dust(Transform parent)
        {
            var ps = Make("FxDust", parent.position, parent, false);
            ps.transform.localPosition = new Vector3(0, 0.15f, 0);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.9f, 0.9f, 0.85f, 0.6f));
            main.gravityModifier = -0.05f;
            var em = ps.emission;
            em.rateOverTime = 0;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Circle;
            sh.radius = 0.4f;
            sh.rotation = new Vector3(-90f, 0, 0);
            FadeAndShrink(ps);
            ps.Play();
            return ps;
        }

        public static void SetRate(ParticleSystem ps, float rate)
        {
            if (ps == null) return;
            var em = ps.emission;
            em.rateOverTime = rate;
        }
    }
}
