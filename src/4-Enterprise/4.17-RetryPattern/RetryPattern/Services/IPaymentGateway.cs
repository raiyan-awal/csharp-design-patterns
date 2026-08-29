namespace RetryPattern.Services;

public interface IPaymentGateway
{
    PaymentResult ProcessPayment(string cardToken, decimal amountCAD, string orderId);
}
