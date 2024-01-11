using FiapStoreDurableFunction.Models;

namespace FiapStoreDurableFunction.Getways;

public static class PaymentGateway
{
    public static bool Process(Order order) => true;
}