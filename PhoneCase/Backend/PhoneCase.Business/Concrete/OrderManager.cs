using System;
using System.Linq.Expressions;
using AutoMapper;
using PhoneCase.Business.Abstract;
using PhoneCase.Data.Abstract;
using PhoneCase.Entities.Concrete;
using PhoneCase.Shared.Dtos.OrderDtos;
using PhoneCase.Shared.Dtos.ResponseDtos;
using LinqKit;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace PhoneCase.Business.Concrete;

public class OrderManager : IOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ICartService _cartManager;
    private readonly IGenericRepository<Order> _orderRepository;
    private readonly IOrderRepository _orderRepositorySpecial;
    private readonly IGenericRepository<Product> _productRepository;

    public OrderManager(IUnitOfWork unitOfWork, IMapper mapper, ICartService cartManager, IOrderRepository orderRepositorySpecial)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _cartManager = cartManager;
        _orderRepository = _unitOfWork.GetRepository<Order>();
        _productRepository = _unitOfWork.GetRepository<Product>();
        _orderRepositorySpecial = orderRepositorySpecial;
    }

    public async Task<ResponseDto<NoContentDto>> CancelOrderAsync(int id)
    {
        try
        {
            var order = await _orderRepository.GetAsync(
                predicate: x => x.Id == id,
                includes: query => query.Include(x => x.OrderItems)
            );
            if (order is null)
            {
                return ResponseDto<NoContentDto>.Fail("Sipariş bulunamadığı için iptal edilemedi!", StatusCodes.Status404NotFound);
            }
            order.IsDeleted = true;
            foreach (OrderItem orderItem in order.OrderItems)
            {
                orderItem.IsDeleted = true;
            }
            order.DeletedAt = DateTime.UtcNow;
            _orderRepository.Update(order);
            var result = await _unitOfWork.SaveAsync();
            if (result < 1)
            {
                return ResponseDto<NoContentDto>.Fail("Beklenmedik bir hata oluştu!", StatusCodes.Status500InternalServerError);
            }
            return ResponseDto<NoContentDto>.Success(StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            return ResponseDto<NoContentDto>.Fail($"Beklenmedik Hata: {ex.Message}", StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<NoContentDto>> ChangeOrderStatusAsync(ChangeOrderStatusDto changeOrderStatusDto)
    {
        try
        {
            var order = await _orderRepository.GetAsync(
                predicate: x => x.Id == changeOrderStatusDto.OrderId
            );
            if (order is null)
            {
                return ResponseDto<NoContentDto>.Fail("Sipariş bulunamadığı için iptal edilemedi!", StatusCodes.Status404NotFound);
            }
            order.OrderStatus = changeOrderStatusDto.OrderStatus;
            order.UpdatedAt = DateTime.UtcNow;
            _orderRepository.Update(order);
            var result = await _unitOfWork.SaveAsync();
            if (result < 1)
            {
                return ResponseDto<NoContentDto>.Fail("Beklenmedik bir hata oluştu!", StatusCodes.Status500InternalServerError);
            }
            return ResponseDto<NoContentDto>.Success(StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            return ResponseDto<NoContentDto>.Fail($"Beklenmedik Hata: {ex.Message}", StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<IEnumerable<OrderDto>>> GetAllAsync(OrderFiltersDto orderFiltersDto)
    {
        try
        {
            Expression<Func<Order, bool>> myPredicate = x => true;
            if (!string.IsNullOrEmpty(orderFiltersDto.UserId))
            {
                //x=>true && x.UserId == orderFiltersDto.UserId
                myPredicate = myPredicate.And(x => x.UserId == orderFiltersDto.UserId);
            }
            if (orderFiltersDto.OrderStatus.HasValue)
            {
                myPredicate = myPredicate.And(x => x.OrderStatus == orderFiltersDto.OrderStatus);
            }
            if (orderFiltersDto.StartDate.HasValue && orderFiltersDto.EndDate.HasValue)
            {
                myPredicate = myPredicate.And(x => x.CreatedAt >= orderFiltersDto.StartDate && x.CreatedAt <= orderFiltersDto.EndDate);
            }
            if (orderFiltersDto.IsDeleted.HasValue)
            {
                myPredicate = myPredicate.And(x => x.IsDeleted == orderFiltersDto.IsDeleted);
            }


            var orders = await _orderRepository.GetAllAsync(
                predicate: myPredicate,
                includes: query => query
                    .Include(x => x.User)
                    .Include(x => x.OrderItems)
                    .ThenInclude(y => y.Product),
                includeDeleted: orderFiltersDto.IsDeleted == false ? false : true
            );

            if (orders is null)
            {
                return ResponseDto<IEnumerable<OrderDto>>.Fail("Sipariş bilgileri getirilemedi", StatusCodes.Status500InternalServerError);
            }
            var orderDtos = _mapper.Map<IEnumerable<OrderDto>>(orders);
            return ResponseDto<IEnumerable<OrderDto>>.Success(orderDtos, StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            return ResponseDto<IEnumerable<OrderDto>>.Fail($"Beklenmedik Hata: {ex.Message}", StatusCodes.Status500InternalServerError);
        }
    }
    public async Task<ResponseDto<OrderDto>> GetMyOrderAsync(int id, string userId)
    {
        try
        {
            var order = await _orderRepository.GetAsync(
                predicate: x => x.Id == id && x.UserId == userId,
                includes: query => query
                    .Include(x => x.User)
                    .Include(x => x.OrderItems)
                    .ThenInclude(y => y.Product)
            );
            if (order is null)
            {
                return ResponseDto<OrderDto>.Fail("Sipariş bilgisi bulunamadı!", StatusCodes.Status404NotFound);
            }
            var orderDto = _mapper.Map<OrderDto>(order);
            return ResponseDto<OrderDto>.Success(orderDto, StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            return ResponseDto<OrderDto>.Fail($"Beklenmedik Hata: {ex.Message}", StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<OrderDto>> GetOrderAsync(int id)
    {
        try
        {
            var order = await _orderRepository.GetAsync(
                predicate: x => x.Id == id,
                includes: query => query
                    .Include(x => x.User)
                    .Include(x => x.OrderItems)
                    .ThenInclude(y => y.Product)
            );
            if (order is null)
            {
                return ResponseDto<OrderDto>.Fail("Sipariş bilgisi bulunamadı!", StatusCodes.Status404NotFound);
            }
            var orderDto = _mapper.Map<OrderDto>(order);
            return ResponseDto<OrderDto>.Success(orderDto, StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            return ResponseDto<OrderDto>.Fail($"Beklenmedik Hata: {ex.Message}", StatusCodes.Status500InternalServerError);
        }
    }

    public ResponseDto<decimal> GetOrdersTotal(OrderFiltersDto orderFiltersDto)
    {
        try
        {
            var total = _orderRepositorySpecial.GetOrdersTotal(orderFiltersDto);
            return ResponseDto<decimal>.Success(total, StatusCodes.Status200OK);
        }
        catch (Exception ex)
        {
            return ResponseDto<decimal>.Fail($"Beklenmedik Hata: {ex.Message}", StatusCodes.Status500InternalServerError);
        }
    }

    public async Task<ResponseDto<OrderDto>> OrderNowAsync(OrderNowDto orderNowDto)
    {
        try
        {
            List<OrderItemCreateDto> orderItems = [];
            for (int i = 0; i < orderNowDto.OrderItems.Count; i++)
            {
                var orderItem = orderNowDto.OrderItems.ToList()[i];
                var isExistsProduct = await _productRepository.ExistsAsync(x => x.Id == orderItem.ProductId);
                if (isExistsProduct)
                {
                    orderItems.Add(orderItem);
                    // return ResponseDto<OrderDto>.Fail($"{orderItem.ProductId} id'li ürün veri tabanında bulunamadığı için sipariş işlemi tamamlanamadı!", StatusCodes.Status404NotFound);
                }
            }
            if (orderItems.Count == 0)
            {
                return ResponseDto<OrderDto>.Fail($"{string.Join(", ", orderNowDto.OrderItems.Select(x => x.ProductId))} id'li ürün/ürünler veri tabanında bulunamadığı için sipariş işlemi tamamlanamadı!", StatusCodes.Status404NotFound);
            }
            orderNowDto.OrderItems.Clear();
            orderNowDto.OrderItems = orderItems;

            var order = _mapper.Map<Order>(orderNowDto);

            await _orderRepository.AddAsync(order);

            var result = await _unitOfWork.SaveAsync();
            if (result < 1)
            {
                return ResponseDto<OrderDto>.Fail("Beklenmedik bir hata oluştu!", StatusCodes.Status500InternalServerError);
            }
            // Madem sipariş tamamlandı, o zaman sepeti boşalt!
            await _cartManager.ClearCartAsync(orderNowDto.UserId!);
            var orderDto = _mapper.Map<OrderDto>(order);
            return ResponseDto<OrderDto>.Success(orderDto, StatusCodes.Status201Created);
        }
        catch (Exception ex)
        {
            return ResponseDto<OrderDto>.Fail($"Beklenmedik Hata: {ex.Message}", StatusCodes.Status500InternalServerError);
        }
    }
}
