using System;

namespace DCEventSystem.Core;

/// <summary>
/// Custom exception wrapper for DCEventSystem
/// </summary>
public class DCEventSystemException : Exception
{
    /// <inheritdoc />
    public DCEventSystemException(string message) : base(message) { }

    /// <inheritdoc />
    public DCEventSystemException(string message, Exception innerException) : base(message, innerException) { }
}

/// <inheritdoc />
public class DCEventSystemNotInitialisedException()
    : DCEventSystemException("EventSystem not initialized. Call EventSystem.Initialize() first.");

/// <inheritdoc />
public class DCEventSystemAlreadyInitialisedException() : DCEventSystemException("EventSystem already initialized.");