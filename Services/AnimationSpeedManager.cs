using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

public static class AnimationSpeedManager
{
    private static readonly Harmony Harmony = new("kg.ValheimEnchantmentSystem.AnimationSpeedManager");
    private static readonly MethodInfo TargetMethod = AccessTools.DeclaredMethod(typeof(CharacterAnimEvent), nameof(CharacterAnimEvent.CustomFixedUpdate));
    private static readonly Dictionary<int, List<Handler>> HandlersByPriority = new();

    private static Handler[][] _handlers = Array.Empty<Handler[]>();
    private static bool _markerPatchInstalled;
    private static bool _wrapperInstalled;
    private static int _handlerIndex;
    private static bool _changed;

    public delegate double Handler(Character character, double speed);

    [PublicAPI]
    public static void Add(Handler handler, int priority = Priority.Normal)
    {
        if (!_markerPatchInstalled)
        {
            Harmony.Patch(TargetMethod, finalizer: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(AnimationSpeedManager), nameof(MarkerPatch))));
            _markerPatchInstalled = true;
        }

        if (!HandlersByPriority.TryGetValue(priority, out List<Handler> handlers))
        {
            handlers = new List<Handler>();
            HandlersByPriority.Add(priority, handlers);
        }

        if (!_wrapperInstalled)
        {
            Harmony.Patch(TargetMethod, postfix: new HarmonyMethod(AccessTools.DeclaredMethod(typeof(AnimationSpeedManager), nameof(Wrapper))));
            _wrapperInstalled = true;
        }

        handlers.Add(handler);
        _handlers = HandlersByPriority.OrderBy(pair => pair.Key).Select(pair => pair.Value.ToArray()).ToArray();
    }

    private static void Wrapper(Character ___m_character, Animator ___m_animator)
    {
        double currentSpeedMarker = ___m_animator.speed * 1e7 % 100;
        if (currentSpeedMarker is > 10 and < 30 || ___m_animator.speed <= 0.001f)
        {
            return;
        }

        double speed = ___m_animator.speed;
        double newSpeed = _handlers[_handlerIndex++].Aggregate(speed, (current, handler) => handler(___m_character, current));
        if (Math.Abs(newSpeed - speed) > double.Epsilon)
        {
            ___m_animator.speed = (float)(newSpeed - newSpeed % 1e-5);
            _changed = true;
        }
    }

    private static void MarkerPatch(Animator ___m_animator)
    {
        if (_changed)
        {
            float speed = ___m_animator.speed;
            double currentSpeedMarker = speed * 1e7 % 100;
            if (currentSpeedMarker is < 10 or > 30)
            {
                ___m_animator.speed += 19e-7f;
            }

            _changed = false;
        }

        _handlerIndex = 0;
    }
}
