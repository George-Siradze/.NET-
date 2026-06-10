using LoanManagement.Data;
using LoanManagement.DTOs;
using LoanManagement.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LoanManagement.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoansController : ControllerBase
    {
        private readonly AppDbContext _conetxt;

        public LoansController(AppDbContext context)
        {
            _conetxt = context;
        }

        [HttpPost("CreateApplication")]
        public async Task<IActionResult> CreateApplication([FromBody] CreateApplicationDto request)
        {
            var cusotmer = await _conetxt.Customers.FindAsync(request.CustomerId);
            
            if (cusotmer == null)
                return BadRequest("Customer not found.");

            var today = DateTime.Today;
            var age = today.Year - cusotmer.BirthDate.Year;
            if (cusotmer.BirthDate.Date > today.AddYears(-age)) age--;

            if (age < 18)
                return BadRequest("Customer must be at least 18 years old.");

            var newLaon = new Loan
            {
                CustomerId = request.CustomerId,
                Amount = request.Amount,
                InterestRate = request.InterestRate,
                TermMonths = request.TermMonths,
                Status = LoanStatus.Pending
            };

            if (cusotmer.CreditScore < 300)
            {
                newLaon.Status = LoanStatus.Rejected;
                newLaon.MonthlyPayment = 0;
            }
            else
            {
                newLaon.Status = LoanStatus.Approved;
                
                double p = (double)request.Amount;
                double mnthlyRate = (double)(request.InterestRate / 100 / 12);
                double mathPwr = Math.Pow(1 + mnthlyRate, request.TermMonths);
                
                decimal pmtAmount = (decimal)(p * (mnthlyRate * mathPwr / (mathPwr - 1)));
                newLaon.MonthlyPayment = Math.Round(pmtAmount, 2);

                var currentDate = DateTime.UtcNow;
                for (int i = 1; i <= request.TermMonths; i++)
                {
                    newLaon.Schedules.Add(new LoanSchedule
                    {
                        PMT = newLaon.MonthlyPayment,
                        Date = currentDate.AddMonths(i)
                    });
                }
            }

            _conetxt.Loans.Add(newLaon);
            await _conetxt.SaveChangesAsync();

            return Ok(new {
                Id = newLaon.Id,
                Status = newLaon.Status.ToString(),
                MonthlyPayment = newLaon.MonthlyPayment
            });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetLoanStatus(int id)
        {
            var laon = await _conetxt.Loans.FindAsync(id);
            if (laon == null) return NotFound();

            return Ok(new {
                laon.Id,
                Status = laon.Status.ToString(),
                laon.MonthlyPayment,
                laon.Amount
            });
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly AppDbContext _conetxt;

        public PaymentsController(AppDbContext context)
        {
            _conetxt = context;
        }

        [HttpPost]
        public async Task<IActionResult> MakePayment([FromBody] MakePaymentDto request)
        {
            var laon = await _conetxt.Loans.FindAsync(request.LoanId);
            
            if (laon == null)
                return BadRequest("Loan not found");

            if (laon.Status == LoanStatus.Closed || laon.Status == LoanStatus.Rejected)
                return BadRequest("Cannot make payment on closed or rejected loan.");

            var paymnt = new Payment
            {
                LoanId = request.LoanId,
                Amount = request.Amount,
                PaymentDate = DateTime.UtcNow
            };

            _conetxt.Payments.Add(paymnt);
            
            var totalPaid = await _conetxt.Payments
                .Where(p => p.LoanId == request.LoanId)
                .SumAsync(p => p.Amount);
                
            var totalRequired = laon.MonthlyPayment * laon.TermMonths;

            if (totalPaid + request.Amount >= totalRequired)
            {
                laon.Status = LoanStatus.Closed;
            }

            await _conetxt.SaveChangesAsync();
            return Ok("Payment processed successfully");
        }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase
    {
        private readonly AppDbContext _conetxt;

        public CustomersController(AppDbContext context)
        {
            _conetxt = context;
        }

        [HttpGet("loans")]
        public async Task<IActionResult> GetCustomerLoans([FromQuery] int customerId)
        {
            var loans = await _conetxt.Loans
                .Where(l => l.CustomerId == customerId)
                .ToListAsync();
                
            return Ok(loans);
        }
    }
}