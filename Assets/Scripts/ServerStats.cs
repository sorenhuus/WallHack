using System;
using System.Diagnostics;
using Mirror;
using UnityEngine;
using Profiler = UnityEngine.Profiling.Profiler;

/// <summary>
/// Displays server CPU usage, tick time, memory, and player count below Mirror's NetworkManagerHUD.
/// Attach to the NetworkManager GameObject.
/// </summary>
public class ServerStats : MonoBehaviour
{
    private float _lastFixedTime;
    private float _maxTickMs;
    private float _tickAccumMs;
    private int _tickCount;

    private Process _process;
    private TimeSpan _lastCpuTime;
    private float _lastCpuSampleTime;

    private string _statsDisplay = "";
    private float _timeSinceUpdate;
    private const float UpdateInterval = 1f;

    private void Start()
    {
        _process = Process.GetCurrentProcess();
        _process.Refresh();
        _lastCpuTime = _process.TotalProcessorTime;
        _lastCpuSampleTime = Time.realtimeSinceStartup;
    }

    private void FixedUpdate()
    {
        if (!NetworkServer.active) return;

        float now = Time.realtimeSinceStartup;
        float tickMs = (now - _lastFixedTime) * 1000f;
        _lastFixedTime = now;

        _tickAccumMs += tickMs;
        _tickCount++;
        if (tickMs > _maxTickMs) _maxTickMs = tickMs;
    }

    private void Update()
    {
        if (!NetworkServer.active) return;

        _timeSinceUpdate += Time.deltaTime;
        if (_timeSinceUpdate < UpdateInterval) return;
        _timeSinceUpdate = 0f;

        float avgTickMs = _tickCount > 0 ? _tickAccumMs / _tickCount : 0f;

        // CPU usage
        _process.Refresh();
        TimeSpan cpuDelta = _process.TotalProcessorTime - _lastCpuTime;
        float wallDelta = Time.realtimeSinceStartup - _lastCpuSampleTime;
        float cpuPercent = (float)(cpuDelta.TotalSeconds / (wallDelta * SystemInfo.processorCount)) * 100f;
        _lastCpuTime = _process.TotalProcessorTime;
        _lastCpuSampleTime = Time.realtimeSinceStartup;

        long allocatedMB = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
        long reservedMB  = Profiler.GetTotalReservedMemoryLong()  / (1024 * 1024);
        long processMB   = _process.WorkingSet64 / (1024 * 1024);
        int  players     = NetworkServer.connections.Count;

        _statsDisplay =
            $"Players:     {players}\n" +
            $"CPU:         {cpuPercent:F1}%\n" +
            $"Tick avg:    {avgTickMs:F2} ms\n" +
            $"Tick max:    {_maxTickMs:F2} ms\n" +
            $"Unity mem:   {allocatedMB} / {reservedMB} MB\n" +
            $"Process RAM: {processMB} MB";

        _tickAccumMs = 0f;
        _tickCount   = 0;
        _maxTickMs   = 0f;
    }

    private void OnGUI()
    {
        if (!NetworkServer.active || string.IsNullOrEmpty(_statsDisplay)) return;
        GUIStyle style = new GUIStyle(GUI.skin.label) { fontSize = 24 };
        GUI.Label(new Rect(10, 145, 600, 240), _statsDisplay, style);
    }
}
