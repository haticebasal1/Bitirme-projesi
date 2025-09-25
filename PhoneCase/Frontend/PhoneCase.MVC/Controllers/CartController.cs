using System.Net.Http.Headers;
using System.Text;
using PhoneCase.Shared.Dtos.CartDtos;
using PhoneCase.Shared.Dtos.ResponseDtos;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using NToastNotify;
using System.Security.Claims;
using Newtonsoft.Json.Linq;

namespace PhoneCase.MVC.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IToastNotification _toastr;
    public CartController(IHttpContextAccessor httpContextAccessor, IToastNotification toastr)
    {
        _httpContextAccessor = httpContextAccessor;
        _toastr = toastr;
    }

    public async Task<IActionResult> Index()
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "http://localhost:5289/carts");
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
                _toastr.AddErrorToastMessage(result!.Errors[0]);
            }
            return View(result!.Data);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hata: {ex.Message}");
            throw;
        }
    }

    [HttpPost]
    public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
    {
        var client = new HttpClient();
        try
        {
            var authResult = await _httpContextAccessor.HttpContext!.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var token = authResult.Properties?.Items["access_token"];

            if (string.IsNullOrEmpty(token))
            {
                _toastr.AddWarningToastMessage("Lütfen giriş yapınız.");
                return RedirectToAction("Login", "Auth");
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var addToCartDto = new AddToCartDto
            {
                ProductId = productId,
                Quantity = quantity
            };

            var jsonContent = JsonConvert.SerializeObject(addToCartDto);
            var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // API endpoint -> POST /carts
            var response = await client.PostAsync("http://localhost:5289/carts", stringContent);
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine("AddToCart Response: " + responseContent);
            var result = JsonConvert.DeserializeObject<ResponseDto<CartItemDto>>(responseContent);

            if (result == null || !result.IsSuccessful)
            {
                _toastr.AddErrorToastMessage(result?.Errors?.FirstOrDefault() ?? "Sepete eklenirken hata oluştu.");
                return RedirectToAction(nameof(Index)); // ✅ sepete yönlendirsin
            }


            _toastr.AddSuccessToastMessage("Ürün sepete eklendi.");
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hata: {ex.Message}");
            _toastr.AddErrorToastMessage("Beklenmedik bir hata oluştu.");
            return RedirectToAction("Index", "Product");
        }
    }


    public async Task<IActionResult> ChangeQuantity(int cartItemId, int quantity)
    {
        var client = new HttpClient();

        // 🔧 DOĞRU URL - interpolated string
        var request = new HttpRequestMessage(HttpMethod.Put,
            $"http://localhost:5289/carts/qty/{cartItemId}?quantity={quantity}");

        try
        {
            // 🔐 Token işlemleri
            var authResult = await _httpContextAccessor.HttpContext!
                .AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var token = authResult.Properties?.Items["access_token"];
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // 🚀 İstek gönder
            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            // 🧠 JSON işleme
            try
            {
                // ResponseDto<NoContentDto> deserialize etmeyi dene
                var result = JsonConvert.DeserializeObject<ResponseDto<NoContentDto>>(responseContent);

                if (result != null && !result.IsSuccessful)
                {
                    // Eğer hatalar bir nesne (dictionary gibi) gelirse — bu durumda cast edemeyiz
                    // Bu yüzden hataları elle çözüyoruz
                    var json = JsonConvert.DeserializeObject<dynamic>(responseContent);

                    if (json?.errors is JObject)
                    {
                        var firstField = ((JObject)json.errors).Properties().FirstOrDefault();
                        var firstError = firstField?.Value?.First?.ToString();
                        if (!string.IsNullOrEmpty(firstError))
                        {
                            _toastr.AddErrorToastMessage(firstError);
                        }
                    }
                    else if (json?.errors is JArray)
                    {
                        var firstError = json.errors[0]?.ToString();
                        if (!string.IsNullOrEmpty(firstError))
                        {
                            _toastr.AddErrorToastMessage(firstError);
                        }
                    }
                    else
                    {
                        _toastr.AddErrorToastMessage("Beklenmeyen bir hata oluştu.");
                    }
                }
            }
            catch (Exception parseEx)
            {
                Console.WriteLine("JSON çözümleme hatası: " + parseEx.Message);
                Console.WriteLine("Gelen JSON: " + responseContent);
                _toastr.AddErrorToastMessage("Sunucu yanıtı çözümlenemedi.");
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hata: {ex.Message}");
            _toastr.AddErrorToastMessage("Bir hata oluştu.");
            return RedirectToAction(nameof(Index));
        }
    }


    public async Task<IActionResult> RemoveFromCart([FromQuery] int cartItemId)
    {
        var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Delete, $"http://localhost:5289/carts/{cartItemId}");
        try
        {
            var authResult = await _httpContextAccessor.HttpContext!
                .AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            var token = authResult.Properties?.Items["access_token"];
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();

            var result = JsonConvert.DeserializeObject<ResponseDto<NoContentDto>>(responseContent);

            if (result == null || !result.IsSuccessful)
            {
                // 🔧 Sadece Remove için özel errors parse
                var json = JsonConvert.DeserializeObject<dynamic>(responseContent);

                if (json?.errors != null)
                {
                    if (json.errors is JObject) // {"cartItemId": ["hata"]}
                    {
                        var firstField = ((JObject)json.errors).Properties().FirstOrDefault();
                        var firstError = firstField?.Value?.First?.ToString();
                        if (!string.IsNullOrEmpty(firstError))
                            _toastr.AddErrorToastMessage(firstError);
                    }
                    else if (json.errors is JArray) // ["hata1", "hata2"]
                    {
                        var firstError = json.errors[0]?.ToString();
                        if (!string.IsNullOrEmpty(firstError))
                            _toastr.AddErrorToastMessage(firstError);
                    }
                    else
                    {
                        _toastr.AddErrorToastMessage("Beklenmeyen bir hata oluştu.");
                    }
                }
                else
                {
                    _toastr.AddErrorToastMessage("Sepetten silinirken hata oluştu.");
                }
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Hata: {ex.Message}");
            _toastr.AddErrorToastMessage("Bir hata oluştu.");
            return RedirectToAction(nameof(Index));
        }
    }

}