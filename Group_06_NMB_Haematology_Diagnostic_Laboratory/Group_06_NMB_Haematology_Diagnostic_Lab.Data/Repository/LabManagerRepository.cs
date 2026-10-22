using Dapper;
using System.Data;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.DataAccess;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Helpers;
using Group_06_NMB_Haematology_Diagnostic_Lab.Data.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group_06_NMB_Haematology_Diagnostic_Lab.Data.Repository
{
    public class LabManagerRepository : ILabManagerRepository
    {
        private readonly ISqlDataAccess _db;

        public LabManagerRepository(ISqlDataAccess db)
        {
            _db = db;
        }

        public async Task<LabManagerDashboardCounts> GetDashboardCounts()
        {
            const string sql = """
                SELECT
                    (SELECT COUNT(*) FROM dbo.TestType WHERE IsActive = 1) AS TestTypeCount,
                    (SELECT COUNT(*) FROM dbo.TestCategory WHERE IsActive = 1) AS CategoryCount,
                    (SELECT COUNT(*) FROM dbo.Doctor d INNER JOIN dbo.[User] u ON u.UserID = d.UserID WHERE u.IsActive = 1) AS DoctorCount,
                    (SELECT COUNT(*) FROM dbo.LabTechnician lt INNER JOIN dbo.[User] u ON u.UserID = lt.UserID WHERE u.IsActive = 1) AS TechnicianCount,
                    (SELECT COUNT(*) FROM dbo.LabManager lm INNER JOIN dbo.[User] u ON u.UserID = lm.UserID WHERE u.IsActive = 1) AS ManagerCount,
                    (SELECT COUNT(*) FROM dbo.Consumable WHERE IsActive = 1) AS ConsumableCount;
                """;

            return await _db.QueryFirstOrDefaultAsync<LabManagerDashboardCounts>(sql)
                ?? new LabManagerDashboardCounts();
        }

        // The generated staff procedures return the legacy User columns.  Addresses
        // live in UserAddress, so attach them consistently after each staff lookup.
        private async Task PopulateStructuredAddresses(IEnumerable<User?> users)
        {
            var addresses = (await _db.GetData<UserAddress, object>(
                "sp_Address_GetAllUserAddresses", new { }))
                .GroupBy(address => address.UserId)
                .ToDictionary(group => group.Key, group => group.First());

            foreach (var user in users.Where(user => user != null))
                user!.StructuredAddress = addresses.GetValueOrDefault(user.UserId);
        }

        public Task<IEnumerable<Province>> GetProvinces() =>
            _db.GetData<Province, object>("sp_Address_GetProvinces", new { });

        public Task<IEnumerable<TownCity>> GetTownCities(int provinceId) =>
            _db.GetData<TownCity, object>("sp_Address_GetTownCitiesByProvince", new { ProvinceID = provinceId });

        public Task<IEnumerable<Suburb>> GetSuburbs(int townCityId) =>
            _db.GetData<Suburb, object>("sp_Address_GetSuburbsByTownCity", new { TownCityID = townCityId });

        // ============================== DOCTOR ================================================================
        //=======================================================================================================

        // ------------------- GET/DISPLAY DOCTORS -----------
        public async Task<IEnumerable<Doctor>> GetDoctors(string filter = "Active")
        {
            var doctors = await _db.GetData<Doctor, User, Doctor, dynamic>(
                "sp_Doctor_GetAll",
                (doctor, user) =>
                {
                    doctor.User = user;
                    return doctor;
                },
                new
                {
                    Filter = filter
                },
                splitOn: "UserId"
            );

            var doctorList = doctors.ToList();
            await PopulateStructuredAddresses(doctorList.Select(doctor => doctor.User));

            return doctorList
                .OrderBy(doctor => doctor.User?.Surname ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(doctor => doctor.User?.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }
        // -------------- CREATE/ADD DOCTORS ----------------
        public async Task CreateDoctor(Doctor doctor, string temporaryPassword)
        {
            if (!PasswordHelper.MeetsPolicy(temporaryPassword))
                throw new ArgumentException("Temporary password does not meet the password policy.", nameof(temporaryPassword));
            string password = temporaryPassword;

            string salt = PasswordHelper.GenerateSalt();

            string hash = PasswordHelper.HashPassword(password, salt);

            await _db.SaveData(
                "sp_Doctor_Create",
                new
                {
                    doctor.User.Name,
                    doctor.User.Surname,
                    doctor.User.Email,

                    PasswordHash = hash,
                    PasswordSalt = salt,

                    doctor.User.Cellphone,
                    HomeAddress = (string?)null,

                    SA_ID_Number = doctor.User.SaIdNumber,

                    doctor.User.DateOfBirth,

                    HPCSA_Number = doctor.HpcsaNumber,
                    AddressLine1 = doctor.User.StructuredAddress?.AddressLine1,
                    AddressLine2 = doctor.User.StructuredAddress?.AddressLine2,
                    SuburbID = doctor.User.StructuredAddress?.SuburbId
                });
        }
        //-------------------------- DET DOCTOR BY ID [For Delete & Update]
        public async Task<Doctor?> GetDoctorById(int doctorId)
        {
            var doctor = await _db.GetData<Doctor, User, Doctor, dynamic>(
                "sp_Doctor_GetById",
                (doctor, user) =>
                {
                    doctor.User = user;
                    return doctor;
                },
                new
                {
                    DoctorID = doctorId
                },
                splitOn: "UserID"
            );

            var result = doctor.FirstOrDefault();
            if (result?.User != null) await PopulateStructuredAddresses(new[] { result.User });
            return result;
        }
        // ---------------------- UPDATE DOCTOR ----------------------
        public async Task UpdateDoctor(Doctor doctor)
        {
            await _db.SaveData(
                "sp_Doctor_Update",
                new
                {
                    doctor.DoctorId,

                    doctor.User.Name,
                    doctor.User.Surname,
                    doctor.User.Email,
                    doctor.User.Cellphone,
                    HomeAddress = (string?)null,

                    SA_ID_Number = doctor.User.SaIdNumber,

                    doctor.User.DateOfBirth,

                    HPCSA_Number = doctor.HpcsaNumber,
                    AddressLine1 = doctor.User.StructuredAddress?.AddressLine1,
                    AddressLine2 = doctor.User.StructuredAddress?.AddressLine2,
                    SuburbID = doctor.User.StructuredAddress?.SuburbId
                });
        }
        // --------------DELETE DOCTOR------[Soft delete]
        public async Task DeleteDoctor(int doctorId)
        {
            await _db.SaveData(
                "sp_Doctor_Delete",
                new
                {
                    DoctorID = doctorId
                });
        }
        // ------------ REACTIVATE -----------------------
        public async Task ReactivateDoctor(int doctorId)
        {
            await _db.SaveData(
                "sp_Doctor_Reactivate",
                new
                {
                    DoctorID = doctorId
                });
        }
        //===========================================================================================================
        //===========================================================================================================





        // ============================TECHNICIAN ===============================================================
        //=======================================================================================================

        //------------------------- DISPLAY TECHNICIANS
        public async Task<IEnumerable<LabTechnician>> GetTechnicians(string filter = "Active")
        {
            var technicians = await _db.GetData<LabTechnician, User, LabTechnician, dynamic>(
                "sp_LabTechnician_GetAll",
                (technician, user) =>
                {
                    technician.User = user;
                    return technician;
                },
                new
                {
                    Filter = filter
                },
                splitOn: "UserId"
            );

            var technicianList = technicians.ToList();
            await PopulateStructuredAddresses(technicianList.Select(technician => technician.User));

            return technicianList
                .OrderBy(technician => technician.User?.Surname ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(technician => technician.User?.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

        // ----------------SELECT TECH BY ID [For Delete & Update]------------------
        public async Task<LabTechnician?> GetTechnicianById(int technicianId)
        {
            var technician = await _db.GetData<LabTechnician, User, LabTechnician, dynamic>(
                "sp_LabTechnician_GetById",
                (technician, user) =>
                {
                    technician.User = user;
                    return technician;
                },
                new
                {
                    LabTechnicianID = technicianId
                },
                splitOn: "UserId"
            );

            var result = technician.FirstOrDefault();
            if (result?.User != null) await PopulateStructuredAddresses(new[] { result.User });
            return result;
        }

        // ------------------------   CREATE / ADD TECHNICIAN --------------------------------
        public async Task<int> CreateTechnician(LabTechnician technician, string temporaryPassword, IEnumerable<int> testTypeIds)
        {
            if (!PasswordHelper.MeetsPolicy(temporaryPassword))
                throw new ArgumentException("Temporary password does not meet policy.", nameof(temporaryPassword));
            var assignments = testTypeIds.Distinct().ToArray();
            if (assignments.Length == 0) throw new ArgumentException("Assign at least one test type.", nameof(testTypeIds));
            var salt = PasswordHelper.GenerateSalt();
            var hash = PasswordHelper.HashPassword(temporaryPassword, salt);
            var technicianId = 0;
            // Account and required assignments commit together, before any welcome email is sent.
            await _db.ExecuteInTransaction(async (connection, transaction) =>
            {
                technicianId = await connection.ExecuteScalarAsync<int>("sp_LabTechnician_Create", new
                {
                    technician.User.Name, technician.User.Surname, technician.User.Email,
                    PasswordHash = hash, PasswordSalt = salt, technician.User.Cellphone,
                    HomeAddress = (string?)null, SA_ID_Number = technician.User.SaIdNumber,
                    technician.User.DateOfBirth, technician.EmployeeNumber,
                    AddressLine1 = technician.User.StructuredAddress?.AddressLine1,
                    AddressLine2 = technician.User.StructuredAddress?.AddressLine2,
                    SuburbID = technician.User.StructuredAddress?.SuburbId
                }, transaction, commandType: CommandType.StoredProcedure);
                await connection.ExecuteAsync("sp_LabTechnicianTestType_Replace", new
                {
                    LabTechnicianID = technicianId, TestTypeIDs = string.Join(",", assignments)
                }, transaction, commandType: CommandType.StoredProcedure);
            });
            return technicianId;
        }
        //  ---------------------------- UPDATE TECHNICIAN -----------------------------------
        public async Task UpdateTechnician(LabTechnician technician)
        {
            await _db.SaveData(
                "sp_LabTechnician_Update",
                new
                {
                    technician.LabTechnicianId,

                    technician.User.Name,
                    technician.User.Surname,
                    technician.User.Email,
                    technician.User.Cellphone,
                    HomeAddress = (string?)null,

                    SA_ID_Number = technician.User.SaIdNumber,

                    technician.User.DateOfBirth,

                    technician.EmployeeNumber,
                    AddressLine1 = technician.User.StructuredAddress?.AddressLine1,
                    AddressLine2 = technician.User.StructuredAddress?.AddressLine2,
                    SuburbID = technician.User.StructuredAddress?.SuburbId,


                });
        }
        // ------------------------ DELETE TECHNICIAN [Soft Delete] ------------
        public async Task DeleteTechnician(int technicianId)
        {
            await _db.SaveData(
                "sp_LabTechnician_Delete",
                new
                {
                    LabTechnicianID = technicianId
                });
        }

        // ------------------------ REACTIVATE TECHNICIAN ------------------------
        public async Task ReactivateTechnician(int technicianId)
        {
            await _db.SaveData(
                "sp_LabTechnician_Reactivate",
                new
                {
                    LabTechnicianID = technicianId
                });
        }

        public Task<IEnumerable<LabTechnicianTestType>> GetTechnicianTestTypes(int technicianId) =>
            _db.GetData<LabTechnicianTestType, object>(
                "sp_LabTechnicianTestType_GetByTechnician",
                new { LabTechnicianID = technicianId });

        public Task<IEnumerable<LabTechnicianTestType>> GetTestTypeAssignments() =>
            _db.GetData<LabTechnicianTestType, object>("sp_LabTechnicianTestType_GetByTechnician", new { });

        public Task ReplaceTestTypeTechnicians(int testTypeId, IEnumerable<int> technicianIds, string expectedTechnicianIds) =>
            _db.SaveData("sp_LabTechnicianTestType_Replace", new {
                TestTypeID = testTypeId, TechnicianIDs = string.Join(",", technicianIds.Distinct()),
                ExpectedTechnicianIDs = expectedTechnicianIds });

        public Task ReplaceTechnicianTestTypes(int technicianId, IEnumerable<int> testTypeIds) =>
            _db.SaveData(
                "sp_LabTechnicianTestType_Replace",
                new
                {
                    LabTechnicianID = technicianId,
                    TestTypeIDs = string.Join(",", testTypeIds.Distinct())
                });
        //==================================================================================================


        //================================ LAB MANAGER =====================================================
        //==================================================================================================


        // ------------------------- DISPLAY MANAGERS -------------------------
        public async Task<IEnumerable<LabManager>> GetManagers(string filter = "Active")
        {
            var managers = await _db.GetData<LabManager, User, LabManager, dynamic>(
                "sp_LabManager_GetAll",
                (manager, user) =>
                {
                    manager.User = user;
                    return manager;
                },
                new
                {
                    Filter = filter
                },
                splitOn: "UserId"
            );

            var managerList = managers.ToList();
            await PopulateStructuredAddresses(managerList.Select(manager => manager.User));

            return managerList
                .OrderBy(manager => manager.User?.Surname ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(manager => manager.User?.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

        // ------------------------- GET MANAGER BY ID -------------------------
        public async Task<LabManager?> GetManagerById(int managerId)
        {
            var manager = await _db.GetData<LabManager, User, LabManager, dynamic>(
                "sp_LabManager_GetById",
                (manager, user) =>
                {
                    manager.User = user;
                    return manager;
                },
                new
                {
                    LabManagerID = managerId
                },
                splitOn: "UserId"
            );

            var result = manager.FirstOrDefault();
            if (result?.User != null) await PopulateStructuredAddresses(new[] { result.User });
            return result;
        }

        // ------------------------- CREATE MANAGER -------------------------
        public async Task CreateManager(LabManager manager, string temporaryPassword)
        {
            if (!PasswordHelper.MeetsPolicy(temporaryPassword))
                throw new ArgumentException("Temporary password does not meet the password policy.", nameof(temporaryPassword));
            string password = temporaryPassword;

            string salt = PasswordHelper.GenerateSalt();

            string hash = PasswordHelper.HashPassword(password, salt);

            await _db.SaveData(
                "sp_LabManager_Create",
                new
                {
                    manager.User.Name,
                    manager.User.Surname,
                    manager.User.Email,

                    PasswordHash = hash,
                    PasswordSalt = salt,

                    manager.User.Cellphone,
                    HomeAddress = (string?)null,

                    SA_ID_Number = manager.User.SaIdNumber,

                    manager.User.DateOfBirth,
                    AddressLine1 = manager.User.StructuredAddress?.AddressLine1,
                    AddressLine2 = manager.User.StructuredAddress?.AddressLine2,
                    SuburbID = manager.User.StructuredAddress?.SuburbId
                });
        }

        // ------------------------- UPDATE MANAGER -------------------------
        public async Task UpdateManager(LabManager manager)
        {
            await _db.SaveData(
                "sp_LabManager_Update",
                new
                {
                    manager.LabManagerId,

                    manager.User.Name,
                    manager.User.Surname,
                    manager.User.Email,
                    manager.User.Cellphone,
                    HomeAddress = (string?)null,

                    SA_ID_Number = manager.User.SaIdNumber,

                    manager.User.DateOfBirth,
                    AddressLine1 = manager.User.StructuredAddress?.AddressLine1,
                    AddressLine2 = manager.User.StructuredAddress?.AddressLine2,
                    SuburbID = manager.User.StructuredAddress?.SuburbId
                });
        }

        // ------------------------- DELETE MANAGER -------------------------
        public async Task DeleteManager(int managerId)
        {
            await _db.SaveData(
                "sp_LabManager_Delete",
                new
                {
                    LabManagerID = managerId
                });
        }

        // ------------------------- REACTIVATE MANAGER -------------------------
        public async Task ReactivateManager(int managerId)
        {
            await _db.SaveData(
                "sp_LabManager_Reactivate",
                new
                {
                    LabManagerID = managerId
                });
        }


        //================================ SUPPLIER ======================================================
        //================================================================================================

        // ------------------------- DISPLAY SUPPLIERS -------------------------
        public async Task<IEnumerable<Supplier>> GetSuppliers(
        string filter = "Active",
         string search = "")
        {
            var suppliers = await _db.GetData<Supplier, dynamic>(
                "sp_Supplier_GetAll",
                new
                {
                    Filter = filter,
                    Search = search
                });

            return suppliers.OrderBy(supplier => supplier.SupplierName ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

        // ------------------------- GET SUPPLIER BY ID -------------------------
        public async Task<Supplier?> GetSupplierById(int supplierId)
        {
            var supplier = await _db.GetData<Supplier, dynamic>(
                "sp_Supplier_GetById",
                new
                {
                    SupplierID = supplierId
                }
            );

            return supplier.FirstOrDefault();
        }

        // ------------------------- CREATE SUPPLIER -------------------------
        public async Task CreateSupplier(Supplier supplier)
        {
            await _db.SaveData(
                "sp_Supplier_Create",
                new
                {
                    supplier.SupplierName,
                    supplier.ContactPerson,
                    supplier.SupplierEmail
                });
        }

        // ------------------------- UPDATE SUPPLIER -------------------------
        public async Task UpdateSupplier(Supplier supplier)
        {
            await _db.SaveData(
                "sp_Supplier_Update",
                new
                {
                    supplier.SupplierId,
                    supplier.SupplierName,
                    supplier.ContactPerson,
                    supplier.SupplierEmail
                });
        }

        // ------------------------- DELETE SUPPLIER [Soft Delete] -------------------------
        public async Task DeleteSupplier(int supplierId)
        {
            await _db.SaveData(
                "sp_Supplier_Delete",
                new
                {
                    SupplierID = supplierId
                });
        }

        // ------------------------ REACTIVATE -----------------------------------------------
        public async Task ReactivateSupplier(int supplierId)
        {
            await _db.SaveData(
                "sp_Supplier_Reactivate",
                new
                {
                    SupplierID = supplierId
                });
        }


        // ============================ CONSUMABLES ============================

        // ------------------------- DISPLAY CONSUMABLES -------------------------

        public async Task<IEnumerable<Consumable>> GetConsumables(
        string filter = "Active",
        string search = "")
        {
            var consumables = (await _db.GetData<Consumable, dynamic>(
                "sp_Consumable_GetAll",
                new
                {
                    Filter = filter,
                    Search = search
                })).ToList();

            foreach (var consumable in consumables)
            {
                consumable.Supplier = await GetSupplierById(consumable.SupplierId);
            }

            return consumables.OrderBy(consumable => consumable.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

        // ------------------------- GET CONSUMABLE BY ID -------------------------

        public async Task<Consumable?> GetConsumableById(int consumableId)
        {
            var consumable = (await _db.GetData<Consumable, dynamic>(
                "sp_Consumable_GetById",
                new
                {
                    ConsumableID = consumableId
                })).FirstOrDefault();

            if (consumable != null)
            {
                consumable.Supplier = await GetSupplierById(consumable.SupplierId);
            }

            return consumable;
        }

        // ------------------------- CREATE CONSUMABLE -------------------------

        public async Task CreateConsumable(
            Consumable consumable,
            int? performedByUserId = null)
        {
            await _db.SaveData(
                "sp_Consumable_Create",
                new
                {
                    consumable.SupplierId,
                    consumable.Name,
                    consumable.ReorderLevel,
                    consumable.QuantityOnHand,
                    PerformedByUserID = performedByUserId
                });
        }

        // ------------------------- UPDATE CONSUMABLE -------------------------

        public async Task UpdateConsumable(Consumable consumable)
        {
            await _db.SaveData(
                "sp_Consumable_UpdateDetails",
                new
                {
                    consumable.ConsumableId,
                    consumable.SupplierId,
                    consumable.Name
                });
        }

        public Task AdjustConsumableLevel(int consumableId, string field, string mode, int value, int expectedValue, int performedByUserId, string? reason) =>
            _db.SaveData("sp_Consumable_AdjustLevel", new {
                ConsumableID = consumableId, Field = field, Mode = mode, Value = value,
                ExpectedValue = expectedValue, PerformedByUserID = performedByUserId, Reason = reason });
        // ------------------------- DELETE CONSUMABLE -------------------------

        public async Task DeleteConsumable(int consumableId)
        {
            await _db.SaveData(
                "sp_Consumable_Delete",
                new
                {
                    ConsumableID = consumableId
                });
        }

        // ------------------------- REACTIVATE CONSUMABLE -------------------------

        public async Task ReactivateConsumable(int consumableId)
        {
            await _db.SaveData(
                "sp_Consumable_Reactivate",
                new
                {
                    ConsumableID = consumableId
                });
        }

        // ------------------------- STOCK TRANSACTION -------------------------

        public async Task UpdateConsumableStock(
            int consumableId,
            int changeAmount,
            string reason,
            int? referenceId = null,
            int? performedByUserId = null)
        {
            await _db.SaveData(
                "sp_Consumable_StockTransaction",
                new
                {
                    ConsumableID = consumableId,
                    ChangeAmount = changeAmount,
                    Reason = reason,
                    ReferenceID = referenceId,
                    PerformedByUserID = performedByUserId
                });
        }

        // ============================ STOCK ORDERS ============================

        public async Task<IEnumerable<StockOrder>> GetStockOrders(
            string filter = "All",
            string search = "")
        {
            var orders = (await _db.GetData<StockOrder, dynamic>(
                "sp_StockOrder_GetAll",
                new { Filter = filter, Search = search })).ToList();

            for (var index = 0; index < orders.Count; index++)
            {
                var detailedOrder = await GetStockOrderById(orders[index].StockOrderId);
                if (detailedOrder != null)
                {
                    orders[index] = detailedOrder;
                }
                else
                {
                    orders[index].Supplier = await GetSupplierById(orders[index].SupplierId);
                }
            }

            // The order screen is operational rather than alphabetical: a manager
            // needs the most recently placed orders at the top of the table.
            return orders
                .OrderByDescending(order => order.OrderDate ?? DateTime.MinValue)
                .ThenByDescending(order => order.StockOrderId);
        }

        public async Task<StockOrder?> GetStockOrderById(int stockOrderId)
        {
            var rows = (await _db.GetData<dynamic, dynamic>(
                "sp_StockOrder_GetById",
                new { StockOrderID = stockOrderId })).ToList();

            if (!rows.Any())
            {
                return null;
            }

            var first = (IDictionary<string, object>)rows[0];
            var order = new StockOrder
            {
                StockOrderId = ReadValue<int>(first, "StockOrderID"),
                SupplierId = ReadValue<int>(first, "SupplierID"),
                OrderNumber = ReadValue<string>(first, "OrderNumber"),
                OrderDate = ReadValue<DateTime?>(first, "OrderDate"),
                Status = ReadValue<string>(first, "Status"),
                CompletionDate = ReadValue<DateTime?>(first, "CompletionDate"),
                CancelledDate = ReadValue<DateTime?>(first, "CancelledDate"),
                CancelReason = ReadValue<string>(first, "CancelReason"),
                Supplier = new Supplier
                {
                    SupplierId = ReadValue<int>(first, "SupplierMappingID"),
                    SupplierName = ReadValue<string>(first, "SupplierName"),
                    ContactPerson = ReadValue<string>(first, "ContactPerson"),
                    SupplierEmail = ReadValue<string>(first, "SupplierEmail"),
                    IsActive = ReadValue<bool>(first, "SupplierIsActive")
                }
            };

            foreach (var row in rows.Cast<IDictionary<string, object>>())
            {
                order.StockOrderItem.Add(new StockOrderItem
                {
                    StockOrderItemId = ReadValue<int>(row, "StockOrderItemID"),
                    StockOrderId = ReadValue<int>(row, "ItemStockOrderID"),
                    ConsumableId = ReadValue<int>(row, "ConsumableID"),
                    QuantityOrdered = ReadValue<int>(row, "QuantityOrdered"),
                    Status = ReadValue<string>(row, "ItemStatus"),
                    DateReceived = ReadValue<DateTime?>(row, "DateReceived"),
                    CancelReason = ReadValue<string>(row, "ItemCancelReason"),
                    CancelledDate = ReadValue<DateTime?>(row, "ItemCancelledDate"),
                    Consumable = new Consumable
                    {
                        ConsumableId = ReadValue<int>(row, "ConsumableMappingID"),
                        SupplierId = ReadValue<int>(row, "ConsumableSupplierID"),
                        Name = ReadValue<string>(row, "Name"),
                        ReorderLevel = ReadValue<int>(row, "ReorderLevel"),
                        QuantityOnHand = ReadValue<int>(row, "QuantityOnHand"),
                        IsActive = ReadValue<bool>(row, "ConsumableIsActive")
                    }
                });
            }

            order.StockOrderItem = order.StockOrderItem
                .OrderBy(item => item.Consumable?.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return order;
        }

        public async Task<int> CreateStockOrderWithItems(StockOrder order, IEnumerable<(int ConsumableId, int Quantity)> items)
        {
            var lines = items.ToList();
            if (lines.Count == 0 || lines.Any(x => x.ConsumableId <= 0 || x.Quantity <= 0) ||
                lines.Select(x => x.ConsumableId).Distinct().Count() != lines.Count)
                throw new ArgumentException("Choose distinct consumables with positive quantities.");
            var id = 0;
            await _db.ExecuteInTransaction(async (connection, transaction) =>
            {
                id = await connection.QuerySingleAsync<int>("dbo.sp_StockOrder_Create",
                    new { order.SupplierId, order.OrderNumber }, transaction, commandType: CommandType.StoredProcedure);
                foreach (var item in lines)
                    await connection.ExecuteAsync("dbo.sp_StockOrder_AddItem",
                        new { StockOrderID = id, ConsumableID = item.ConsumableId, QuantityOrdered = item.Quantity },
                        transaction, commandType: CommandType.StoredProcedure);
            });
            return id;
        }

        public Task ReceiveStockOrder(int stockOrderId) =>
            _db.SaveData("sp_StockOrder_ReceiveItem", new { StockOrderID = stockOrderId });

        public Task<int> CreateStockOrder(StockOrder order) =>
            _db.SaveDataAndReturn<int, dynamic>(
                "sp_StockOrder_Create",
                new
                {
                    order.SupplierId,
                    order.OrderNumber
                });

        public Task CreateStockOrderItem(int stockOrderId, int consumableId, int quantityOrdered) =>
            _db.SaveData(
                "sp_StockOrder_AddItem",
                new
                {
                    StockOrderID = stockOrderId,
                    ConsumableID = consumableId,
                    QuantityOrdered = quantityOrdered
                });

        public Task ReceiveStockOrderItem(int stockOrderItemId) =>
            _db.SaveData(
                "sp_StockOrder_ReceiveItem",
                new { StockOrderItemID = stockOrderItemId });

        public Task CancelStockOrderItem(int stockOrderItemId, string cancelReason) =>
            _db.SaveData(
                "sp_StockOrder_CancelItem",
                new { StockOrderItemID = stockOrderItemId, CancelReason = cancelReason });

        public Task CancelStockOrder(int stockOrderId, string cancelReason) =>
            _db.SaveData(
                "sp_StockOrder_Cancel",
                new { StockOrderID = stockOrderId, CancelReason = cancelReason });

        public async Task<IEnumerable<Consumable>> GetLowStockConsumables()
        {
            var consumables = (await _db.GetData<Consumable, dynamic>(
                "sp_StockOrder_GetLowStock",
                new { })).ToList();

            foreach (var consumable in consumables)
            {
                consumable.Supplier = await GetSupplierById(consumable.SupplierId);
            }

            return consumables.OrderBy(consumable => consumable.Name ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

        private static T ReadValue<T>(IDictionary<string, object> row, string columnName)
        {
            if (!row.TryGetValue(columnName, out var value) || value is null || value == DBNull.Value)
            {
                return default!;
            }

            var targetType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(value, targetType);
        }

        // ==================== TEST TYPES ====================================

        // ============================================================
        // GET ALL
        // ============================================================

        public async Task<IEnumerable<TestType>> GetTestTypes(
            string filter = "Active",
            string search = "")
        {
            var parameters = new
            {
                Filter = filter,
                Search = search ?? ""
            };

            return MapTestTypes(await _db.GetData<dynamic, object>("sp_TestType_GetAll", parameters))
                .OrderBy(testType => testType.TestName ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }


        // ============================================================
        // GET BY ID
        // ============================================================

        public async Task<TestType?> GetTestTypeById(int testTypeId)
        {
            var parameters = new
            {
                TestTypeID = testTypeId
            };

            return MapTestTypes(await _db.GetData<dynamic, object>("sp_TestType_GetById", parameters))
                .FirstOrDefault();
        }


        // ============================================================
        // CREATE
        // ============================================================

        public async Task<int> CreateTestType(
            int testCategoryId,
            string testName,
            int sampleTypeId,
            string unitOfMeasurement,
            decimal? normalRangeMin,
            decimal? normalRangeMax,
            int turnaroundTimeMinutes,
            int consumableId,
            int quantityRequired)
        {
            var parameters = new
            {
                TestCategoryID = testCategoryId,
                TestName = testName,
                SampleTypeID = sampleTypeId,
                UnitOfMeasurement = unitOfMeasurement,
                NormalRangeMin = normalRangeMin,
                NormalRangeMax = normalRangeMax,
                TurnaroundTimeMinutes = turnaroundTimeMinutes,
                ConsumableID = consumableId,
                QuantityRequired = quantityRequired
            };

            return await _db.SaveDataAndReturn<int, object>(
                "sp_TestType_Create",
                parameters);
        }


        // ============================================================
        // UPDATE
        // ============================================================

        public async Task UpdateTestType(
            int testTypeId,
            int testCategoryId,
            string testName,
            int sampleTypeId,
            string unitOfMeasurement,
            decimal? normalRangeMin,
            decimal? normalRangeMax,
            int turnaroundTimeMinutes)
        {
            var parameters = new
            {
                TestTypeID = testTypeId,
                TestCategoryID = testCategoryId,
                TestName = testName,
                SampleTypeID = sampleTypeId,
                UnitOfMeasurement = unitOfMeasurement,
                NormalRangeMin = normalRangeMin,
                NormalRangeMax = normalRangeMax,
                TurnaroundTimeMinutes = turnaroundTimeMinutes
            };

            await _db.SaveData(
                "sp_TestType_Update",
                parameters);
        }


        // ============================================================
        // DELETE / DEACTIVATE
        // ============================================================

        public async Task DeleteTestType(int testTypeId)
        {
            var parameters = new
            {
                TestTypeID = testTypeId
            };

            await _db.SaveData(
                "sp_TestType_Delete",
                parameters);
        }


        // ============================================================
        // REACTIVATE
        // ============================================================

        public async Task ReactivateTestType(int testTypeId)
        {
            var parameters = new
            {
                TestTypeID = testTypeId
            };

            await _db.SaveData(
                "sp_TestType_Reactivate",
                parameters);
        }


        private static IEnumerable<TestType> MapTestTypes(IEnumerable<dynamic> rows)
        {
            var testTypes = new Dictionary<int, TestType>();

            foreach (IDictionary<string, object> row in rows)
            {
                var testTypeId = ReadValue<int>(row, "TestTypeId");
                if (!testTypes.TryGetValue(testTypeId, out var testType))
                {
                    testType = new TestType
                    {
                        TestTypeId = testTypeId,
                        TestCategoryId = ReadValue<int>(row, "TestCategoryId"),
                        TestName = ReadValue<string>(row, "TestName"),
                        SampleTypeId = ReadValue<int>(row, "SampleTypeId"),
                        UnitOfMeasurement = ReadValue<string>(row, "UnitOfMeasurement"),
                        NormalRangeMin = ReadValue<decimal?>(row, "NormalRangeMin"),
                        NormalRangeMax = ReadValue<decimal?>(row, "NormalRangeMax"),
                        TurnaroundTimeMinutes = ReadValue<int>(row, "TurnaroundTimeMinutes"),
                        CreatedAt = ReadValue<DateTime?>(row, "TestTypeCreatedAt"),
                        IsActive = ReadValue<bool>(row, "TestTypeIsActive"),
                        TestCategory = new TestCategory
                        {
                            TestCategoryId = ReadValue<int>(row, "CategoryId"),
                            CategoryName = ReadValue<string>(row, "CategoryName")
                        },
                        SampleType = new SampleType
                        {
                            SampleTypeId = ReadValue<int>(row, "SampleTypeId"),
                            SampleTypeName = ReadValue<string>(row, "SampleTypeName")
                        }
                    };
                    testTypes.Add(testTypeId, testType);
                }

                var requirementId = ReadValue<int>(row, "TestTypeConsumableId");
                if (requirementId != 0)
                {
                    testType.TestTypeConsumable.Add(new TestTypeConsumable
                    {
                        TestTypeConsumableId = requirementId,
                        TestTypeId = testTypeId,
                        ConsumableId = ReadValue<int>(row, "ConsumableId"),
                        QuantityRequired = ReadValue<int>(row, "QuantityRequired"),
                        Consumable = new Consumable
                        {
                            ConsumableId = ReadValue<int>(row, "ConsumableId"),
                            Name = ReadValue<string>(row, "ConsumableName") ?? ReadValue<string>(row, "Name"),
                            QuantityOnHand = ReadValue<int>(row, "QuantityOnHand"),
                            IsActive = ReadValue<bool>(row, "ConsumableIsActive")
                        }
                    });
                }
            }

            return testTypes.Values;
        }

        public Task CreateTestTypeConsumable(int testTypeId, int consumableId, int quantityRequired) =>
            _db.SaveData("sp_TestTypeConsumable_Create", new
            {
                TestTypeID = testTypeId,
                ConsumableID = consumableId,
                QuantityRequired = quantityRequired
            });

        public Task UpdateTestTypeConsumable(int testTypeConsumableId, int testTypeId, int consumableId, int quantityRequired) =>
            _db.SaveData("sp_TestTypeConsumable_Update", new
            {
                TestTypeConsumableID = testTypeConsumableId,
                TestTypeID = testTypeId,
                ConsumableID = consumableId,
                QuantityRequired = quantityRequired
            });

        public Task DeleteTestTypeConsumable(int testTypeConsumableId, int testTypeId) =>
            _db.SaveData("sp_TestTypeConsumable_Delete", new
            {
                TestTypeConsumableID = testTypeConsumableId,
                TestTypeID = testTypeId
            });

        //// Delete one
        //public async Task DeleteTestTypeConsumable(int testTypeConsumableId)
        //{
        //    await _db.SaveData(
        //        "sp_TestTypeConsumable_Delete",
        //        new
        //        {
        //            TestTypeConsumableID = testTypeConsumableId
        //        });
        //}

        ////delete all for the test type
        //public async Task DeleteTestTypeConsumables(int testTypeId)
        //{
        //    await _db.SaveData(
        //        "sp_TestTypeConsumable_DeleteByTestType",
        //        new
        //        {
        //            TestTypeID = testTypeId
        //        });
        //}



        // ==================== CATEGORIES ====================

        // ------------------- DISPLAY CATEGORIES

        public async Task<IEnumerable<TestCategory>> GetTestCategories(
            string filter = "Active",
            string search = "")
        {
            var categories = await _db.GetData<TestCategory, object>(
                "sp_TestCategory_GetAll",
                new
                {
                    Filter = filter,
                    Search = search
                });

            return categories.OrderBy(category => category.CategoryName ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

        // ------------------- GET CATEGOTRY BY ID -----------
        public async Task<TestCategory?> GetTestCategoryById(int id)
        {
            var result = await _db.GetData<TestCategory, object>(
                "sp_TestCategory_GetById",
                new
                {
                    TestCategoryID = id
                });

            return result.FirstOrDefault();
        }

        // --------------------- CREATE CATEGORY ID ----------
        public async Task CreateTestCategory(TestCategory category)
        {
            await _db.SaveData(
                "sp_TestCategory_Create",
                new
                {
                    category.CategoryName,
                    category.CategoryDescription
                });
        }

        // -------------------- UPDATE ------------------------
        public async Task UpdateTestCategory(TestCategory category)
        {
            await _db.SaveData(
                "sp_TestCategory_Update",
                new
                {
                    category.TestCategoryId,
                    category.CategoryName,
                    category.CategoryDescription
                });
        }

        // --------------------- DELETE TEST CATEGORY -----------
        public async Task DeleteTestCategory(int id)
        {
            await _db.SaveData(
                "sp_TestCategory_Delete",
                new
                {
                    TestCategoryID = id
                });
        }

        // ------------------ REACTIVATE ---------------------------
        public async Task ReactivateTestCategory(int id)
        {
            await _db.SaveData(
                "sp_TestCategory_Reactivate",
                new
                {
                    TestCategoryID = id
                });
        }




        // ==================== SAMPLE TYPES ====================
        //----------------------- DISPLAY -----------------------
        public async Task<IEnumerable<SampleType>> GetSampleTypes(
            string filter = "Active",
            string search = "")
        {
            var sampleTypes = await _db.GetData<SampleType, object>(
                "sp_SampleType_GetAll",
                new
                {
                    Filter = filter,
                    Search = search
                });

            return sampleTypes.OrderBy(sampleType => sampleType.SampleTypeName ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        }

        //-------------------- GET BY ID ---------------------------------
        public async Task<SampleType?> GetSampleTypeById(int id)
        {
            var result = await _db.GetData<SampleType, object>(
                "sp_SampleType_GetById",
                new
                {
                    SampleTypeID = id
                });

            return result.FirstOrDefault();
        }

        // ----------------- CREATE ------------------------
        public async Task CreateSampleType(SampleType sampleType)
        {
            await _db.SaveData(
                "sp_SampleType_Create",
                new
                {
                    sampleType.SampleTypeName
                });
        }

        // --------------------- UPDATE -------------------------
        public async Task UpdateSampleType(SampleType sampleType)
        {
            await _db.SaveData(
                "sp_SampleType_Update",
                new
                {
                    sampleType.SampleTypeId,
                    sampleType.SampleTypeName
                });
        }

        // -------------------- DELETE ---------------------
        public async Task DeleteSampleType(int id)
        {
            await _db.SaveData(
                "sp_SampleType_Delete",
                new
                {
                    SampleTypeID = id
                });
        }
        // -------------------- REACTIVATE -----------------
        public async Task ReactivateSampleType(int id)
        {
            await _db.SaveData(
                "sp_SampleType_Reactivate",
                new
                {
                    SampleTypeID = id
                });
        }

        public Task<IEnumerable<TestsPerformedByCategory>> GetTestsPerformedByCategory(
            DateTime startDate,
            DateTime endDateExclusive) =>
            _db.GetData<TestsPerformedByCategory, object>(
                "sp_ManagerReport_TestsPerformedByCategory",
                new
                {
                    StartDate = startDate,
                    EndDateExclusive = endDateExclusive
                });

        public Task<IEnumerable<TestsPerformedDetail>> GetTestsPerformedDetails(
            DateTime startDate,
            DateTime endDateExclusive) =>
            _db.GetData<TestsPerformedDetail, object>(
                "sp_ManagerReport_TestsPerformedDetail",
                new { StartDate = startDate, EndDateExclusive = endDateExclusive });
}
}
