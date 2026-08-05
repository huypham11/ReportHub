using System;
using System.Collections.Generic;
using System.Text;

namespace RHApplication.Reports.Dtos
{
    public class TopCustomerDto
    {
        public string CustomerName { get; set; } = default!;
        public decimal TotalRevenue { get; set; }
        public int OrderCount { get; set; }
    }
}
