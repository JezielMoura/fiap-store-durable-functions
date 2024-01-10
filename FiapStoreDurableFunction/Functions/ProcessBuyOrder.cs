using FiapStoreDurableFunction.Getways;
using FiapStoreDurableFunction.Helpers;
using FiapStoreDurableFunction.Models;
using FiapStoreDurableFunction.Repositories;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.DurableTask;
using Microsoft.DurableTask.Client;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FiapStoreDurableFunction.Functions;

public static class BuyOrderApproval
{
    [Function(nameof(BuyOrderApproval))]
    public static async Task<string> RunOrchestrator([OrchestrationTrigger] TaskOrchestrationContext context)
    {
        var logger = context.CreateReplaySafeLogger(nameof(BuyOrderApproval));

        logger.LogInformation("Order Received.");

        var orderRequest = context.GetInput<Order>();
        var paymentDone = await context.CallActivityAsync<bool>(nameof(ProcessPayment), orderRequest);

        if (!paymentDone)
            return "Erro ao realizar pagamento.";

        var managerApproval = await context.CallActivityAsync<Order>(nameof(ManagerApproval), orderRequest);

        if (!managerApproval.Aprovado)
            return "Pedido pendendente de aprova��o.";

        await context.CallActivityAsync(nameof(PublishOrderToServiceBus), managerApproval);

        return "Pedido efetuado com sucesso.";
    }

    [Function(nameof(ProcessPayment))]
    public static bool ProcessPayment([ActivityTrigger] Order order, FunctionContext executionContext)
    {
        bool success = PaymentGetway.Process(order);

        executionContext.GetLogger(nameof(ProcessPayment)).LogInformation($"Pagamento do Pedido {order.IdPedido} processado.");

        return success;
    }

    [Function(nameof(ManagerApproval))]
    public static Order ManagerApproval([ActivityTrigger] Order order, FunctionContext executionContext)
    {
        bool approved = OrderRepository.GetManagerApproval();

        order.Aprovado = approved;

        if (order.Aprovado)
            executionContext.GetLogger(nameof(ManagerApproval)).LogInformation($"Pedido {order.IdPedido} aprovado.");

        return order;
    }

    [Function(nameof(PublishOrderToServiceBus))]
    public static void PublishOrderToServiceBus([ActivityTrigger] Order order, FunctionContext executionContext)
    {
        ServiceBusHelper.PublishOrder(order);

        executionContext.GetLogger(nameof(PublishOrderToServiceBus)).LogInformation($"Pedido {order.IdPedido} enviado para fila.");

        OrderRepository.UpdateServiceBusPub();
    }

    [Function(nameof(ProcessOrderHttpStart))]
    public static async Task<HttpResponseData> ProcessOrderHttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req, [DurableClient] DurableTaskClient client, FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger(nameof(ProcessOrderHttpStart));
        var order = await req.ReadFromJsonAsync<Order>();
        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(BuyOrderApproval), order);

        logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return client.CreateCheckStatusResponse(req, instanceId);
    }
}
