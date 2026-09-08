-- Bảng cây menu điều hướng cho FE (Sider dạng cây, giống menu chức năng của FBO)
-- - không thuộc SalesLT (schema mẫu AdventureWorksLT), đặt riêng ở dbo với tiền tố
-- RH_ để phân biệt bảng của ReportHub. ReportCode NULL = node là 1 category (thư
-- mục), không NULL = node lá, click vào sẽ mở report tương ứng.
IF OBJECT_ID('dbo.RH_MenuItem', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.RH_MenuItem
    (
        Id         INT IDENTITY(1,1) PRIMARY KEY,
        ParentId   INT           NULL,
        Label      NVARCHAR(200) NOT NULL,
        ReportCode NVARCHAR(100) NULL,
        SortOrder  INT           NOT NULL DEFAULT 0,
        CONSTRAINT FK_RH_MenuItem_Parent
            FOREIGN KEY (ParentId) REFERENCES dbo.RH_MenuItem(Id)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.RH_MenuItem)
BEGIN
    INSERT INTO dbo.RH_MenuItem (ParentId, Label, ReportCode, SortOrder)
    VALUES (NULL, N'Báo cáo', NULL, 1);

    DECLARE @BaoCaoId INT = SCOPE_IDENTITY();

    INSERT INTO dbo.RH_MenuItem (ParentId, Label, ReportCode, SortOrder)
    VALUES
        (@BaoCaoId, N'Đơn hàng bán', N'RPT_SALES_ORDERS', 1),
        (@BaoCaoId, N'Report test',  N'RPT_TEST',         2);
END
GO
