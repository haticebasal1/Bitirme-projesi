using System.Net.Http.Headers;
using PhoneCase.MVC.Models;
using PhoneCase.Shared.Dtos.ResponseDtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace PhoneCase.MVC.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles ="Admin")]
public class HomeController : Controller
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HomeController(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

public async Task<IActionResult> Index()
{
    var model = new AdminHomePageViewModel();
    var client = new HttpClient();
    try
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5289/products/admin/count?isdeleted=false");
        var authResult = await _httpContextAccessor.HttpContext!.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var token = authResult.Properties?.Items["access_token"];
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // 1. Ürün Sayısı
        var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ResponseDto<int>>(responseContent);
        model.ProductCount = result!.Data;

        // 2. Kategori Sayısı
        request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5289/categories/count");
        response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        responseContent = await response.Content.ReadAsStringAsync();
        result = JsonConvert.DeserializeObject<ResponseDto<int>>(responseContent);
        model.CategoryCount = result!.Data;

        // 3. Sipariş Toplamı (BUG FIXED)
        var startDate = DateTime.Now.Date.ToString("yyyy-MM-dd");
        var endDate = DateTime.Now.Date.AddDays(1).ToString("yyyy-MM-dd");
        var url = $"http://localhost:5289/orders/total?startDate={startDate}&endDate={endDate}";
        request = new HttpRequestMessage(HttpMethod.Get, url);

        response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();
        responseContent = await response.Content.ReadAsStringAsync();
        var resultOrdersTotal = JsonConvert.DeserializeObject<ResponseDto<decimal>>(responseContent);
        model.OrdersTotal = resultOrdersTotal!.Data;

        return View(model);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Hata: {ex.Message}");
        throw;
    }
}

}