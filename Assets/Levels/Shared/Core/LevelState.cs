using System;

namespace XunJiYiDa.Levels.Shared
{
    public enum LevelState { Ready, Running, Paused, Succeeded, Failed }

    [Flags]
    public enum PauseReason { None = 0, Manual = 1, Modal = 2, Focus = 4 }
}
