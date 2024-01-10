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
    public static async Task<string> RunOrchestrator(
        [OrchestrationTrigger] TaskOrchestrationContext context)
    {
        ILogger logger = context.CreateReplaySafeLogger(nameof(BuyOrderApproval));
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

        executionContext.GetLogger(nameof(ProcessPayment)).LogInformation($"Pedido {order.IdPedido} processado.");

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

    [Function("ProcessOrderStart")]
    public static async Task<HttpResponseData> HttpStart(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req, [DurableClient] DurableTaskClient client, FunctionContext executionContext)
    {
        var logger = executionContext.GetLogger("Function1_HttpStart");
        var order = JsonSerializer.Deserialize<Order>(await req.ReadAsStringAsync() ?? throw new ArgumentNullException(nameof(req)));
        var instanceId = await client.ScheduleNewOrchestrationInstanceAsync(nameof(BuyOrderApproval), order);

        logger.LogInformation("Started orchestration with ID = '{instanceId}'.", instanceId);

        return client.CreateCheckStatusResponse(req, instanceId);
    }
}
/*
//Pedido
{
"IdPedido": 1,
"IdCliente": 101,
"DataPedido": "2023-12-21T12:34:56", // Use the current date and time in ISO 8601 format
"ValorTotal": 150.50,
"Pago": false,
"Aprovado": false,
"Items": [
{
 "IdItem": 1,
 "IdPedido": 1,
 "Produto": {
   "IdProduto": 1001,
   "IdTipoProduto": 201,
   "Nome": "Product A",
   "Preco": 30.25,
   "Descricao": "Description of Product A",
   "Quantidade": 2
 },
 "Preco": 30.25,
 "Quantidade": 2,
 "SubTotal": 60.50
}
// Add more items as needed
]
}

//OrderRequest
{
"IdCliente": 123,
"CreatedDate" : "2023-12-21T12:34:56",
"IdProdutoXQuantidade": [
{
 "IdProduto": 1,
 "Quantidade": 10
},
{
 "IdProduto": 2,
 "Quantidade": 5
},
{
 "IdProduto": 3,
 "Quantidade": 8
}
// Add more items as needed
]
}
*/