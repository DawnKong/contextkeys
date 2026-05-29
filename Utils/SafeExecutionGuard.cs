namespace ContextKeys.Utils;

public static class SafeExecutionGuard
{
    private static int _executionDepth;
    private const int MaxExecutionDepth = 1;

    private static readonly object Lock = new();

    /// <summary>
    /// Whether we are currently executing an action (prevents re-entry).
    /// </summary>
    public static bool IsExecuting
    {
        get
        {
            lock (Lock)
                return _executionDepth > 0;
        }
    }

    /// <summary>
    /// Try to enter the execution guard. Returns false if already executing.
    /// </summary>
    public static bool TryEnter()
    {
        lock (Lock)
        {
            if (_executionDepth >= MaxExecutionDepth)
                return false;
            _executionDepth++;
            return true;
        }
    }

    /// <summary>
    /// Exit the execution guard. Must be called after TryEnter succeeds.
    /// </summary>
    public static void Exit()
    {
        lock (Lock)
        {
            if (_executionDepth > 0)
                _executionDepth--;
        }
    }
}
