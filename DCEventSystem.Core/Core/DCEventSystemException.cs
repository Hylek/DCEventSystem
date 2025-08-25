namespace DCEventSystem.Core;

public class DCEventSystemException : Exception
{
    public DCEventSystemException(string message) : base(message) { }
    public DCEventSystemException(string message, Exception innerException) : base(message, innerException) { }
}
    
public class DCEventSystemNotInitialisedException()
    : DCEventSystemException("EventSystem not initialized. Call EventSystem.Initialize() first.");
    
public class DCEventSystemAlreadyInitialisedException() : DCEventSystemException("EventSystem already initialized.");