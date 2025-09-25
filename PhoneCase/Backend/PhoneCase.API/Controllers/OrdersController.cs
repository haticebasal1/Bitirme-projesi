using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PhoneCase.API.Controllers.BaseController;
using PhoneCase.Business.Abstract;
using PhoneCase.Shared.Dtos.OrderDtos;
using PhoneCase.Shared.Enums;

namespace PhoneCase.API.Controllers
{
    [Route("orders")]
    [ApiController]
    public class OrdersController : CustomControllerBase
    {
        private readonly IOrderService _orderManager;

        public OrdersController(IOrderService orderManager)
        {
            _orderManager = orderManager;
        }
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> OrderNow(OrderNowDto orderNowDto)
        {
            orderNowDto.UserId = UserId;

            // Sipariş öncesi kontrol ve log
            if (orderNowDto.OrderItems == null || !orderNowDto.OrderItems.Any())
            {
                return BadRequest("Siparişe ürün eklenmemiş!");
            }

            foreach (var item in orderNowDto.OrderItems)
            {
                Console.WriteLine($"ProductId: {item.ProductId}, Quantity: {item.Quantity}, UnitPrice: {item.UnitPrice}");
                if (item.ProductId == 0)
                {
                    return BadRequest("Geçersiz ürün ID’si (0) bulundu!");
                }
            }

            var response = await _orderManager.OrderNowAsync(orderNowDto);
            return CreateResult(response);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetOrder([FromQuery] int orderId)
        {
            var response = await _orderManager.GetOrderAsync(orderId);
            return CreateResult(response);
        }
        [HttpGet("myorder")]
        [Authorize]
        public async Task<IActionResult> GetMyOrder([FromQuery] int orderId)
        {
            var response = await _orderManager.GetMyOrderAsync(orderId, UserId);
            return CreateResult(response);
        }
        [HttpPut("{orderId}")]
        [Authorize(Roles = "Admin")]
        // /orders/5?orderStatus=3
        public async Task<IActionResult> ChangeOrderStatus(int orderId, [FromQuery] OrderStatus orderStatus)
        {
            var response = await _orderManager.ChangeOrderStatusAsync(new ChangeOrderStatusDto { OrderId = orderId, OrderStatus = orderStatus });
            return CreateResult(response);
        }

        [HttpPut("cancel")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CancelOrder([FromQuery] int orderId)
        {
            var response = await _orderManager.CancelOrderAsync(orderId);
            return CreateResult(response);
        }
        [HttpGet("all")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllOrders([FromQuery] OrderFiltersDto? orderFiltersDto)
        {
            var response = await _orderManager.GetAllAsync(orderFiltersDto!);
            return CreateResult(response);
        }
        [HttpGet("myorders")]
        [Authorize]
        public async Task<IActionResult> GetAllMyOrders([FromQuery] OrderFiltersDto? orderFiltersDto)
        {
            orderFiltersDto!.UserId = UserId;
            var response = await _orderManager.GetAllAsync(orderFiltersDto!);
            return CreateResult(response);
        }
        [HttpGet("total")]
        [Authorize(Roles = "Admin")]
        public IActionResult GetOrdersTotal([FromQuery] OrderFiltersDto orderFiltersDto)
        {
            var response = _orderManager.GetOrdersTotal(orderFiltersDto);
            return CreateResult(response);
        }
    }
}
