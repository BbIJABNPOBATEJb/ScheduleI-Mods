using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace S1Shared;

/// <summary>
/// Minimal coroutine runner driven from MelonMod.OnUpdate. Works the same on Mono and IL2CPP
/// (no Unity coroutine interop): an iterator may yield null (next frame), a float (seconds,
/// unscaled) or another IEnumerator (nested, runs to completion first).
/// </summary>
internal sealed class CoRunner
{
    private sealed class Routine
    {
        public readonly Stack<IEnumerator> Stack = new();
        public float WaitUntil;
    }

    private readonly List<Routine> _routines = new();
    private readonly Action<Exception> _onError;

    public CoRunner(Action<Exception> onError) => _onError = onError;

    public void Start(IEnumerator routine)
    {
        var r = new Routine();
        r.Stack.Push(routine);
        _routines.Add(r);
    }

    public void StopAll() => _routines.Clear();

    public void Tick()
    {
        var now = Time.realtimeSinceStartup;
        for (var i = _routines.Count - 1; i >= 0; i--)
        {
            var r = _routines[i];
            if (now < r.WaitUntil)
                continue;
            try
            {
                if (!Step(r, now))
                    _routines.RemoveAt(i);
            }
            catch (Exception ex)
            {
                _routines.RemoveAt(i);
                _onError(ex);
            }
        }
    }

    private static bool Step(Routine r, float now)
    {
        while (r.Stack.Count > 0)
        {
            var top = r.Stack.Peek();
            if (!top.MoveNext())
            {
                r.Stack.Pop();
                continue;
            }

            switch (top.Current)
            {
                case IEnumerator nested:
                    r.Stack.Push(nested);
                    continue;
                // Waits count from now, not from the start of the tick: the step itself may have taken long.
                case float seconds:
                    r.WaitUntil = Time.realtimeSinceStartup + seconds;
                    return true;
                case int seconds:
                    r.WaitUntil = Time.realtimeSinceStartup + seconds;
                    return true;
                default:
                    return true;
            }
        }
        return false;
    }
}
