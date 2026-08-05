using RHApplication.Reports.Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace RHApplication.Reports
{
    public interface ISalesReportService
    {
        Task<List<TopCustomerDto>> GetTopCustomersAsync(DateTime fromDate, DateTime toDate, int top = 10);
    }
}
