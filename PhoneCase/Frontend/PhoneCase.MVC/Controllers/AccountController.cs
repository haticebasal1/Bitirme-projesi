using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using PhoneCase.Shared.Dtos.OrderDtos;
using PhoneCase.Shared.Dtos.ResponseDtos;
using PhoneCase.Shared.Enums;

namespace PhoneCase.MVC.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AccountController(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }
    public async Task<IActionResult> Index([FromQuery] OrderStatus? orderstatus, [FromQuery] DateTime startDate, [FromQuery] DateTime? endDate, [FromQuery] bool? isDeleted)
    {
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:5289/orders/myorders?OrderStatus={orderstatus}&StartDate={startDate}&EndDate={endDate}&IsDeleted={isDeleted}");
            try
            {
                var authResult = await _httpContextAccessor.HttpContext!.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                var token = authResult.Properties?.Items["access_token"];
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await client.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ResponseDto<List<OrderDto>>>(responseContent);
                if (response is not null)
                {
                    response.EnsureSuccessStatusCode();
                }
                var model = orderstatus is not null ? result!.Data : result!.Data.Where(x => x.OrderStatus != OrderStatus.Delivered).ToList();
                return View(model);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Hata: {ex.Message}");
                throw;
            }
        }
    }
}