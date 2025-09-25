using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NToastNotify;
using PhoneCase.Shared.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using PhoneCase.Shared.Dtos.ResponseDtos;
using PhoneCase.Shared.Dtos.OrderDtos;

namespace PhoneCase.MVC.Areas.Admin.Controllers;


[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OrderController : Controller
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IToastNotification _toastr;

    public OrderController(IHttpContextAccessor httpContextAccessor, IToastNotification toastr)
    {
        _httpContextAccessor = httpContextAccessor;
        _toastr = toastr;
    }

    public async Task<IActionResult> Index([FromQuery] OrderStatus? orderStatus, [FromQuery] string? applicationUserId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate, [FromQuery] bool? isDeleted)
    {
        ViewBag.OrderStatusList = GetOrderStatuses(orderStatus);
        ViewBag.GetOrderStatusesSelectList = new Func<OrderStatus?, List<SelectListItem>>(GetOrderStatuses);


        var client = new HttpClient();
        var url = $"http://localhost:5289/orders/all?OrderStatus={orderStatus}&UserId={applicationUserId}&StartDate={startDate}&EndDate={endDate}&IsDeleted={isDeleted}";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        try
        {
            var authResult = await _httpContextAccessor.HttpContext!.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var token = authResult.Properties?.Items["access_token"];
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResponseDto<List<OrderDto>>>(responseContent);

            if (!result!.IsSuccessful)
            {
                var errorMessage = result.Errors.FirstOrDefault() ?? "Bir hata oluştu.";
                _toastr.AddErrorToastMessage(errorMessage);
            }

            return View(result.Data);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hata: {ex.Message}");
            throw;
        }
    }


    [HttpPut]
    public async Task<IActionResult> ChangeOrderStatus(int orderId, OrderStatus orderStatus)
    {
        var client = new HttpClient();
        try
        {
            var authResult = await _httpContextAccessor.HttpContext!.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var token = authResult.Properties?.Items["access_token"];
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsync($"http://localhost:5289/orders/{orderId}?orderStatus={orderStatus}", null);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResponseDto<NoContentDto>>(responseContent);
            return Json(result);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hata: {ex.Message}");
            throw;
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, $"http://localhost:5289/orders?orderId={id}");
        try
        {
            var authResult = await _httpContextAccessor.HttpContext!.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var token = authResult.Properties?.Items["access_token"];
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResponseDto<OrderDto>>(responseContent);
            if (!result!.IsSuccessful)
            {
                _toastr.AddErrorToastMessage(result!.Errors[0]);
                return RedirectToAction(nameof(Index));
            }
            return View(result!.Data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hata:{ex.Message}");
            throw;
        }
    }


    public async Task<IActionResult> Cancel(int id)
    {
        var client = new HttpClient();
        try
        {
            var authResult = await _httpContextAccessor.HttpContext!.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var token = authResult.Properties?.Items["access_token"];
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.PutAsync($"http://localhost:5289/orders/cancel?orderId={id}", null);
            var responseContent = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<ResponseDto<NoContentDto>>(responseContent);
            if (!result!.IsSuccessful)
            {
                _toastr.AddErrorToastMessage(result!.Errors[0]);
            }
            _toastr.AddSuccessToastMessage("İşlem başarılı!");
            return RedirectToAction(nameof(Index), new { IsDeleted = false });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hata: {ex.Message}");
            throw;
        }
    }
    private List<SelectListItem> GetOrderStatuses(OrderStatus? selectedStatus)
    {
        return Enum
            .GetValues(typeof(OrderStatus))
            .Cast<OrderStatus>()
            .Select(x => new SelectListItem
            {
                Text = x
                    .GetType()
                    .GetField(x.ToString())?
                    .GetCustomAttributes(typeof(DisplayAttribute), false)
                    .Cast<DisplayAttribute>()
                    .FirstOrDefault()?
                    .Name ?? x.ToString(),
                Value = ((int)x).ToString(),
                Selected = x == selectedStatus
            }).ToList();
    }
}