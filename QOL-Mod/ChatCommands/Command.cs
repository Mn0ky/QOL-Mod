using System;
using System.Collections.Generic;
using UnityEngine;

namespace QOL;

public class Command
{
    public bool IsPublic
    {
        get => _isPublic;
        set
        {
            if (_alwaysPublic || _alwaysPrivate)
            {
                Debug.LogWarning("Cannot modify cmd visibility once it has been set always public/private!");
                return;
            }

            _isPublic = value;
        }
    }
        
    public string Name { get; }
    public List<string> Aliases { get; } = new();
    public bool IsToggle { get; private set; }
    public bool IsEnabled { get; set; }
    public static readonly char CmdPrefix = Plugin.ConfigCmdPrefix.Value.Length == 1
        ? Plugin.ConfigCmdPrefix.Value[0]
        : '/'; // If more than 1 char was entered, use the default cmd prefix

    private readonly Action<string[], Command> _runCmdAction; // Use Action as method will never return anything
    private readonly int _minExpectedArgs; // Minimal # of args required for cmd to function
    private bool _isPublic;
    private bool _alwaysPublic;
    private bool _alwaysPrivate;

    private string _currentOutputMsg;
    private LogType _currentLogType; // Any mod msg will be of type "success" by default

    // TODO: Implement auto-suggested parameters property
    // public String[] AutoParams = new String[] {"placeholder"};

    public Command(string name, Action<string[], Command> cmdMethod, int minNumExpectedArgs, bool defaultPrivate)
    {
        Name = CmdPrefix + name;
        _runCmdAction = cmdMethod;
        _minExpectedArgs = minNumExpectedArgs;
        IsPublic = !defaultPrivate;
    }
        
    // Private as there has been no cases where this type of visibility was necessary and the cmd was not a toggle
    private void SetAlwaysPrivate() 
    {
        if (_alwaysPublic)
        {
            Debug.LogWarning("Cmd is already always public, cannot modify this!");
            return;
        }

        _alwaysPrivate = true;
        IsPublic = false;
    }
        
    public Command SetAlwaysPublic()
    {
        if (_alwaysPrivate)
        {
            Debug.LogWarning("Cmd is already always private, cannot modify this!");
            return this;
        }

        _alwaysPublic = true;
        IsPublic = true;
        return this;
    }

    public Command MarkAsToggle()
    {
        IsToggle = true;
        return this;
    }

    public void SetOutputMsg(string msg) => _currentOutputMsg = msg;
    public void SetLogType(LogType type) => _currentLogType = type;
    public void Toggle() => IsEnabled = !IsEnabled;

    public void Execute(string[] args = null)
    {
        // Minus 1 from args count so the inputted cmd isn't counted as an arg
        if ((args != null && args.Length - 1 < _minExpectedArgs) || (args == null && _minExpectedArgs > 0))
        {
            _currentLogType = LogType.Warning;
            _currentOutputMsg = "Invalid # of arguments specified. See /help for more info.";
            Helper.SendModOutput(_currentOutputMsg, _currentLogType, false);
                
            _currentLogType = LogType.Success;
            return;
        }
            
        _runCmdAction(args, this);
            
        if (string.IsNullOrEmpty(_currentOutputMsg)) // Some cmds may not have any output at all
            return;
            
        if (_currentLogType == LogType.Warning) // All warning msg's should be client-side
        {
            Helper.SendModOutput(_currentOutputMsg, LogType.Warning, false);
            _currentLogType = LogType.Success;
            return;
        }

        Helper.SendModOutput(_currentOutputMsg, LogType.Success, !IsToggle && IsPublic, !IsToggle || IsEnabled);
        _currentLogType = LogType.Success;
    }

    public enum LogType
    {
        Success,
        Warning
    }
}