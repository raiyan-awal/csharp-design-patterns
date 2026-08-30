namespace SagaPattern.Services;

public sealed class FlightUnavailableException(string message) : Exception(message);
public sealed class HotelUnavailableException(string message)  : Exception(message);
public sealed class CarUnavailableException(string message)    : Exception(message);
public sealed class PaymentDeclinedException(string message)   : Exception(message);
