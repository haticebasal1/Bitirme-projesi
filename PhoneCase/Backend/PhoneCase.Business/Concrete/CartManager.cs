using System;
using AutoMapper;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PhoneCase.Business.Abstract;
using PhoneCase.Data.Abstract;
using PhoneCase.Entities.Concrete;
using PhoneCase.Shared.Dtos.CartDtos;
using PhoneCase.Shared.Dtos.CartDtos.ChangeQuantityDto;
using PhoneCase.Shared.Dtos.ProductDtos;
using PhoneCase.Shared.Dtos.ResponseDtos;

namespace PhoneCase.Business.Concrete;

public class CartManager : ICartService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly IGenericRepository<Cart> _cartRepository;
    private readonly IGenericRepository<CartItem> _cartItemRepository;
    private readonly IGenericRepository<Product> _productRepository;

    public CartManager(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cartRepository = _unitOfWork.GetRepository<Cart>();
        _cartItemRepository = _unitOfWork.GetRepository<CartItem>();
        _productRepository = _unitOfWork.GetRepository<Product>();
    }

    public async Task<ResponseDto<CartItemDto>> AddToCartAsync(AddToCartDto addToCartDto)
    {
        try
        {
            var isExists = await _productRepository.ExistsAsync(x => x.Id == addToCartDto.ProductId);
            if (!isExists)
            {
                List<string> errors = [
                    "Ürün bulunamadı!",
                    "Ürün bulunamadığı için sepete eklenemedi!",
                    "Ürün silinmiş olabilir!"
];
                return ResponseDto<CartItemDto>.Fail(errors, StatusCodes.Status404NotFound);
            }
            var cart = await _cartRepository.GetAsync(
                predicate: x => x.UserId == addToCartDto.UserId,
                includes: query => query.Include(x => x.CartItems).ThenInclude(y => y.Product)
            );
            if (cart is null)
            {
                cart = new Cart(addToCartDto.UserId);
                await _cartRepository.AddAsync(cart);
                if (await _unitOfWork.SaveAsync() < 1)
                {
                    return ResponseDto<CartItemDto>.Fail("Sepet oluşturulamadı!", StatusCodes.Status500InternalServerError);
                }
            }

            var existsCartItem = cart.CartItems.FirstOrDefault(x => x.ProductId == addToCartDto.ProductId);
            CartItemDto cartItemDto = null!;
            if (existsCartItem is not null)
            {
                existsCartItem.Quantity += addToCartDto.Quantity;
                _cartItemRepository.Update(existsCartItem);
                if (await _unitOfWork.SaveAsync() < 1)
                {
                    return ResponseDto<CartItemDto>.Fail("Sunucuda bir sorun oluştuğu için ürün sepete eklenemedi!", StatusCodes.Status500InternalServerError);
                }
                cartItemDto = _mapper.Map<CartItemDto>(existsCartItem);
                return ResponseDto<CartItemDto>.Success(cartItemDto, StatusCodes.Status200OK);
            }
            var cartItem = new CartItem(cart.Id, addToCartDto.ProductId, addToCartDto.Quantity);
            cart.CartItems.Add(cartItem);
            _cartRepository.Update(cart);
            if (await _unitOfWork.SaveAsync() < 1)
            {
                return ResponseDto<CartItemDto>.Fail("Sunucuda bir sorun oluştuğu için ürün sepete eklenemedi!", StatusCodes.Status500InternalServerError);
            }
            cartItem = await _cartItemRepository.GetAsync(
                predicate: x => x.Id == cartItem.Id,
                includes: query => query.Include(x => x.Product)
            );
            cartItemDto = _mapper.Map<CartItemDto>(cartItem);
            return ResponseDto<CartItemDto>.Success(cartItemDto, StatusCodes.Status201Created);

        }
        catch (Exception ex)
        {
            return ResponseDto<CartItemDto>.Fail($"Beklenmedik Hata:{ex.Message}", StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<NoContentDto>> ChangeQuantityAsync(ChangeQuantityDto changeQuantityDto)
    {
        try
        {
            var cartItem = await _cartItemRepository.GetAsync(x => x.Id == changeQuantityDto.CartItemId);
            if (cartItem is null)
            {
                return ResponseDto<NoContentDto>.Fail("İlgili ürün sepette bulunamadı!", StatusCodes.Status404NotFound);
            }
            cartItem.Quantity = changeQuantityDto.Quantity;
            _cartItemRepository.Update(cartItem);
            if (await _unitOfWork.SaveAsync() < 1)
            {
                return ResponseDto<NoContentDto>.Fail("Sunucuda bir sorun oluştuğu için ürün adedi güncellenemedi!", StatusCodes.Status500InternalServerError);
            }
            return ResponseDto<NoContentDto>.Success(StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            return ResponseDto<NoContentDto>.Fail($"Beklenmedik Hata:{ex.Message}", StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<NoContentDto>> ClearCartAsync(string userId)
    {
        try
        {
            var cart = await _cartRepository.GetAsync(
                predicate: x => x.UserId == userId,
                includes: query => query.Include(x => x.CartItems)
            );
            if (cart is null)
            {
                return ResponseDto<NoContentDto>.Fail("Sepet bulunamadı!", StatusCodes.Status404NotFound);
            }
            cart.CartItems.Clear();
            _cartRepository.Update(cart);
            if (await _unitOfWork.SaveAsync() < 1)
            {
                return ResponseDto<NoContentDto>.Fail("Sunucuda bir sorun oluştuğu için sepet temizlenemedi!", StatusCodes.Status500InternalServerError);
            }
            return ResponseDto<NoContentDto>.Success(StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            return ResponseDto<NoContentDto>.Fail($"Beklenmedik Hata:{ex.Message}", StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<CartDto>> CreateCartAsync(CartCreateDto cartCreateDto)
    {
        try
        {
            var isExists = await _cartRepository.ExistsAsync(x => x.UserId == cartCreateDto.UserId);
            if (isExists)
            {
                return ResponseDto<CartDto>.Fail("Kullanıcının sepeti zaten var!", StatusCodes.Status400BadRequest);
            }
            var cart = new Cart(cartCreateDto.UserId);
            await _cartRepository.AddAsync(cart);
            if (await _unitOfWork.SaveAsync() < 1)
            {
                return ResponseDto<CartDto>.Fail("Sunucuda bir sorun oluştuğu için sepet yaratılamadı!", StatusCodes.Status500InternalServerError);
            }
            var cartDto = _mapper.Map<CartDto>(cart);
            return ResponseDto<CartDto>.Success(StatusCodes.Status201Created);
        }
        catch (Exception ex)
        {
            return ResponseDto<CartDto>.Fail($"Beklenmedik Hata:{ex.Message}", StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<CartDto>> GetCartAsync(string userId)
    {
        try
        {
            var cart = await _cartRepository.GetAsync(
                  predicate: x => x.UserId == userId,
                  includes: query => query.Include(x => x.CartItems).ThenInclude(y => y.Product)
              );

            if (cart is null)
            {
                return ResponseDto<CartDto>.Fail("Kullanıcıya ait sepet bulunamadı!", StatusCodes.Status404NotFound);
            }
            var cartDto = new CartDto
            {
                Id = cart.Id,
                UserId = cart.UserId,
                CartItems = cart.CartItems.Select(ci => new CartItemDto
                {
                    Id = ci.Id,
                    CartId = ci.CartId,
                    ProductId = ci.ProductId,
                    Quantity = ci.Quantity,
                    Product = new ProductDto
                    {
                        Id = ci.Product!.Id,
                        Name = ci.Product.Name,
                        Price = ci.Product.Price,
                        ImageUrl = ci.Product.ImageUrl
                    }
                }).ToList()
            };

            return ResponseDto<CartDto>.Success(cartDto, StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            return ResponseDto<CartDto>.Fail($"Beklenmedik Hata:{ex.Message}", StatusCodes.Status500InternalServerError);
        }
    }


    public async Task<ResponseDto<NoContentDto>> RemoveFromCartAsync(int cartItemId)
    {
        try
        {
            var cartItem = await _cartItemRepository.GetAsync(x => x.Id == cartItemId);
            if (cartItem is null)
            {
                return ResponseDto<NoContentDto>.Fail("Ürün sepette bulunamadığı için silinemedi!", StatusCodes.Status404NotFound);
            }
            _cartItemRepository.Delete(cartItem);
            if (await _unitOfWork.SaveAsync() < 1)
            {
                return ResponseDto<NoContentDto>.Fail("Sunucuda bir sorun oluştuğu için sepet yaratılamadı!", StatusCodes.Status500InternalServerError);
            }
            return ResponseDto<NoContentDto>.Success(StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            return ResponseDto<NoContentDto>.Fail($"Beklenmedik Hata:{ex.Message}", StatusCodes.Status500InternalServerError);
        }
    }
}
