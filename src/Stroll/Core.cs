using System;
using System.Collections.Generic;

namespace Stroll
{
    public enum Phase { Working, Snoozed, Prompt, Breaking, Paused }

    // Clock supplied by the host: monotonic elapsed time, never wall-clock time.
    public sealed class BreakClock
    {
        public Phase Phase { get; private set; }
        public double Remaining { get; private set; }
        public bool WarningSent { get; private set; }
        public event Action Changed;
        public event Action Warning;
        private double last;
        private Phase pausedPhase;
        private readonly Func<double> now;
        private readonly Func<Settings> settings;

        public BreakClock(Func<double> now, Func<Settings> settings)
        { this.now = now; this.settings = settings; FreshInterval(); }

        public void Tick()
        {
            double current = now();
            double elapsed = Math.Max(0, current - last); last = Math.Max(last, current);
            if (Phase == Phase.Paused || Phase == Phase.Prompt) return;
            Remaining = Math.Max(0, Remaining - elapsed);
            Settings s = settings();
            if (Remaining <= 0)
            {
                if (Phase == Phase.Breaking) { FreshInterval(); return; }
                Phase = Phase.Prompt;
                Notify(); return;
            }
            if ((Phase == Phase.Working || Phase == Phase.Snoozed) &&
                s.WarningEnabled && !WarningSent && Remaining <= s.WarningSeconds)
            { WarningSent = true; if (Warning != null) Warning(); }
        }
        private void Set(Phase phase, double seconds)
        { Phase = phase; Remaining = seconds; last = now(); WarningSent = false; Notify(); }
        private void Notify() { if (Changed != null) Changed(); }
        public void FreshInterval() { Set(Phase.Working, settings().WorkMinutes * 60); }
        public void Snooze() { Set(Phase.Snoozed, settings().SnoozeMinutes * 60); }
        public void Skip() { FreshInterval(); }
        public void StartBreak() { Set(Phase.Breaking, settings().BreakMinutes * 60); }
        // Keep the full break duration visible after the overlay has faded in.
        public void HoldBreakFor(double seconds)
        { if (Phase == Phase.Breaking) last = now() + Math.Max(0, seconds); }
        public void Pause()
        {
            if (Phase == Phase.Paused) return;
            Tick(); pausedPhase = Phase; Phase = Phase.Paused; Notify();
        }
        public void Resume()
        {
            if (Phase != Phase.Paused) return;
            last = now(); Phase = pausedPhase; Notify();
        }
    }

    public struct PetPose
    {
        public double X, Y, Angle;
        public bool Reverse, Jumping;
        public PetPose(double x, double y, double a, bool reverse, bool jump)
        { X = x; Y = y; Angle = a; Reverse = reverse; Jumping = jump; }
    }

    public static class PetRoute
    {
        // Edge order is clockwise: bottom, right, top, left.
        public static PetPose At(double time, double width, double height, double size,
            double speed, int edges)
        {
            double margin = size / 2 + 12;
            double x0 = margin, y0 = margin;
            double x1 = Math.Max(x0 + 1, width - margin);
            double y1 = Math.Max(y0 + 1, height - margin);
            double[] xs = { x0, x1, x1, x0 };
            double[] ys = { y1, y1, y0, y0 };
            double[] lengths = { x1 - x0, y1 - y0, x1 - x0, y1 - y0 };
            double[] angles = { 0, -90, -180, -270 };
            edges &= 15; if (edges == 0) edges = 1;
            var order = new List<int>();
            int start = 0;
            if (edges != 15)
                for (int e = 0; e < 4; e++)
                    if ((edges & (1 << e)) != 0 && (edges & (1 << ((e + 3) % 4))) == 0)
                    { start = e; break; }
            for (int k = 0; k < 4; k++)
            { int e = (start + k) % 4; if ((edges & (1 << e)) != 0) order.Add(e); }
            bool opposite = edges == 5 || edges == 10;
            double total = 0; foreach (int e in order) total += lengths[e];
            double distance = Math.Max(0, time) * Math.Max(1, speed);
            bool reverse = false;
            if (opposite)
            {
                double hop = Math.Max(1.2, Math.Min(3, Math.Max(width, height) / 650));
                double cycle = total / speed + hop * 2;
                double local = time % cycle;
                for (int k = 0; k < order.Count; k++)
                {
                    int e = order[k], next = order[(k + 1) % order.Count];
                    double walk = lengths[e] / speed;
                    if (local < walk) return OnEdge(e, local / walk, false, xs, ys, angles);
                    local -= walk;
                    if (local < hop)
                    {
                        double t = local / hop, smooth = t * t * (3 - 2 * t);
                        int end = (e + 1) % 4;
                        double x = xs[end] + (xs[next] - xs[end]) * smooth;
                        double y = ys[end] + (ys[next] - ys[end]) * smooth;
                        // Arc bows toward the centre, inside the screen.
                        double arc = Math.Sin(t * Math.PI) * Math.Min(width, height) * .16;
                        if (e == 0 || e == 2) x += (width / 2 - x >= 0 ? 1 : -1) * arc;
                        else y += (height / 2 - y >= 0 ? 1 : -1) * arc;
                        double delta = angles[next] - angles[e];
                        if (delta > 180) delta -= 360; if (delta < -180) delta += 360;
                        return new PetPose(x, y, angles[e] + delta * smooth, false, true);
                    }
                    local -= hop;
                }
            }
            else if (edges == 15) distance %= total;
            else
            { distance %= total * 2; if (distance > total) { distance = total * 2 - distance; reverse = true; } }
            foreach (int e in order)
            {
                if (distance <= lengths[e]) return OnEdge(e, distance / lengths[e], reverse, xs, ys, angles);
                distance -= lengths[e];
            }
            return OnEdge(order[0], 0, false, xs, ys, angles);
        }
        private static PetPose OnEdge(int e, double t, bool reverse, double[] xs, double[] ys, double[] angles)
        {
            int end = (e + 1) % 4;
            return new PetPose(xs[e] + (xs[end] - xs[e]) * t,
                ys[e] + (ys[end] - ys[e]) * t, angles[e], reverse, false);
        }
    }
}
