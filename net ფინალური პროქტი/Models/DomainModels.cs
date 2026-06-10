using System.ComponentModel.DataAnnotations;

namespace LoanManagement.Models
{
    public enum LoanStatus
    {
        Pending, Approved, Rejected, Closed, Overdue
    }

    public class Customer
    {
        public int Id { get; set; }
        [Required] public string FirstName { get; set; }
        [Required] public string LastName { get; set; }
        [Required] public string PersonalNumber { get; set; }
        public DateTime BirthDate { get; set; }
        public int CreditScore { get; set; }
        public ICollection<Loan> Loans { get; set; } = new List<Loan>();
    }

    public class Loan
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public Customer Customer { get; set; }
        [Range(500, 50000)] public decimal Amount { get; set; }
        public decimal InterestRate { get; set; }
        [Range(6, 60)] public int TermMonths { get; set; }
        public decimal MonthlyPayment { get; set; }
        public LoanStatus Status { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<LoanSchedule> Schedules { get; set; } = new List<LoanSchedule>();
    }

    public class Payment
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public Loan Loan { get; set; }
        [Range(0.01, (double)decimal.MaxValue)] public decimal Amount { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    }

    public class LoanSchedule
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public Loan Loan { get; set; }
        public decimal PMT { get; set; }
        public DateTime Date { get; set; }
    }
}

namespace LoanManagement.DTOs
{
    public class CreateApplicationDto
    {
        public int CustomerId { get; set; }
        public decimal Amount { get; set; }
        public decimal InterestRate { get; set; }
        public int TermMonths { get; set; }
    }

    public class MakePaymentDto
    {
        public int LoanId { get; set; }
        public decimal Amount { get; set; }
    }
}