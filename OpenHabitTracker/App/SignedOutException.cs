namespace OpenHabitTracker.App;

// The server rejected this device's session, which retrying cannot fix, unlike an unreachable server.
public class SignedOutException : Exception;
