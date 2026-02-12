using Microsoft.EntityFrameworkCore;
using School.API.Data;
using School.API.Data.DBModels.HR;
using School.API.DTOs;
using School.API.Repositories.Interfaces.HR;

namespace School.API.Repositories.Implementations.HR
{
    public class SalaryRepository : ISalaryRepository
    {
        private readonly SchoolDbContext _context;

        public SalaryRepository(SchoolDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<SalaryListDto>> GetAllAsync()
        {
            return await _context.Salaries
                .Include(s => s.Employee)
                    .ThenInclude(e => e!.Designation)
                .OrderByDescending(s => s.Year)
                .ThenByDescending(s => s.Month)
                .ThenBy(s => s.Employee!.FirstName)
                .Select(s => new SalaryListDto
                {
                    Id = s.Id,
                    EmployeeName = s.Employee != null ? s.Employee.FirstName + " " + s.Employee.LastName : null,
                    EmployeeCode = s.Employee != null ? s.Employee.EmployeeCode : null,
                    DesignationName = s.Employee != null && s.Employee.Designation != null ? s.Employee.Designation.Name : null,
                    MonthYear = $"{GetMonthName(s.Month)} {s.Year}",
                    BasicSalary = s.BasicSalary,
                    GrossSalary = s.BasicSalary + s.HouseRentAllowance + s.MedicalAllowance + s.TransportAllowance + s.OtherAllowances + s.Overtime + s.Bonus,
                    TotalDeductions = s.TaxDeduction + s.ProvidentFund + s.LoanDeduction + s.OtherDeductions,
                    NetSalary = s.NetSalary,
                    Status = s.Status,
                    StatusName = s.Status.ToString()
                })
                .ToListAsync();
        }

        public async Task<SalaryDto?> GetByIdAsync(int id)
        {
            return await _context.Salaries
                .Include(s => s.Employee)
                    .ThenInclude(e => e!.Designation)
                .Where(s => s.Id == id)
                .Select(s => new SalaryDto
                {
                    Id = s.Id,
                    EmployeeId = s.EmployeeId,
                    EmployeeName = s.Employee != null ? s.Employee.FirstName + " " + s.Employee.LastName : null,
                    EmployeeCode = s.Employee != null ? s.Employee.EmployeeCode : null,
                    DesignationName = s.Employee != null && s.Employee.Designation != null ? s.Employee.Designation.Name : null,
                    Month = s.Month,
                    Year = s.Year,
                    BasicSalary = s.BasicSalary,
                    HouseRentAllowance = s.HouseRentAllowance,
                    MedicalAllowance = s.MedicalAllowance,
                    TransportAllowance = s.TransportAllowance,
                    OtherAllowances = s.OtherAllowances,
                    TaxDeduction = s.TaxDeduction,
                    ProvidentFund = s.ProvidentFund,
                    LoanDeduction = s.LoanDeduction,
                    OtherDeductions = s.OtherDeductions,
                    Overtime = s.Overtime,
                    Bonus = s.Bonus,
                    TotalAllowances = s.HouseRentAllowance + s.MedicalAllowance + s.TransportAllowance + s.OtherAllowances,
                    TotalDeductions = s.TaxDeduction + s.ProvidentFund + s.LoanDeduction + s.OtherDeductions,
                    GrossSalary = s.BasicSalary + s.HouseRentAllowance + s.MedicalAllowance + s.TransportAllowance + s.OtherAllowances + s.Overtime + s.Bonus,
                    NetSalary = s.NetSalary,
                    Status = s.Status,
                    PaymentDate = s.PaymentDate,
                    PaymentReference = s.PaymentReference,
                    Remarks = s.Remarks,
                    WorkingDays = s.WorkingDays,
                    PresentDays = s.PresentDays,
                    AbsentDays = s.AbsentDays,
                    LeaveDays = s.LeaveDays,
                    CreatedAt = s.CreatedAt
                })
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<SalaryListDto>> GetByMonthYearAsync(int month, int year)
        {
            return await _context.Salaries
                .Include(s => s.Employee)
                    .ThenInclude(e => e!.Designation)
                .Where(s => s.Month == month && s.Year == year)
                .OrderBy(s => s.Employee!.FirstName)
                .Select(s => new SalaryListDto
                {
                    Id = s.Id,
                    EmployeeName = s.Employee != null ? s.Employee.FirstName + " " + s.Employee.LastName : null,
                    EmployeeCode = s.Employee != null ? s.Employee.EmployeeCode : null,
                    DesignationName = s.Employee != null && s.Employee.Designation != null ? s.Employee.Designation.Name : null,
                    MonthYear = $"{GetMonthName(s.Month)} {s.Year}",
                    BasicSalary = s.BasicSalary,
                    GrossSalary = s.BasicSalary + s.HouseRentAllowance + s.MedicalAllowance + s.TransportAllowance + s.OtherAllowances + s.Overtime + s.Bonus,
                    TotalDeductions = s.TaxDeduction + s.ProvidentFund + s.LoanDeduction + s.OtherDeductions,
                    NetSalary = s.NetSalary,
                    Status = s.Status,
                    StatusName = s.Status.ToString()
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<SalaryListDto>> GetByEmployeeAsync(int employeeId)
        {
            return await _context.Salaries
                .Include(s => s.Employee)
                    .ThenInclude(e => e!.Designation)
                .Where(s => s.EmployeeId == employeeId)
                .OrderByDescending(s => s.Year)
                .ThenByDescending(s => s.Month)
                .Select(s => new SalaryListDto
                {
                    Id = s.Id,
                    EmployeeName = s.Employee != null ? s.Employee.FirstName + " " + s.Employee.LastName : null,
                    EmployeeCode = s.Employee != null ? s.Employee.EmployeeCode : null,
                    DesignationName = s.Employee != null && s.Employee.Designation != null ? s.Employee.Designation.Name : null,
                    MonthYear = $"{GetMonthName(s.Month)} {s.Year}",
                    BasicSalary = s.BasicSalary,
                    GrossSalary = s.BasicSalary + s.HouseRentAllowance + s.MedicalAllowance + s.TransportAllowance + s.OtherAllowances + s.Overtime + s.Bonus,
                    TotalDeductions = s.TaxDeduction + s.ProvidentFund + s.LoanDeduction + s.OtherDeductions,
                    NetSalary = s.NetSalary,
                    Status = s.Status,
                    StatusName = s.Status.ToString()
                })
                .ToListAsync();
        }

        public async Task<Salary> CreateAsync(CreateSalaryDto dto)
        {
            var totalAllowances = dto.HouseRentAllowance + dto.MedicalAllowance + dto.TransportAllowance + dto.OtherAllowances;
            var totalDeductions = dto.TaxDeduction + dto.ProvidentFund + dto.LoanDeduction + dto.OtherDeductions;
            var grossSalary = dto.BasicSalary + totalAllowances + dto.Overtime + dto.Bonus;
            var netSalary = grossSalary - totalDeductions;

            var entity = new Salary
            {
                EmployeeId = dto.EmployeeId,
                Month = dto.Month,
                Year = dto.Year,
                BasicSalary = dto.BasicSalary,
                HouseRentAllowance = dto.HouseRentAllowance,
                MedicalAllowance = dto.MedicalAllowance,
                TransportAllowance = dto.TransportAllowance,
                OtherAllowances = dto.OtherAllowances,
                TaxDeduction = dto.TaxDeduction,
                ProvidentFund = dto.ProvidentFund,
                LoanDeduction = dto.LoanDeduction,
                OtherDeductions = dto.OtherDeductions,
                Overtime = dto.Overtime,
                Bonus = dto.Bonus,
                NetSalary = netSalary,
                Status = SalaryStatus.Pending,
                Remarks = dto.Remarks,
                WorkingDays = dto.WorkingDays,
                PresentDays = dto.PresentDays,
                AbsentDays = dto.AbsentDays,
                LeaveDays = dto.LeaveDays,
                CreatedAt = DateTime.UtcNow
            };

            _context.Salaries.Add(entity);
            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<Salary?> UpdateAsync(int id, UpdateSalaryDto dto)
        {
            var entity = await _context.Salaries.FindAsync(id);
            if (entity == null)
                return null;

            var totalAllowances = dto.HouseRentAllowance + dto.MedicalAllowance + dto.TransportAllowance + dto.OtherAllowances;
            var totalDeductions = dto.TaxDeduction + dto.ProvidentFund + dto.LoanDeduction + dto.OtherDeductions;
            var grossSalary = dto.BasicSalary + totalAllowances + dto.Overtime + dto.Bonus;
            var netSalary = grossSalary - totalDeductions;

            entity.EmployeeId = dto.EmployeeId;
            entity.Month = dto.Month;
            entity.Year = dto.Year;
            entity.BasicSalary = dto.BasicSalary;
            entity.HouseRentAllowance = dto.HouseRentAllowance;
            entity.MedicalAllowance = dto.MedicalAllowance;
            entity.TransportAllowance = dto.TransportAllowance;
            entity.OtherAllowances = dto.OtherAllowances;
            entity.TaxDeduction = dto.TaxDeduction;
            entity.ProvidentFund = dto.ProvidentFund;
            entity.LoanDeduction = dto.LoanDeduction;
            entity.OtherDeductions = dto.OtherDeductions;
            entity.Overtime = dto.Overtime;
            entity.Bonus = dto.Bonus;
            entity.NetSalary = netSalary;
            entity.Status = dto.Status;
            entity.PaymentDate = dto.PaymentDate;
            entity.PaymentReference = dto.PaymentReference;
            entity.Remarks = dto.Remarks;
            entity.WorkingDays = dto.WorkingDays;
            entity.PresentDays = dto.PresentDays;
            entity.AbsentDays = dto.AbsentDays;
            entity.LeaveDays = dto.LeaveDays;
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return entity;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var entity = await _context.Salaries.FindAsync(id);
            if (entity == null)
                return false;

            _context.Salaries.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Salaries.AnyAsync(s => s.Id == id);
        }

        public async Task<bool> SalaryExistsAsync(int employeeId, int month, int year)
        {
            return await _context.Salaries
                .AnyAsync(s => s.EmployeeId == employeeId && s.Month == month && s.Year == year);
        }

        public async Task ApproveSalariesAsync(List<int> salaryIds)
        {
            var salaries = await _context.Salaries
                .Where(s => salaryIds.Contains(s.Id) && s.Status == SalaryStatus.Pending)
                .ToListAsync();

            foreach (var salary in salaries)
            {
                salary.Status = SalaryStatus.Approved;
                salary.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task PaySalaryAsync(PaySalaryDto dto)
        {
            var salary = await _context.Salaries.FindAsync(dto.SalaryId);
            if (salary == null || salary.Status != SalaryStatus.Approved)
                return;

            salary.Status = SalaryStatus.Paid;
            salary.PaymentDate = dto.PaymentDate;
            salary.PaymentReference = dto.PaymentReference;
            salary.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        private static string GetMonthName(int month)
        {
            return month switch
            {
                1 => "January", 2 => "February", 3 => "March", 4 => "April",
                5 => "May", 6 => "June", 7 => "July", 8 => "August",
                9 => "September", 10 => "October", 11 => "November", 12 => "December",
                _ => "Unknown"
            };
        }
    }
}
