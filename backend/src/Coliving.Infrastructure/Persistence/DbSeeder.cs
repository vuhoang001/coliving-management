using Coliving.Application.Common;
using Coliving.Application.Interfaces;
using Coliving.Domain.Entities;
using Coliving.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Coliving.Infrastructure.Persistence;

/// <summary>Khởi tạo dữ liệu nền tảng: tài khoản admin + bộ dữ liệu demo phong phú cho mọi nghiệp vụ.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher, SeedSettings settings)
    {
        // ---- Tài khoản admin bootstrap ----
        var adminEmail = settings.Admin.Email.Trim().ToLowerInvariant();
        if (!await db.Users.AnyAsync(u => u.Email == adminEmail))
        {
            db.Users.Add(new User
            {
                Email = adminEmail,
                PasswordHash = hasher.Hash(settings.Admin.Password),
                FullName = settings.Admin.FullName,
                Phone = settings.Admin.Phone,
                Role = UserRole.Admin,
                EmailConfirmed = true
            });
            await db.SaveChangesAsync();
        }

        if (!settings.SeedDemoData) return;
        if (await db.Buildings.AnyAsync()) return; // đã seed demo rồi (idempotent)

        var now = DateTime.UtcNow;

        // ══════════ Người dùng ══════════
        var manager = NewUser(hasher, "manager@coliving.local", "Manager@123", "Trần Quản Lý", "0900000001", UserRole.Manager);
        var staff1 = NewUser(hasher, "staff@coliving.local", "Staff@123", "Lê Kỹ Thuật", "0900000002", UserRole.Staff);
        var staff2 = NewUser(hasher, "staff2@coliving.local", "Staff@123", "Võ Vệ Sinh", "0900000003", UserRole.Staff);
        db.Users.AddRange(manager, staff1, staff2);

        var tenantNames = new[]
        {
            "Nguyễn Văn An", "Trần Thị Bình", "Phạm Văn Cường", "Lê Thị Dung", "Hoàng Văn Em",
            "Đỗ Thị Phương", "Bùi Văn Giang", "Ngô Thị Hoa", "Đặng Văn Ích", "Vũ Thị Kim",
            "Dương Văn Long", "Lý Thị Mai", "Phan Văn Nam", "Tạ Thị Oanh"
        };
        var occupations = new[] { "Kỹ sư phần mềm", "Kế toán", "Sinh viên", "Nhân viên văn phòng", "Giáo viên", "Bác sĩ", "Nhân viên bán hàng", "Freelancer" };
        var tenants = new List<User>();
        for (var i = 0; i < tenantNames.Length; i++)
        {
            var email = $"tenant{i + 1}@coliving.local";
            if (i == 0) email = "an@coliving.local"; // giữ tài khoản demo quen thuộc
            var t = NewUser(hasher, email, "Tenant@123", tenantNames[i], $"09{(11111111 + i * 111):D8}",
                UserRole.Tenant, identity: $"0790{(12345670 + i):D8}");
            t.DateOfBirth = new DateTime(1990 + i % 12, 1 + i % 12, 1 + i % 27, 0, 0, 0, DateTimeKind.Utc);
            t.Gender = i % 2 == 0 ? "Male" : "Female";
            t.Nationality = "Việt Nam";
            t.PermanentAddress = $"Số {10 + i} đường Lê Lợi, TP. Biên Hoà, Đồng Nai";
            t.Occupation = occupations[i % occupations.Length];
            t.EmergencyContactName = $"Người thân của {tenantNames[i]}";
            t.EmergencyContactPhone = $"08{(22222222 + i * 131):D8}";
            t.IdIssueDate = now.AddYears(-3).AddDays(i);
            t.IdIssuePlace = "Cục Cảnh sát QLHC về TTXH";
            tenants.Add(t);
        }
        db.Users.AddRange(tenants);
        await db.SaveChangesAsync();

        // ══════════ Toà nhà ══════════
        var buildings = new[]
        {
            new Building { Name = "Coliving Sài Gòn", Address = "12 Nguyễn Huệ, Quận 1", City = "TP.HCM", Floors = 15, Description = "Chuỗi căn hộ dịch vụ trung tâm Quận 1.", IsActive = true, District = "Quận 1", Ward = "Phường Bến Nghé", ContactPhone = "02838111222", ContactEmail = "saigon@coliving.local", YearBuilt = 2018, TotalFloorArea = 4500, ParkingSlots = 40, HasElevator = true, Notes = "Toà nhà cao cấp, an ninh 24/7." },
            new Building { Name = "Coliving Thủ Đức", Address = "88 Võ Văn Ngân, TP.Thủ Đức", City = "TP.HCM", Floors = 20, Description = "Gần các trường đại học lớn.", IsActive = true, District = "TP. Thủ Đức", Ward = "Phường Bình Thọ", ContactPhone = "02838333444", ContactEmail = "thuduc@coliving.local", YearBuilt = 2020, TotalFloorArea = 6000, ParkingSlots = 60, HasElevator = true, Notes = "Phù hợp sinh viên và người đi làm trẻ." },
            new Building { Name = "Coliving Bình Thạnh", Address = "200 Điện Biên Phủ, Bình Thạnh", City = "TP.HCM", Floors = 12, Description = "Kết nối thuận tiện vào trung tâm.", IsActive = true, District = "Quận Bình Thạnh", Ward = "Phường 15", ContactPhone = "02838555666", ContactEmail = "binhthanh@coliving.local", YearBuilt = 2016, TotalFloorArea = 3800, ParkingSlots = 30, HasElevator = true, Notes = "Gần tuyến metro và trung tâm." }
        };
        db.Buildings.AddRange(buildings);
        await db.SaveChangesAsync();

        // ══════════ Căn hộ ══════════
        var aptSpecs = new (int bldg, string code, int floor, double area, int beds)[]
        {
            (0, "A-12A", 12, 78, 3), (0, "A-12B", 12, 45, 2), (0, "A-15A", 15, 40, 2),
            (1, "B-08A", 8, 72, 3), (1, "B-08B", 8, 44, 2),
            (2, "C-05A", 5, 46, 2), (2, "C-05B", 5, 42, 2)
        };
        var directions = new[] { "Đông Nam", "Tây Bắc", "Đông", "Nam", "Bắc", "Tây", "Đông Bắc" };
        var furnishings = new[] { "Full", "Basic", "Full", "Basic", "Unfurnished", "Full", "Basic" };
        var apartments = aptSpecs.Select((s, idx) => new Apartment
        {
            BuildingId = buildings[s.bldg].Id, Code = s.code, Floor = s.floor,
            Area = s.area, BedroomCount = s.beds, IsActive = true,
            Direction = directions[idx % directions.Length],
            Furnishing = furnishings[idx % furnishings.Length],
            HasBalcony = idx % 2 == 0,
            MaintenanceFee = 200_000 + (idx % 3) * 100_000,
            Notes = idx % 3 == 0 ? "Căn góc, thoáng, nhiều ánh sáng." : null
        }).ToList();
        db.Apartments.AddRange(apartments);
        await db.SaveChangesAsync();

        // ══════════ Phòng ══════════
        var rooms = new List<Room>();
        foreach (var (spec, apt) in aptSpecs.Zip(apartments))
        {
            for (var r = 1; r <= spec.beds; r++)
            {
                var shared = r == 1 && spec.beds >= 3;
                rooms.Add(new Room
                {
                    ApartmentId = apt.Id,
                    Code = $"{spec.code}-R{r}",
                    Type = shared ? RoomType.Shared : (spec.area >= 40 && spec.beds <= 2 && r == 1 ? RoomType.Studio : RoomType.Private),
                    Capacity = shared ? 2 : 1,
                    Area = shared ? 22 : 16 + r,
                    MonthlyPrice = 3_000_000 + r * 500_000 + spec.bldg * 500_000,
                    Deposit = 3_000_000 + r * 500_000 + spec.bldg * 500_000,
                    Status = RoomStatus.Available,
                    HasWindow = true,
                    HasPrivateBathroom = !shared,
                    HasAirConditioner = true,
                    ElectricityUnitPrice = 3_500,
                    WaterUnitPrice = 15_000,
                    InternetFee = 100_000,
                    Notes = shared ? "Phòng ở ghép, có 2 giường riêng." : null
                });
            }
        }
        db.Rooms.AddRange(rooms);
        await db.SaveChangesAsync();

        // ══════════ Tài sản ══════════
        var assets = new List<Asset>();
        foreach (var room in rooms.Take(10))
        {
            assets.Add(new Asset { Name = "Máy lạnh Daikin 1.5HP", Category = "HVAC", SerialNumber = $"DK-{room.Id:D4}", Status = AssetStatus.Good, PurchaseValue = 12_000_000, PurchaseDate = now.AddYears(-1), Brand = "Daikin", Model = "FTKB35", Quantity = 1, WarrantyUntil = now.AddYears(1), Supplier = "Điện máy Chợ Lớn", BuildingId = room.Apartment?.BuildingId, ApartmentId = room.ApartmentId, RoomId = room.Id });
            assets.Add(new Asset { Name = "Giường + nệm", Category = "Furniture", Status = AssetStatus.Good, PurchaseValue = 4_500_000, Brand = "Kymdan", Model = "Deluxe 1m6", Quantity = 1, Supplier = "Nội thất Hoà Phát", RoomId = room.Id, ApartmentId = room.ApartmentId });
        }
        assets.Add(new Asset { Name = "Tủ lạnh Samsung Inverter", Category = "Appliance", SerialNumber = "SS-2201", Status = AssetStatus.NeedsRepair, PurchaseValue = 8_000_000, Brand = "Samsung", Model = "RT29K5532", Quantity = 1, WarrantyUntil = now.AddMonths(-2), Supplier = "Nguyễn Kim", BuildingId = buildings[0].Id });
        assets.Add(new Asset { Name = "Máy giặt LG (chung)", Category = "Appliance", SerialNumber = "LG-7788", Status = AssetStatus.Good, PurchaseValue = 9_000_000, Brand = "LG", Model = "FV1409S4W", Quantity = 2, WarrantyUntil = now.AddYears(2), Supplier = "Điện máy Xanh", BuildingId = buildings[0].Id });
        assets.Add(new Asset { Name = "Máy nước nóng năng lượng mặt trời", Category = "Other", Status = AssetStatus.UnderMaintenance, PurchaseValue = 15_000_000, Brand = "Sơn Hà", Model = "Titan 180L", Quantity = 1, Supplier = "Sơn Hà Group", BuildingId = buildings[1].Id });
        db.Assets.AddRange(assets);

        // ══════════ Tiện ích chung ══════════
        db.Amenities.AddRange(
            new Amenity { BuildingId = buildings[0].Id, Name = "Bể bơi tầng thượng", Capacity = 20, OpenHour = 6, CloseHour = 21, SlotMinutes = 60, FeePerSlot = 0 },
            new Amenity { BuildingId = buildings[0].Id, Name = "Phòng Gym", Capacity = 12, OpenHour = 5, CloseHour = 22, SlotMinutes = 60, FeePerSlot = 0 },
            new Amenity { BuildingId = buildings[0].Id, Name = "Vườn BBQ", Capacity = 4, OpenHour = 8, CloseHour = 22, SlotMinutes = 120, FeePerSlot = 100_000 },
            new Amenity { BuildingId = buildings[1].Id, Name = "Phòng sinh hoạt chung", Capacity = 30, OpenHour = 7, CloseHour = 23, SlotMinutes = 60, FeePerSlot = 50_000 },
            new Amenity { BuildingId = buildings[1].Id, Name = "Sân thượng cà phê", Capacity = 15, OpenHour = 6, CloseHour = 22, SlotMinutes = 60, FeePerSlot = 0 },
            new Amenity { BuildingId = buildings[2].Id, Name = "Phòng họp / co-working", Capacity = 10, OpenHour = 7, CloseHour = 21, SlotMinutes = 60, FeePerSlot = 30_000 }
        );

        // ══════════ Danh mục dịch vụ ══════════
        var services = new[]
        {
            new ServiceCatalog { Name = "Giặt sấy", Category = "Laundry", UnitPrice = 25_000, Unit = "kg", IsActive = true },
            new ServiceCatalog { Name = "Vệ sinh phòng", Category = "Cleaning", UnitPrice = 150_000, Unit = "lần", IsActive = true },
            new ServiceCatalog { Name = "Sửa chữa điện nước", Category = "Maintenance", UnitPrice = 200_000, Unit = "lần", IsActive = true },
            new ServiceCatalog { Name = "Chuyển đồ nội khu", Category = "Moving", UnitPrice = 300_000, Unit = "lần", IsActive = true },
            new ServiceCatalog { Name = "Giặt rèm / chăn ga", Category = "Laundry", UnitPrice = 120_000, Unit = "bộ", IsActive = true }
        };
        db.ServiceCatalogs.AddRange(services);
        await db.SaveChangesAsync();

        // ══════════ Phân bổ khách ở: 10 phòng đầu có người ở ══════════
        var occupancies = new List<(Room room, List<User> people)>();
        var ti = 0;
        for (var i = 0; i < rooms.Count && i < 10; i++)
        {
            var room = rooms[i];
            var need = room.Type == RoomType.Shared ? 2 : 1;
            var people = new List<User>();
            for (var k = 0; k < need && ti < tenants.Count; k++) people.Add(tenants[ti++]);
            if (people.Count == 0) break;
            room.Status = RoomStatus.Occupied;
            occupancies.Add((room, people));

            foreach (var person in people)
            {
                var perPersonRent = room.MonthlyPrice / people.Count;
                var booking = new Booking
                {
                    Code = CodeGenerator.New("BK"), RoomId = room.Id, TenantId = person.Id,
                    CheckInDate = now.AddMonths(-6), CheckOutDate = now.AddMonths(6),
                    MonthlyPrice = perPersonRent, Deposit = room.Deposit / people.Count,
                    Status = BookingStatus.CheckedIn,
                    NumberOfOccupants = 1, Purpose = "Ở dài hạn đi làm",
                    SourceChannel = "Website", VehiclePlate = $"59X1-{(person.Id * 137) % 100000:D5}",
                    Contract = new Contract
                    {
                        ContractNumber = CodeGenerator.New("HD"), TenantId = person.Id,
                        StartDate = now.AddMonths(-6), EndDate = now.AddMonths(6),
                        MonthlyRent = perPersonRent, Deposit = room.Deposit / people.Count,
                        Status = ContractStatus.Active,
                        TenantSignature = person.FullName,
                        TenantSignedAt = now.AddMonths(-6), LandlordSignedAt = now.AddMonths(-6),
                        ContractType = "FixedTerm", PaymentCycle = "Monthly",
                        NoticePeriodDays = 30, LateFeePercent = 2, UtilitiesIncluded = false,
                        MaxOccupants = room.Type == RoomType.Shared ? 2 : 1, DepositPaid = true,
                        RenewalTerms = "Tự động gia hạn từng tháng nếu hai bên không có thông báo chấm dứt."
                    }
                };
                db.Bookings.Add(booking);
            }
        }

        // Phòng còn lại: 1 đã đặt (Reserved), 1 có yêu cầu chờ duyệt, 1 bảo trì.
        if (rooms.Count > 12 && ti + 1 < tenants.Count)
        {
            rooms[10].Status = RoomStatus.Reserved;
            db.Bookings.Add(new Booking
            {
                Code = CodeGenerator.New("BK"), RoomId = rooms[10].Id, TenantId = tenants[ti].Id,
                CheckInDate = now.AddDays(5), CheckOutDate = now.AddMonths(6),
                MonthlyPrice = rooms[10].MonthlyPrice, Deposit = rooms[10].Deposit,
                Status = BookingStatus.Confirmed,
                NumberOfOccupants = 1, Purpose = "Ở ngắn hạn công tác",
                SourceChannel = "Agent", VehiclePlate = "51F1-23456"
            });
            db.Bookings.Add(new Booking
            {
                Code = CodeGenerator.New("BK"), RoomId = rooms[11].Id, TenantId = tenants[ti + 1].Id,
                CheckInDate = now.AddDays(10), CheckOutDate = now.AddMonths(12),
                MonthlyPrice = rooms[11].MonthlyPrice, Deposit = rooms[11].Deposit,
                Status = BookingStatus.Pending,
                NumberOfOccupants = 2, Purpose = "Ở ghép cùng bạn",
                SourceChannel = "Referral"
            });
            rooms[12].Status = RoomStatus.Maintenance;
        }
        await db.SaveChangesAsync();

        // ══════════ Hoá đơn 6 tháng gần nhất + thanh toán + chia tiền ở ghép ══════════
        var firstOfThisMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var occIdx = 0;
        foreach (var occ in occupancies)
        {
            var primary = occ.people[0];
            for (var m = 0; m < 6; m++)
            {
                var month = firstOfThisMonth.AddMonths(m - 5);
                var isCurrent = m == 5;
                var kwh = 90 + (occ.room.Id * 7 + m * 13) % 120;
                var m3 = 5 + (occ.room.Id + m) % 8;
                // Chỉ số công tơ luỹ kế: kỳ trước = kỳ này - lượng tiêu thụ.
                var elecCurrent = 1000 + occ.room.Id * 500 + (m + 1) * kwh;
                var elecPrev = elecCurrent - kwh;
                var waterCurrent = 100 + occ.room.Id * 50 + (m + 1) * m3;
                var waterPrev = waterCurrent - m3;

                var items = new List<InvoiceItem>
                {
                    Item(InvoiceItemType.Rent, $"Tiền phòng {occ.room.Code}", 1, occ.room.MonthlyPrice, "tháng"),
                    Item(InvoiceItemType.Electricity, $"Tiền điện ({kwh} kWh)", kwh, 3_500, "kWh"),
                    Item(InvoiceItemType.Water, $"Tiền nước ({m3} m³)", m3, 15_000, "m³"),
                    Item(InvoiceItemType.Internet, "Internet", 1, 100_000, "tháng")
                };
                var subtotal = items.Sum(x => x.Amount);
                // Giảm giá nhỏ cho một vài hoá đơn (khách trả sớm / khuyến mãi).
                var discount = (occIdx + m) % 4 == 0 ? 50_000m : 0m;
                var tax = 0m;
                var total = Math.Max(0m, subtotal - discount + tax);

                var invoice = new Invoice
                {
                    InvoiceNumber = CodeGenerator.New("INV"), TenantId = primary.Id, RoomId = occ.room.Id,
                    PeriodStart = month, PeriodEnd = month.AddMonths(1).AddDays(-1),
                    IssueDate = month.AddDays(1), DueDate = month.AddDays(10),
                    Subtotal = subtotal, Total = total, Items = items,
                    PreviousElectricityReading = elecPrev, CurrentElectricityReading = elecCurrent,
                    PreviousWaterReading = waterPrev, CurrentWaterReading = waterCurrent,
                    Discount = discount, Tax = tax
                };

                var paid = !isCurrent || occIdx % 3 == 0;
                if (paid)
                {
                    invoice.Status = InvoiceStatus.Paid;
                    invoice.PaidAmount = total;
                    invoice.Payments.Add(new Payment
                    {
                        PaidById = primary.Id, Amount = total,
                        Method = (PaymentMethod)((occIdx + m) % 3), Status = PaymentStatus.Paid,
                        PaidAt = isCurrent ? now.AddDays(-1) : month.AddDays(5),
                        TransactionRef = $"SEED-{invoice.InvoiceNumber}"
                    });
                }
                else
                {
                    invoice.Status = occIdx % 3 == 2 ? InvoiceStatus.Overdue : InvoiceStatus.Issued;
                    if (invoice.Status == InvoiceStatus.Overdue) invoice.DueDate = now.AddDays(-3);
                }

                // Chia tiền cho người ở ghép.
                if (occ.people.Count > 1)
                {
                    var each = Math.Round(total / occ.people.Count, 0);
                    var running = 0m;
                    for (var k = 0; k < occ.people.Count; k++)
                    {
                        var amt = k == occ.people.Count - 1 ? total - running : each;
                        running += amt;
                        invoice.Shares.Add(new InvoiceShare
                        {
                            TenantId = occ.people[k].Id, ShareAmount = amt,
                            IsPaid = paid, PaidAt = paid ? invoice.Payments.FirstOrDefault()?.PaidAt : null
                        });
                    }
                }

                db.Invoices.Add(invoice);
            }
            occIdx++;
        }

        // ══════════ Sự cố ══════════
        db.Incidents.AddRange(
            new Incident { Code = CodeGenerator.New("INC"), Title = "Tủ lạnh không lạnh", Description = "Tủ lạnh bếp chung A-12A không làm lạnh từ sáng nay.", ReporterId = tenants[0].Id, Priority = IncidentPriority.High, Status = IncidentStatus.Open, BuildingId = buildings[0].Id, ApartmentId = apartments[0].Id, Category = "Appliance", LocationDetail = "Bếp chung căn A-12A", ContactPhone = tenants[0].Phone, ExpectedResolutionDate = now.AddDays(1), Cost = 0 },
            new Incident { Code = CodeGenerator.New("INC"), Title = "Rò rỉ nước nhà tắm", Description = "Vòi sen phòng B-08A-R2 bị rò rỉ.", ReporterId = tenants[8].Id, Priority = IncidentPriority.Medium, Status = IncidentStatus.InProgress, AssignedToId = staff1.Id, BuildingId = buildings[1].Id, Category = "Plumbing", LocationDetail = "Phòng B-08A-R2", ContactPhone = tenants[8].Phone, ExpectedResolutionDate = now.AddDays(2), Cost = 150_000 },
            new Incident { Code = CodeGenerator.New("INC"), Title = "Mất điện hành lang tầng 5", Description = "Đèn hành lang tầng 5 toà Bình Thạnh không sáng.", ReporterId = tenants[3].Id, Priority = IncidentPriority.Urgent, Status = IncidentStatus.InProgress, AssignedToId = staff1.Id, BuildingId = buildings[2].Id, Category = "Electrical", LocationDetail = "Hành lang tầng 5", ContactPhone = tenants[3].Phone, ExpectedResolutionDate = now, Cost = 300_000 },
            new Incident { Code = CodeGenerator.New("INC"), Title = "Ổ khoá cửa kẹt", Description = "Khoá từ phòng A-12B-R1 nhận thẻ chậm.", ReporterId = tenants[2].Id, Priority = IncidentPriority.Low, Status = IncidentStatus.Resolved, AssignedToId = staff2.Id, ResolvedAt = now.AddDays(-2), ResolutionNote = "Đã thay pin khoá từ.", BuildingId = buildings[0].Id, Category = "Security", LocationDetail = "Cửa phòng A-12B-R1", ContactPhone = tenants[2].Phone, Cost = 80_000 },
            new Incident { Code = CodeGenerator.New("INC"), Title = "Wifi chậm buổi tối", Description = "Wifi tầng 12 chậm vào giờ cao điểm.", ReporterId = tenants[1].Id, Priority = IncidentPriority.Medium, Status = IncidentStatus.Closed, AssignedToId = staff1.Id, ResolvedAt = now.AddDays(-5), ResolutionNote = "Đã nâng băng thông và đặt lại router.", BuildingId = buildings[0].Id, Category = "Internet", LocationDetail = "Tầng 12", ContactPhone = tenants[1].Phone, Cost = 0 }
        );

        // ══════════ Đặt lịch tiện ích (sắp tới) ══════════
        var pool = await db.Amenities.FirstAsync(a => a.Name.Contains("Bể bơi"));
        var gym = await db.Amenities.FirstAsync(a => a.Name == "Phòng Gym");
        var bbq = await db.Amenities.FirstAsync(a => a.Name.Contains("BBQ"));
        db.AmenityBookings.AddRange(
            new AmenityBooking { AmenityId = pool.Id, UserId = tenants[0].Id, StartTime = now.AddDays(1).Date.AddHours(18), EndTime = now.AddDays(1).Date.AddHours(19), PartySize = 1, Fee = 0, Status = AmenityBookingStatus.Booked },
            new AmenityBooking { AmenityId = gym.Id, UserId = tenants[1].Id, StartTime = now.AddDays(1).Date.AddHours(7), EndTime = now.AddDays(1).Date.AddHours(8), PartySize = 1, Fee = 0, Status = AmenityBookingStatus.Booked },
            new AmenityBooking { AmenityId = bbq.Id, UserId = tenants[4].Id, StartTime = now.AddDays(3).Date.AddHours(18), EndTime = now.AddDays(3).Date.AddHours(20), PartySize = 4, Fee = 100_000, Status = AmenityBookingStatus.Booked }
        );

        // ══════════ Yêu cầu dịch vụ ══════════
        db.ServiceRequests.AddRange(
            new ServiceRequest { Code = CodeGenerator.New("SR"), ServiceCatalogId = services[0].Id, RequesterId = tenants[0].Id, RoomId = rooms[0].Id, ScheduledAt = now.AddDays(1), Quantity = 3, UnitPrice = services[0].UnitPrice, TotalPrice = services[0].UnitPrice * 3, Status = ServiceRequestStatus.Requested, ContactPhone = tenants[0].Phone, LocationDetail = $"Phòng {rooms[0].Code}" },
            new ServiceRequest { Code = CodeGenerator.New("SR"), ServiceCatalogId = services[1].Id, RequesterId = tenants[5].Id, RoomId = rooms[5].Id, ScheduledAt = now.AddDays(2), Quantity = 1, UnitPrice = services[1].UnitPrice, TotalPrice = services[1].UnitPrice, Status = ServiceRequestStatus.Scheduled, AssignedToId = staff2.Id, ContactPhone = tenants[5].Phone, LocationDetail = $"Phòng {rooms[5].Code}" },
            new ServiceRequest { Code = CodeGenerator.New("SR"), ServiceCatalogId = services[2].Id, RequesterId = tenants[8].Id, RoomId = rooms[8].Id, ScheduledAt = now.AddDays(-1), Quantity = 1, UnitPrice = services[2].UnitPrice, TotalPrice = services[2].UnitPrice, Status = ServiceRequestStatus.Completed, AssignedToId = staff1.Id, ContactPhone = tenants[8].Phone, LocationDetail = $"Phòng {rooms[8].Code}" }
        );

        // ══════════ Thông báo mẫu ══════════
        db.Notifications.AddRange(
            new Notification { UserId = tenants[0].Id, Title = "Chào mừng đến Coliving", Message = "Tài khoản của bạn đã sẵn sàng. Xem hoá đơn và tiện ích trong trang cá nhân.", Type = "system" },
            new Notification { UserId = tenants[0].Id, Title = "Hoá đơn tháng này", Message = "Hoá đơn kỳ hiện tại đã phát hành, vui lòng thanh toán trước hạn.", Type = "invoice", IsRead = false }
        );

        await db.SaveChangesAsync();
    }

    private static User NewUser(IPasswordHasher hasher, string email, string password, string name,
        string? phone, UserRole role, string? identity = null) => new()
    {
        Email = email.ToLowerInvariant(), PasswordHash = hasher.Hash(password),
        FullName = name, Phone = phone, IdentityNumber = identity, Role = role, EmailConfirmed = true
    };

    private static InvoiceItem Item(InvoiceItemType type, string desc, decimal qty, decimal unitPrice, string? unit = null) => new()
    {
        Type = type, Description = desc, Quantity = qty, UnitPrice = unitPrice, Amount = qty * unitPrice, Unit = unit
    };
}
