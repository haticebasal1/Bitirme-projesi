using System;
using PhoneCase.Data.Abstract;
using PhoneCase.Shared.Dtos.OrderDtos;

namespace PhoneCase.Data.Concreate.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _appDbContext;

    public OrderRepository(AppDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }
    public decimal GetOrdersTotal(OrderFiltersDto orderFiltersDto)
    {
        var query = _appDbContext.Orders.AsQueryable();

        if (orderFiltersDto.OrderStatus.HasValue)
        {
            query = query.Where(o => o.OrderStatus == orderFiltersDto.OrderStatus.Value);
        }

        if (!string.IsNullOrEmpty(orderFiltersDto.UserId))
        {
            query = query.Where(o => o.UserId == orderFiltersDto.UserId);
        }

        if (orderFiltersDto.IsDeleted.HasValue)
        {
            query = query.Where(o => o.IsDeleted == orderFiltersDto.IsDeleted.Value);
        }


        if (orderFiltersDto.StartDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= orderFiltersDto.StartDate.Value);
        }

        if (orderFiltersDto.EndDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt <= orderFiltersDto.EndDate.Value);
        }


        var total = query
           .SelectMany(o => o.OrderItems)
           .Sum(oi => oi.UnitPrice * oi.Quantity);

        return total;
    }
}