using Domain.Entities;

namespace Shared.Helpers;

public static class TemplateSeeder
{
    public static List<Template> GetDefaultTemplates()
    {
        return new List<Template>
        {
            // Template 1: Văn Bản Trống
            new Template
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                Name = "Văn Bản Trống",
                Description = "Bắt đầu từ trang giấy trắng",
                Category = "Khác",
                Icon = "FileText",
                Color = "gray",
                Content = @"<div style=""font-family: 'Times New Roman', serif; font-size: 14px; line-height: 1.6; max-width: 800px; margin: 0 auto; padding: 40px;"">
    <h1 style=""text-align: center; font-size: 18px; font-weight: bold; margin-bottom: 30px;"">VĂN BẢN</h1>
    <div style=""margin-top: 50px;"">
        <p style=""text-align: justify; margin-bottom: 20px;"">Nội dung văn bản...</p>
    </div>
    <div style=""margin-top: 100px; display: flex; justify-content: space-around;"">
        <div style=""text-align: center;"">
            <p style=""margin-bottom: 50px;""><strong>BÊN A</strong></p>
            <p style=""border-top: 1px solid #000; padding-top: 5px; width: 200px;"">(Ký, ghi rõ họ tên)</p>
        </div>
        <div style=""text-align: center;"">
            <p style=""margin-bottom: 50px;""><strong>BÊN B</strong></p>
            <p style=""border-top: 1px solid #000; padding-top: 5px; width: 200px;"">(Ký, ghi rõ họ tên)</p>
        </div>
    </div>
</div>",
                Signers = new List<string> { "Bên A", "Bên B" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Template 2: Giấy Biên Nhận Tiền
            new Template
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                Name = "Giấy Biên Nhận Tiền",
                Description = "Biên lai nhận tiền cơ bản",
                Category = "Tài chính",
                Icon = "Receipt",
                Color = "green",
                Content = GetMoneyReceiptTemplate(),
                Signers = new List<string> { "Người nhận tiền", "Người gửi tiền" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Template 3: Hợp Đồng Lao Động
            new Template
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                Name = "Hợp Đồng Lao Động",
                Description = "Hợp đồng lao động có thời hạn/không thời hạn",
                Category = "Lao động",
                Icon = "Briefcase",
                Color = "blue",
                Content = GetLaborContractTemplate(),
                Signers = new List<string> { "Người sử dụng lao động (Bên A)", "Người lao động (Bên B)" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Template 4: Hợp Đồng Thuê Nhà
            new Template
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                Name = "Hợp Đồng Thuê Nhà",
                Description = "Hợp đồng thuê nhà ở, văn phòng, mặt bằng",
                Category = "Bất động sản",
                Icon = "Home",
                Color = "green",
                Content = GetRentalContractTemplate(),
                Signers = new List<string> { "Bên cho thuê (Bên A)", "Bên thuê (Bên B)" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Template 5: Giấy Vay Tiền/Hợp Đồng Vay
            new Template
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000005"),
                Name = "Hợp Đồng Vay",
                Description = "Hợp đồng vay mượn tiền có/không lãi suất",
                Category = "Tài chính",
                Icon = "Wallet",
                Color = "yellow",
                Content = GetLoanAgreementTemplate(),
                Signers = new List<string> { "Người cho vay (Bên A)", "Người vay (Bên B)" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Template 6: Hợp Đồng Mua Bán
            new Template
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000006"),
                Name = "Hợp Đồng Mua Bán",
                Description = "Hợp đồng mua bán hàng hóa, tài sản",
                Category = "Thương mại",
                Icon = "ShoppingCart",
                Color = "purple",
                Content = GetSalesContractTemplate(),
                Signers = new List<string> { "Bên bán (Bên A)", "Bên mua (Bên B)" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },

            // Template 7: Hợp Đồng Dịch Vụ
            new Template
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000007"),
                Name = "Hợp Đồng Dịch Vụ",
                Description = "Hợp đồng cung cấp dịch vụ",
                Category = "Thương mại",
                Icon = "Wrench",
                Color = "orange",
                Content = GetServiceContractTemplate(),
                Signers = new List<string> { "Bên cung cấp dịch vụ (Bên A)", "Bên sử dụng dịch vụ (Bên B)" },
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };
    }

    private static string GetMoneyReceiptTemplate()
    {
        return @"<div style=""font-family: 'Times New Roman', serif; font-size: 14px; line-height: 1.6; max-width: 800px; margin: 0 auto; padding: 40px;"">
    <h1 style=""text-align: center; font-size: 18px; font-weight: bold; margin-bottom: 30px; text-transform: uppercase;"">GIẤY BIÊN NHẬN TIỀN</h1>
    <div style=""margin-top: 30px;"">
        <p style=""margin-bottom: 15px;""><strong>Hôm nay, ngày</strong> _____ tháng _____ năm _____</p>
        <p style=""margin-bottom: 15px;"">Tôi là: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 15px;"">CMND/CCCD số: <strong>_________________________</strong></p>
        <p style=""margin-bottom: 15px;"">Nơi cấp: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 15px;"">Địa chỉ: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 30px;"">Điện thoại: <strong>_____________________________</strong></p>
        
        <p style=""margin-bottom: 15px;""><strong>Xin xác nhận đã nhận từ:</strong></p>
        <p style=""margin-bottom: 15px;"">Họ và tên: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 15px;"">Đơn vị/Địa chỉ: <strong>_____________________________</strong></p>
        <p style=""margin-bottom: 15px;"">CMND/CCCD số: <strong>_________________________</strong></p>
        <p style=""margin-bottom: 15px;"">Nơi cấp: <strong>_________________________________</strong></p>
        
        <p style=""margin-bottom: 15px; margin-top: 30px;""><strong>Số tiền:</strong></p>
        <p style=""margin-bottom: 15px;"">Bằng số: <strong>_________________________ VNĐ</strong></p>
        <p style=""margin-bottom: 15px;"">Bằng chữ: <strong>_________________________________</strong></p>
        
        <p style=""margin-bottom: 15px; margin-top: 30px;""><strong>Lý do nộp:</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">_________________________________</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">_________________________________</p>
        
        <p style=""margin-top: 50px; text-align: justify;"">Giấy biên nhận này được lập làm bằng chứng cho việc đã nhận tiền nêu trên. Tôi cam kết thông tin trên là đúng sự thật và chịu trách nhiệm trước pháp luật về nội dung này.</p>
    </div>
    <div style=""margin-top: 100px; display: flex; justify-content: space-around;"">
        <div style=""text-align: center;"">
            <p style=""margin-bottom: 50px;""><strong>NGƯỜI NHẬN TIỀN</strong></p>
            <p style=""border-top: 1px solid #000; padding-top: 5px; width: 200px;"">(Ký, ghi rõ họ tên)</p>
        </div>
        <div style=""text-align: center;"">
            <p style=""margin-bottom: 50px;""><strong>NGƯỜI GỬI TIỀN</strong></p>
            <p style=""border-top: 1px solid #000; padding-top: 5px; width: 200px;"">(Ký, ghi rõ họ tên)</p>
        </div>
    </div>
</div>";
    }

    private static string GetLaborContractTemplate()
    {
        return @"<div style=""font-family: 'Times New Roman', serif; font-size: 14px; line-height: 1.6; max-width: 800px; margin: 0 auto; padding: 40px;"">
    <h1 style=""text-align: center; font-size: 18px; font-weight: bold; margin-bottom: 30px; text-transform: uppercase;"">HỢP ĐỒNG LAO ĐỘNG</h1>
    <div style=""margin-top: 30px;"">
        <p style=""text-align: center; margin-bottom: 30px;""><strong>Căn cứ:</strong> Bộ luật Lao động năm 2019</p>
        <p style=""text-align: center; margin-bottom: 30px;""><strong>Căn cứ:</strong> Nhu cầu của các bên</p>
        
        <p style=""margin-bottom: 20px;""><strong>Hôm nay, ngày</strong> _____ tháng _____ năm _____</p>
        <p style=""margin-bottom: 20px;"">Chúng tôi gồm:</p>
        
        <p style=""margin-bottom: 15px; margin-top: 20px;""><strong>BÊN A (NGƯỜI SỬ DỤNG LAO ĐỘNG):</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Tên công ty/Đơn vị: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Mã số thuế: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Địa chỉ: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Người đại diện: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Chức vụ: <strong>_________________________________</strong></p>
        
        <p style=""margin-bottom: 15px; margin-top: 20px;""><strong>BÊN B (NGƯỜI LAO ĐỘNG):</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Họ và tên: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">CMND/CCCD số: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Nơi cấp: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Ngày cấp: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Địa chỉ thường trú: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Điện thoại: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Email: <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 30px; margin-bottom: 20px;"">Hai bên thỏa thuận ký kết hợp đồng lao động với các điều khoản sau:</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 1: THỜI HẠN HỢP ĐỒNG</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px; text-align: justify;"">Hợp đồng này có thời hạn từ ngày _____ tháng _____ năm _____ đến ngày _____ tháng _____ năm _____.</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 2: CÔNG VIỆC VÀ ĐỊA ĐIỂM LÀM VIỆC</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Bên B được giao công việc: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Mô tả công việc: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">Địa điểm làm việc: <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 3: THỜI GIỜ LÀM VIỆC VÀ NGHỈ NGƠI</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Thời gian làm việc: <strong>_________________________________</strong> giờ/ngày, <strong>_____</strong> ngày/tuần</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">Nghỉ phép năm: <strong>_____</strong> ngày/năm</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 4: TIỀN LƯƠNG</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Mức lương: <strong>_________________________________</strong> VNĐ/tháng</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Hình thức trả lương: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">Ngày trả lương: <strong>_________________________________</strong> hàng tháng</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 5: CHẾ ĐỘ ĐÀO TẠO, BỒI DƯỠNG</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px; text-align: justify;"">Bên A có trách nhiệm đào tạo, bồi dưỡng nâng cao trình độ chuyên môn, kỹ năng nghề nghiệp cho Bên B khi cần thiết.</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 6: BẢO HIỂM XÃ HỘI, BẢO HIỂM Y TẾ</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px; text-align: justify;"">Bên A có trách nhiệm đóng bảo hiểm xã hội, bảo hiểm y tế, bảo hiểm thất nghiệp cho Bên B theo quy định của pháp luật.</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 7: QUYỀN VÀ NGHĨA VỤ CỦA BÊN A</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Trả lương đầy đủ, đúng thời hạn</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Bảo đảm điều kiện làm việc an toàn</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Tôn trọng danh dự, nhân phẩm của người lao động</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">- Thực hiện đầy đủ các quy định của pháp luật về lao động</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 8: QUYỀN VÀ NGHĨA VỤ CỦA BÊN B</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Thực hiện công việc theo đúng hợp đồng</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Tuân thủ nội quy lao động, kỷ luật lao động</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Bồi thường thiệt hại nếu vi phạm hợp đồng</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">- Thực hiện đầy đủ các nghĩa vụ theo quy định của pháp luật</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 9: CHẤM DỨT HỢP ĐỒNG</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px; text-align: justify;"">Hợp đồng chấm dứt khi hết thời hạn hoặc theo quy định của Bộ luật Lao động năm 2019.</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 10: CAM KẾT</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px; text-align: justify;"">Hai bên cam kết thực hiện đúng các điều khoản đã thỏa thuận trong hợp đồng này. Mọi tranh chấp phát sinh sẽ được giải quyết thông qua thương lượng, hòa giải hoặc theo quy định của pháp luật.</p>
    </div>
    <div style=""margin-top: 100px; display: flex; justify-content: space-around;"">
        <div style=""text-align: center;"">
            <p style=""margin-bottom: 50px;""><strong>NGƯỜI SỬ DỤNG LAO ĐỘNG<br/>(BÊN A)</strong></p>
            <p style=""border-top: 1px solid #000; padding-top: 5px; width: 200px;"">(Ký, ghi rõ họ tên)</p>
        </div>
        <div style=""text-align: center;"">
            <p style=""margin-bottom: 50px;""><strong>NGƯỜI LAO ĐỘNG<br/>(BÊN B)</strong></p>
            <p style=""border-top: 1px solid #000; padding-top: 5px; width: 200px;"">(Ký, ghi rõ họ tên)</p>
        </div>
    </div>
</div>";
    }

    private static string GetRentalContractTemplate()
    {
        return @"<div style=""font-family: 'Times New Roman', serif; font-size: 14px; line-height: 1.6; max-width: 800px; margin: 0 auto; padding: 40px;"">
    <h1 style=""text-align: center; font-size: 18px; font-weight: bold; margin-bottom: 30px; text-transform: uppercase;"">HỢP ĐỒNG THUÊ NHÀ</h1>
    <div style=""margin-top: 30px;"">
        <p style=""text-align: center; margin-bottom: 30px;""><strong>Căn cứ:</strong> Bộ luật Dân sự năm 2015</p>
        <p style=""text-align: center; margin-bottom: 30px;""><strong>Căn cứ:</strong> Nhu cầu của các bên</p>
        
        <p style=""margin-bottom: 20px;""><strong>Hôm nay, ngày</strong> _____ tháng _____ năm _____</p>
        <p style=""margin-bottom: 20px;"">Chúng tôi gồm:</p>
        
        <p style=""margin-bottom: 15px; margin-top: 20px;""><strong>BÊN A (BÊN CHO THUÊ):</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Họ và tên: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">CMND/CCCD số: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Nơi cấp: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Địa chỉ: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Điện thoại: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Email: <strong>_________________________________</strong></p>
        
        <p style=""margin-bottom: 15px; margin-top: 20px;""><strong>BÊN B (BÊN THUÊ):</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Họ và tên: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">CMND/CCCD số: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Nơi cấp: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Địa chỉ: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Điện thoại: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Email: <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 30px; margin-bottom: 20px;"">Hai bên thỏa thuận ký kết hợp đồng thuê nhà với các điều khoản sau:</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 1: ĐỐI TƯỢNG CHO THUÊ</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Địa chỉ nhà cho thuê: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Diện tích: <strong>_________________________________</strong> m²</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">Mục đích sử dụng: <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 2: THỜI HẠN THUÊ</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Thời hạn thuê: Từ ngày _____ tháng _____ năm _____ đến ngày _____ tháng _____ năm _____</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">(Tổng cộng: <strong>_____</strong> tháng)</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 3: GIÁ THUÊ VÀ PHƯƠNG THỨC THANH TOÁN</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Giá thuê: <strong>_________________________________</strong> VNĐ/tháng</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Tiền đặt cọc: <strong>_________________________________</strong> VNĐ</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Phương thức thanh toán: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">Thời hạn thanh toán: Trước ngày _____ hàng tháng</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 4: TRÁCH NHIỆM CỦA BÊN A (CHO THUÊ)</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Giao nhà đúng thời hạn, đảm bảo chất lượng</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Sửa chữa các hư hỏng do hao mòn tự nhiên</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">- Không được đơn phương chấm dứt hợp đồng trước thời hạn</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 5: TRÁCH NHIỆM CỦA BÊN B (THUÊ)</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Thanh toán đầy đủ, đúng thời hạn</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Sử dụng đúng mục đích, không được cho thuê lại</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Bảo quản, giữ gìn tài sản</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">- Trả lại nhà đúng thời hạn, đúng tình trạng ban đầu</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 6: ĐIỀU KHOẢN CHẤM DỨT HỢP ĐỒNG</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px; text-align: justify;"">Hợp đồng chấm dứt khi hết thời hạn hoặc một trong hai bên vi phạm nghiêm trọng các điều khoản đã thỏa thuận.</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 7: CAM KẾT</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px; text-align: justify;"">Hai bên cam kết thực hiện đúng các điều khoản đã thỏa thuận trong hợp đồng này. Mọi tranh chấp phát sinh sẽ được giải quyết thông qua thương lượng, hòa giải hoặc theo quy định của pháp luật.</p>
    </div>
    <div style=""margin-top: 100px; display: flex; justify-content: space-around;"">
        <div style=""text-align: center;"">
            <p style=""margin-bottom: 50px;""><strong>BÊN CHO THUÊ<br/>(BÊN A)</strong></p>
            <p style=""border-top: 1px solid #000; padding-top: 5px; width: 200px;"">(Ký, ghi rõ họ tên)</p>
        </div>
        <div style=""text-align: center;"">
            <p style=""margin-bottom: 50px;""><strong>BÊN THUÊ<br/>(BÊN B)</strong></p>
            <p style=""border-top: 1px solid #000; padding-top: 5px; width: 200px;"">(Ký, ghi rõ họ tên)</p>
        </div>
    </div>
</div>";
    }

    private static string GetLoanAgreementTemplate()
    {
        return @"<div style=""font-family: 'Times New Roman', serif; font-size: 14px; line-height: 1.6; max-width: 800px; margin: 0 auto; padding: 40px;"">
    <h1 style=""text-align: center; font-size: 18px; font-weight: bold; margin-bottom: 30px; text-transform: uppercase;"">HỢP ĐỒNG VAY</h1>
    <div style=""margin-top: 30px;"">
        <p style=""text-align: center; margin-bottom: 30px;""><strong>Căn cứ:</strong> Bộ luật Dân sự năm 2015</p>
        <p style=""text-align: center; margin-bottom: 30px;""><strong>Căn cứ:</strong> Nhu cầu của các bên</p>
        
        <p style=""margin-bottom: 20px;""><strong>Hôm nay, ngày</strong> _____ tháng _____ năm _____</p>
        <p style=""margin-bottom: 20px;"">Chúng tôi gồm:</p>
        
        <p style=""margin-bottom: 15px; margin-top: 20px;""><strong>BÊN A (NGƯỜI CHO VAY):</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Họ và tên: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">CMND/CCCD số: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Nơi cấp: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Địa chỉ: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Điện thoại: <strong>_________________________________</strong></p>
        
        <p style=""margin-bottom: 15px; margin-top: 20px;""><strong>BÊN B (NGƯỜI VAY):</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Họ và tên: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">CMND/CCCD số: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Nơi cấp: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Địa chỉ: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Điện thoại: <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 30px; margin-bottom: 20px;"">Hai bên thỏa thuận ký kết hợp đồng vay với các điều khoản sau:</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 1: SỐ TIỀN VAY</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Bên A cho Bên B vay số tiền:</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Bằng số: <strong>_________________________________</strong> VNĐ</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">Bằng chữ: <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 2: MỤC ĐÍCH VAY</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">Mục đích vay: <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 3: THỜI HẠN VAY</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Thời hạn vay: <strong>_____</strong> tháng, từ ngày _____ tháng _____ năm _____</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">Ngày đáo hạn: <strong>_____</strong> tháng _____ năm _____</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 4: LÃI SUẤT</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Lãi suất: <strong>_____</strong> %/tháng (hoặc không có lãi suất)</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">Cách tính lãi: <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 5: PHƯƠNG THỨC TRẢ NỢ</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Phương thức trả nợ: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">(Một lần hoặc nhiều lần theo lịch đã thỏa thuận)</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 6: TRÁCH NHIỆM CỦA BÊN A (CHO VAY)</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Giao đủ số tiền vay cho Bên B</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">- Không được đơn phương thay đổi điều khoản hợp đồng</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 7: TRÁCH NHIỆM CỦA BÊN B (VAY)</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Trả đủ số tiền vay và lãi suất (nếu có) đúng thời hạn</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Sử dụng tiền vay đúng mục đích</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">- Bồi thường thiệt hại nếu vi phạm hợp đồng</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 8: TÀI SẢN ĐẢM BẢO</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">Tài sản đảm bảo (nếu có): <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 9: GIẢI QUYẾT TRANH CHẤP</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px; text-align: justify;"">Mọi tranh chấp phát sinh sẽ được giải quyết thông qua thương lượng, hòa giải. Nếu không giải quyết được, sẽ đưa ra Tòa án có thẩm quyền.</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 10: CAM KẾT</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px; text-align: justify;"">Hai bên cam kết thực hiện đúng các điều khoản đã thỏa thuận trong hợp đồng này và chịu trách nhiệm trước pháp luật về nội dung này.</p>
    </div>
    <div style=""margin-top: 100px; display: flex; justify-content: space-around;"">
        <div style=""text-align: center;"">
            <p style=""margin-bottom: 50px;""><strong>NGƯỜI CHO VAY<br/>(BÊN A)</strong></p>
            <p style=""border-top: 1px solid #000; padding-top: 5px; width: 200px;"">(Ký, ghi rõ họ tên)</p>
        </div>
        <div style=""text-align: center;"">
            <p style=""margin-bottom: 50px;""><strong>NGƯỜI VAY<br/>(BÊN B)</strong></p>
            <p style=""border-top: 1px solid #000; padding-top: 5px; width: 200px;"">(Ký, ghi rõ họ tên)</p>
        </div>
    </div>
</div>";
    }

    private static string GetSalesContractTemplate()
    {
        return @"<div style=""font-family: 'Times New Roman', serif; font-size: 14px; line-height: 1.6; max-width: 800px; margin: 0 auto; padding: 40px;"">
    <h1 style=""text-align: center; font-size: 18px; font-weight: bold; margin-bottom: 30px; text-transform: uppercase;"">HỢP ĐỒNG MUA BÁN</h1>
    <div style=""margin-top: 30px;"">
        <p style=""text-align: center; margin-bottom: 30px;""><strong>Căn cứ:</strong> Bộ luật Dân sự năm 2015</p>
        <p style=""text-align: center; margin-bottom: 30px;""><strong>Căn cứ:</strong> Luật Thương mại năm 2005</p>
        
        <p style=""margin-bottom: 20px;""><strong>Hôm nay, ngày</strong> _____ tháng _____ năm _____</p>
        <p style=""margin-bottom: 20px;"">Chúng tôi gồm:</p>
        
        <p style=""margin-bottom: 15px; margin-top: 20px;""><strong>BÊN A (BÊN BÁN):</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Họ và tên/Công ty: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">CMND/CCCD/MST: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Địa chỉ: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Điện thoại: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Email: <strong>_________________________________</strong></p>
        
        <p style=""margin-bottom: 15px; margin-top: 20px;""><strong>BÊN B (BÊN MUA):</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Họ và tên/Công ty: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">CMND/CCCD/MST: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Địa chỉ: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Điện thoại: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Email: <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 30px; margin-bottom: 20px;"">Hai bên thỏa thuận ký kết hợp đồng mua bán với các điều khoản sau:</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 1: ĐỐI TƯỢNG MUA BÁN</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Tên hàng hóa/tài sản: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Số lượng: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Đơn vị tính: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Chất lượng: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">Xuất xứ: <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 2: GIÁ CẢ VÀ PHƯƠNG THỨC THANH TOÁN</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Đơn giá: <strong>_________________________________</strong> VNĐ</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Tổng giá trị: <strong>_________________________________</strong> VNĐ</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Thuế VAT: <strong>_____</strong>% (nếu có)</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Tổng thanh toán: <strong>_________________________________</strong> VNĐ</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">Phương thức thanh toán: <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 3: THỜI GIAN VÀ ĐỊA ĐIỂM GIAO HÀNG</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Thời gian giao hàng: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">Địa điểm giao hàng: <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 4: NGHĨA VỤ CỦA BÊN A (BÁN)</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Giao hàng đúng số lượng, chất lượng, thời hạn</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Cung cấp đầy đủ giấy tờ, hóa đơn</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">- Bảo hành theo quy định</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 5: NGHĨA VỤ CỦA BÊN B (MUA)</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Thanh toán đầy đủ, đúng thời hạn</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Nhận hàng đúng thời hạn, địa điểm</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">- Kiểm tra hàng hóa trước khi nhận</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 6: BẢO HÀNH VÀ KHIẾU NẠI</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Thời gian bảo hành: <strong>_____</strong> tháng</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">Điều kiện bảo hành: <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 7: VI PHẠM HỢP ĐỒNG</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px; text-align: justify;"">Bên vi phạm hợp đồng phải bồi thường thiệt hại cho bên kia theo quy định của pháp luật.</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 8: GIẢI QUYẾT TRANH CHẤP</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px; text-align: justify;"">Mọi tranh chấp phát sinh sẽ được giải quyết thông qua thương lượng, hòa giải. Nếu không giải quyết được, sẽ đưa ra Tòa án có thẩm quyền.</p>
    </div>
    <div style=""margin-top: 100px; display: flex; justify-content: space-around;"">
        <div style=""text-align: center;"">
            <p style=""margin-bottom: 50px;""><strong>BÊN BÁN<br/>(BÊN A)</strong></p>
            <p style=""border-top: 1px solid #000; padding-top: 5px; width: 200px;"">(Ký, ghi rõ họ tên)</p>
        </div>
        <div style=""text-align: center;"">
            <p style=""margin-bottom: 50px;""><strong>BÊN MUA<br/>(BÊN B)</strong></p>
            <p style=""border-top: 1px solid #000; padding-top: 5px; width: 200px;"">(Ký, ghi rõ họ tên)</p>
        </div>
    </div>
</div>";
    }

    private static string GetServiceContractTemplate()
    {
        return @"<div style=""font-family: 'Times New Roman', serif; font-size: 14px; line-height: 1.6; max-width: 800px; margin: 0 auto; padding: 40px;"">
    <h1 style=""text-align: center; font-size: 18px; font-weight: bold; margin-bottom: 30px; text-transform: uppercase;"">HỢP ĐỒNG DỊCH VỤ</h1>
    <div style=""margin-top: 30px;"">
        <p style=""text-align: center; margin-bottom: 30px;""><strong>Căn cứ:</strong> Bộ luật Dân sự năm 2015</p>
        <p style=""text-align: center; margin-bottom: 30px;""><strong>Căn cứ:</strong> Luật Thương mại năm 2005</p>
        
        <p style=""margin-bottom: 20px;""><strong>Hôm nay, ngày</strong> _____ tháng _____ năm _____</p>
        <p style=""margin-bottom: 20px;"">Chúng tôi gồm:</p>
        
        <p style=""margin-bottom: 15px; margin-top: 20px;""><strong>BÊN A (BÊN CUNG CẤP DỊCH VỤ):</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Tên công ty/cá nhân: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">MST/CMND: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Địa chỉ: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Người đại diện: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Chức vụ: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Email: <strong>_________________________________</strong></p>
        
        <p style=""margin-bottom: 15px; margin-top: 20px;""><strong>BÊN B (BÊN SỬ DỤNG DỊCH VỤ):</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Tên công ty/cá nhân: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">MST/CMND: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Địa chỉ: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Người đại diện: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Chức vụ: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Email: <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 30px; margin-bottom: 20px;"">Hai bên thỏa thuận ký kết hợp đồng dịch vụ với các điều khoản sau:</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 1: NỘI DUNG DỊCH VỤ</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Tên dịch vụ: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Phạm vi: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">Mô tả chi tiết: <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 2: THỜI GIAN THỰC HIỆN</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Thời gian bắt đầu: <strong>_________________________________</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">Thời gian hoàn thành: <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 3: GIÁ TRỊ HỢP ĐỒNG VÀ THANH TOÁN</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Tổng giá trị hợp đồng: <strong>_________________________________</strong> VNĐ</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">Tiến độ thanh toán:</p>
        <p style=""margin-bottom: 5px; padding-left: 40px;"">- Đợt 1: <strong>_____</strong>% khi ký hợp đồng</p>
        <p style=""margin-bottom: 5px; padding-left: 40px;"">- Đợt 2: <strong>_____</strong>% khi hoàn thành <strong>_____</strong>% công việc</p>
        <p style=""margin-bottom: 5px; padding-left: 40px;"">- Đợt 3: <strong>_____</strong>% khi nghiệm thu hoàn thành</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">Phương thức thanh toán: <strong>_________________________________</strong></p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 4: TRÁCH NHIỆM CỦA BÊN A (CUNG CẤP DỊCH VỤ)</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Thực hiện đúng, đầy đủ nội dung dịch vụ đã thỏa thuận</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Bảo đảm chất lượng dịch vụ</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Hoàn thành đúng thời hạn</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">- Bảo mật thông tin của Bên B</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 5: TRÁCH NHIỆM CỦA BÊN B (SỬ DỤNG DỊCH VỤ)</strong></p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Cung cấp đầy đủ thông tin, tài liệu cần thiết</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Thanh toán đầy đủ, đúng thời hạn</p>
        <p style=""margin-bottom: 10px; padding-left: 20px;"">- Phối hợp với Bên A trong quá trình thực hiện</p>
        <p style=""margin-bottom: 15px; padding-left: 20px;"">- Nghiệm thu và thanh toán khi hoàn thành</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 6: BẢO MẬT THÔNG TIN</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px; text-align: justify;"">Hai bên cam kết bảo mật thông tin của nhau và không tiết lộ cho bên thứ ba mà không có sự đồng ý của bên kia.</p>
        
        <p style=""margin-top: 20px; margin-bottom: 10px;""><strong>ĐIỀU 7: ĐIỀU KHOẢN CHẤM DỨT HỢP ĐỒNG</strong></p>
        <p style=""margin-bottom: 15px; padding-left: 20px; text-align: justify;"">Hợp đồng chấm dứt khi hoàn thành dịch vụ hoặc một trong hai bên vi phạm nghiêm trọng các điều khoản đã thỏa thuận.</p>
    </div>
    <div style=""margin-top: 100px; display: flex; justify-content: space-around;"">
        <div style=""text-align: center;"">
            <p style=""margin-bottom: 50px;""><strong>BÊN CUNG CẤP DỊCH VỤ<br/>(BÊN A)</strong></p>
            <p style=""border-top: 1px solid #000; padding-top: 5px; width: 200px;"">(Ký, ghi rõ họ tên)</p>
        </div>
        <div style=""text-align: center;"">
            <p style=""margin-bottom: 50px;""><strong>BÊN SỬ DỤNG DỊCH VỤ<br/>(BÊN B)</strong></p>
            <p style=""border-top: 1px solid #000; padding-top: 5px; width: 200px;"">(Ký, ghi rõ họ tên)</p>
        </div>
    </div>
</div>";
    }
}

