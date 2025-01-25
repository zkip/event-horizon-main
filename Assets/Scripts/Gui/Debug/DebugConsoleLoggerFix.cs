﻿#nullable enable

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Zenject;
using GameDatabase;

namespace Gui.DebugConsole
{
    public class DebugConsoleLoggerFix : IInitializable, IDisposable, ITickable, IDebugConsoleLogger, GameDiagnostics.ILogger
    {
        private IDatabase? _database;
        private readonly List<LogEntry> _messages = new();
        private bool _enabled;
        private bool _gotErrorsSinceLastCheck;

        public event Action? MessageReceived;


        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;

                if (_enabled)
                    GameDiagnostics.Trace.Logger = this;
                else if (GameDiagnostics.Trace.Logger == this)
                    GameDiagnostics.Trace.Logger = null;
            }
        }

        public DebugConsoleLoggerFix(IDatabase database)
        {
            _database = database;
        }

        public void Initialize()
        {
            Application.logMessageReceivedThreaded += OnLogMessageReceived;

            if (_database == null) return;
            _database.DatabaseLoading += OnDatabaseLoading;
            _database.DatabaseLoaded += OnDatabaseLoaded;
            OnDatabaseLoaded();
        }

        public void Dispose()
        {
            Application.logMessageReceivedThreaded -= OnLogMessageReceived;

            if (_database == null) return;
            _database.DatabaseLoading -= OnDatabaseLoading;
            _database.DatabaseLoaded -= OnDatabaseLoaded;
        }

        public void Tick()
        {
            if (!_enabled) return;
            if (_gotErrorsSinceLastCheck)
            {
                _gotErrorsSinceLastCheck = false;
                MessageReceived?.Invoke();
            }
        }

        public void GetMessages(List<LogEntry> list, int maxAmount)
        {
            list.Clear();
            lock (_messages)
            {
                if (_messages.Count <= maxAmount)
                    list.AddRange(_messages);
                else
                    list.AddRange(_messages.Skip(_messages.Count - maxAmount).Take(maxAmount));

                _messages.Clear();
            }
        }

        public void Log(string message, GameObject context = null)
        {
            WriteLogMessage(new LogEntry(message, null, LogType.Log));
            GameDiagnostics.UnityLogger.Instance.Log(message, context);
        }

        public void LogWarning(string message, GameObject context = null)
        {
            WriteLogMessage(new LogEntry(message, null, LogType.Warning));
            GameDiagnostics.UnityLogger.Instance.LogWarning(message, context);
        }

        public void LogError(string message, GameObject context = null)
        {
            WriteLogMessage(new LogEntry(message, null, LogType.Error));
            GameDiagnostics.UnityLogger.Instance.LogError(message, context);
        }

        public void LogException(Exception e, GameObject context = null)
        {
            WriteLogMessage(new LogEntry(e.Message, e.StackTrace, LogType.Exception));
            GameDiagnostics.UnityLogger.Instance.LogException(e, context);
        }

        private void WriteLogMessage(LogEntry logEntry)
        {
            lock (_messages)
            {
                _messages.Add(logEntry);

                if (logEntry.IsError)
                    _gotErrorsSinceLastCheck = true;
            }
        }

        private void OnDatabaseLoaded()
        {
            _database = GameDatabase.Database.getCurrent();
            _enabled = IsEnabled();
        }

        private void OnDatabaseLoading(GameDatabase.Storage.IDataStorage dataStorage)
        {
            _enabled = dataStorage != null && dataStorage.IsEditable;
        }

        private void OnLogMessageReceived(string logString, string stackTrace, LogType type)
        {
            if (!_enabled) return;
            if (type != LogType.Exception) return;
            WriteLogMessage(new LogEntry(logString, stackTrace, type));
        }

        private bool IsEnabled()
        {
            if (_database == null) return false;
            if (_database.IsEditable) return true;
            if (_database.DebugSettings != null && _database.DebugSettings.EnableDebugConsole) return true;
            return false;
        }
    }
}
