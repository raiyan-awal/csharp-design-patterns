namespace RetryPattern.Services;

public sealed class SimulatedPaymentGateway : IPaymentGateway
{
    private int  _failuresRemaining = 0;
    private bool _declined          = false;

    public int  CallCount { get; private set; }
    public bool IsHealthy => _failuresRemaining == 0 && !_declined;

    public void FailTimes(int count) { _failuresRemaining = count; _declined = false; }
    public void Decline()            { _declined = true; _failuresRemaining = 0; }
    public void SetHealthy()         { _failuresRemaining = 0; _declined = false; }

    public PaymentResult ProcessPayment(string cardToken, decimal amountCAD, string orderId)
    {
        CallCount++;

        if (_declined)
            throw new PaymentDeclinedException("Card declined: insufficient funds.");

        if (_failuresRemaining > 0)
        {
            _failuresRemaining--;
            throw new HttpRequestException("Payment gateway timeout (503 Service Unavailable).");
        }

        var txnId = $"TXN-{Guid.NewGuid():N}"[..20].ToUpper();
        return new PaymentResult(txnId, amountCAD, "Approved");
    }
}
