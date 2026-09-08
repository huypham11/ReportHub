-- Proc demo cho tính năng "proc trả nhiều bảng, chọn bảng lên Grid" (ProcessTable) -
-- bảng 0 = tổng hợp (1 dòng), bảng 1 = chi tiết (nhiều dòng), giống use-case thật
-- (proc vừa trả summary vừa trả detail trong 1 lần gọi).
CREATE OR ALTER PROCEDURE dbo.usp_Report_Test_Multi
    @FromDate date,
    @BranchId int
AS
BEGIN
    -- Bảng 0: tổng hợp
    SELECT
        COUNT(*) AS OrderCount,
        SUM(h.TotalDue) AS TotalDue
    FROM SalesLT.SalesOrderHeader h
    WHERE h.OrderDate >= @FromDate;

    -- Bảng 1: chi tiết
    SELECT TOP 5
        h.SubTotal AS Amount,
        CAST(0 AS money) AS Discount,
        h.TotalDue AS Total,
        h.TaxAmt AS Tax
    FROM SalesLT.SalesOrderHeader h
    WHERE h.OrderDate >= @FromDate
    ORDER BY h.OrderDate DESC;
END
