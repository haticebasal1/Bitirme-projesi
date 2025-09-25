using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PhoneCase.Shared.Dtos.OrderDtos;
using PhoneCase.Shared.Dtos.ResponseDtos;

namespace PhoneCase.MVC.Controllers;

[Authorize]

public class OrderController : Controller
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OrderController(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IActionResult> Index()
    {
        //O sırada login olmuş kullanıcının geçmiş siparişleri için kullanılabilir.
        return View();
    }
    public async Task<IActionResult> CheckOut()
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get,
        "http://localhost:5289/carts");
        try
        {
            var authResult = await _httpContextAccessor.HttpContext!.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var token = authResult.Properties?.Items["access_token"];
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResponseDto<CartDto>>(responseContent);
            if (!result!.IsSuccessful)
            {
                Console.WriteLine($"Hata: {result!.Errors[0]}");
                return Redirect("/");
            }
            var orderNowDto = new OrderNowDto
            {
                Address = "Test Adresi",
                City = "Test Şehir",
                OrderItems = result!.Data.CartItems.Select(x => new OrderItemCreateDto
                {
                    ProductId = x.ProductId,
                    Quantity = x.Quantity,
                    UnitPrice = x.Quantity,
                    Product = x.Product
                }).ToList()
            };


            return View(orderNowDto);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hata: {ex.Message}");
            throw;
        }
    }
    [HttpPost]
    public async Task<IActionResult> Checkout(OrderNowDto orderNowDto)
    {
        var client = new HttpClient();

        try
        {
            // Kullanıcı oturumundan token al
            var authResult = await _httpContextAccessor.HttpContext!
                .AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            var token = authResult.Properties?.Items["access_token"];
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("Hata: Access token bulunamadı");
                return Redirect("/Account/Login");
            }

            // Token'ı request header'a ekle
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            // Sepet bilgilerini API'den al
            var cartResponse = await client.GetAsync("http://localhost:5289/carts");
            var cartResponseContent = await cartResponse.Content.ReadAsStringAsync();
            var cartResult = JsonConvert.DeserializeObject<ResponseDto<CartDto>>(cartResponseContent);

            if (cartResult == null || !cartResult.IsSuccessful)
            {
                Console.WriteLine($"Hata: {cartResult?.Errors?.FirstOrDefault() ?? "Sepet alınamadı"}");
                return Redirect("/");
            }

            var cart = cartResult.Data;

            // Sepet içeriğini logla
            foreach (var item in cart.CartItems)
            {
                Console.WriteLine(
                    $"CartItem => CartId: {item.CartId}, ProductId: {item.ProductId}, " +
                    $"Product?.Id: {item.Product?.Id}, Product Null?: {item.Product == null}"
                );
            }

            // Geçerli ürünleri filtrele
            var validCartItems = cart.CartItems
                .Where(x => x.ProductId > 0 && x.Product != null)
                .ToList();

            if (!validCartItems.Any())
            {
                Console.WriteLine("Sepette geçerli ürün yok!");
                return Redirect("Confirmation");
            }

            // Sipariş için gerekli alanları set et
            orderNowDto.OrderItems = validCartItems
                .Select(x => new OrderItemCreateDto
                {
                    ProductId = x.ProductId,
                    Quantity = x.Quantity,
                    UnitPrice = x.Product!.Price
                }).ToList();

            // Siparişi API'ye gönder
            var jsonContent = JsonConvert.SerializeObject(orderNowDto);
            var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");
            var orderResponse = await client.PostAsync("http://localhost:5289/orders", stringContent);
            var orderResponseContent = await orderResponse.Content.ReadAsStringAsync();

            Console.WriteLine(">>> API StatusCode: " + orderResponse.StatusCode);
            Console.WriteLine(">>> API Response: " + orderResponseContent);

            if (!orderResponse.IsSuccessStatusCode)
            {
                Console.WriteLine("Sipariş API'ye gönderilemedi!");
                return Redirect("/");
            }

            var resultOrderNow = JsonConvert.DeserializeObject<ResponseDto<OrderDto>>(orderResponseContent);

            if (resultOrderNow == null || !resultOrderNow.IsSuccessful)
            {
                Console.WriteLine($"Hata: {resultOrderNow?.Errors?.FirstOrDefault() ?? "Sipariş oluşturulamadı"}");
                return Redirect("/");
            }

            // Sipariş başarılı → onay sayfası
            return View("Confirmation");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hata: {ex.Message}");
            throw;
        }
    }
}