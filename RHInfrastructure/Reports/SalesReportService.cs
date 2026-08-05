using Microsoft.EntityFrameworkCore;
using RHApplication.Reports;
using RHApplication.Reports.Dtos;
using RHInfrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Text;

namespace RHInfrastructure.Reports
{
    public class SalesReportService : ISalesReportService
    {
        private readonly SourceDbContext _db;
        public SalesReportService(SourceDbContext db) => _db = db;
        public async Task<List<TopCustomerDto>> GetTopCustomersAsync(DateTime fromDate, DateTime toDate, int top = 10)
        {
            return await _db.SalesOrderHeaders
                .AsNoTracking()
                .Where(o => o.OrderDate >= fromDate && o.OrderDate <= toDate)
                .GroupBy(o => new { o.Customer.CustomerId, o.Customer.FirstName, o.Customer.LastName })
                .Select(g => new TopCustomerDto
                {
                    CustomerName = g.Key.FirstName + " " + g.Key.LastName,
                    TotalRevenue = g.Sum(o => o.TotalDue),
                    OrderCount = g.Count()
                })
                .OrderByDescending(x => x.TotalRevenue)
                .Take(top)
                .ToListAsync();
        }
    }
}
