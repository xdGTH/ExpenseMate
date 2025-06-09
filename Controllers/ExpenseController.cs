using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ExpenseMate.Models;
using Microsoft.EntityFrameworkCore;
using ExpenseMate.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Globalization;

namespace ExpenseMate.Controllers;

[Authorize]
[Route("/[controller]")]
public class ExpenseController : Controller
{
    private readonly ApplicationDbContext _db;
    public ExpenseController(ApplicationDbContext applicationDbContext)
    {
        _db = applicationDbContext;
    }

    [Route("/[controller]/[action]")]
    [HttpGet]
    public async Task<IActionResult> Index(string categoryFilter, DateTime? fromDate, DateTime? toDate,
        string sortBy = nameof(Expense.Amount), string sortOrder = "ASC")
    {
        if (User.FindFirst(ClaimTypes.NameIdentifier).Value == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        string userID = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        List<Expense> allExpense = await _db.Expenses.ToListAsync();

        List<Expense> sortedExpense = (sortBy, sortOrder) switch
        {
            (nameof(Expense.Amount), "ASC") => allExpense.OrderBy(temp => temp.Amount).ToList(),
            (nameof(Expense.Amount), "DESC") => allExpense.OrderByDescending(temp => temp.Amount).ToList(),

            (nameof(Expense.Date), "ASC") => allExpense.OrderBy(temp => temp.Date).ToList(),
            (nameof(Expense.Date), "DESC") => allExpense.OrderByDescending(temp => temp.Date).ToList(),
            _ => allExpense
        };


        ViewBag.Title = "Monthly Summary";
        ViewBag.Categories = (await _db.Expenses
            .Where(e => !string.IsNullOrEmpty(e.Category))
            .Select(e => e.Category)
            .Distinct()
            .ToListAsync())
            .Select(category => new SelectListItem
            {
                Text = category,
                Value = category
            }).ToList();

        ViewBag.Category = categoryFilter;
        ViewBag.fromDate = fromDate;
        ViewBag.toDate = toDate;
        ViewBag.sortOrder = sortOrder;
        ViewBag.sortBy = sortBy;

        if (string.IsNullOrEmpty(categoryFilter))
            return View(sortedExpense.OrderBy(temp => temp.Amount).ToList());

        sortedExpense = sortedExpense.Where(temp => (!string.IsNullOrEmpty(temp.Category)) ?
        temp.Category.Equals(categoryFilter, StringComparison.OrdinalIgnoreCase) : true
        && temp.UserID == userID)
            .OrderByDescending(temp => temp.Date)
            .ToList();

        if (fromDate.HasValue)
            sortedExpense = sortedExpense.Where(temp => temp.Date >= fromDate).ToList();

        if (toDate.HasValue)
            sortedExpense = sortedExpense.Where(temp => temp.Date <= toDate).ToList();

        ViewBag.Total = sortedExpense.Sum(temp => temp.Amount);
        return View(sortedExpense);
    }

    [Route("/expense/[action]")]
    public async Task<IActionResult> Summary(string sortBy = nameof(Expense.Amount), string sortOrder = "ASC")
    {
        if (User.FindFirst(ClaimTypes.NameIdentifier).Value == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        var currentMonth = DateTime.Now.Month;
        var currentYear = DateTime.Now.Year;

        ViewBag.sortOrder = sortOrder;
        ViewBag.sortBy = sortBy;

        string userID = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var monthlyExpenses = await _db.Expenses
            .Where(temp => temp.Date.Month == currentMonth && temp.Date.Year == currentYear && temp.UserID == userID)
            .Distinct()
            .ToListAsync();

        var total = monthlyExpenses.Sum(e => e.Amount);

        var grouped = monthlyExpenses
            .GroupBy(e => e.Category)
            .Select(g => new
            {
                Category = g.Key,
                Total = g.Sum(e => e.Amount)
            }).ToList();

        ViewBag.Total = total;
        ViewBag.Categories = grouped.Select(g => new SelectListItem
        {
            Text = g.Category,
            Value = g.Category
        }).ToList();
        ViewBag.Amounts = grouped.Select(g => g.Total).ToList();

        List<Expense> sortedExpense = (sortBy, sortOrder) switch
        {
            (nameof(Expense.Amount), "ASC") => monthlyExpenses.OrderBy(temp => temp.Amount).ToList(),

            (nameof(Expense.Amount), "DESC") => monthlyExpenses.OrderByDescending(temp => temp.Amount).ToList(),

            (nameof(Expense.Date), "ASC") => monthlyExpenses.OrderBy(temp => temp.Date).ToList(),

            (nameof(Expense.Date), "DESC") => monthlyExpenses.OrderByDescending(temp => temp.Date).ToList(),

            _ => monthlyExpenses
        };

        return View(sortedExpense);
    }

    [Route("/[controller]/[action]")]
    public async Task<IActionResult> ExportToCsv(string categoryFilter, DateTime? fromDate, DateTime? toDate)
    {
        if (User.FindFirst(ClaimTypes.NameIdentifier).Value == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        string userID = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

        var expenses = from e in _db.Expenses select e;

        if (!string.IsNullOrEmpty(categoryFilter))
            expenses = expenses.Where(e => e.Category == categoryFilter);

        if (fromDate.HasValue)
            expenses = expenses.Where(e => e.Date >= fromDate.Value);

        if (toDate.HasValue)
            expenses = expenses.Where(e => e.Date <= toDate.Value);

        var data = await expenses.OrderBy(e => e.Amount)
            .Where(temp => temp.UserID == userID)
            .ToListAsync();

        var csv = new StringBuilder();
        csv.AppendLine("Title,Amount,Category,Date");

        foreach (var item in data)
        {
            csv.AppendLine($"\"{item.Title}\",\"{item.Amount}\",\"{item.Category}\",\"{item.Date:yyyy-MM-dd}\"");
        }

        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "expenses.csv");
    }

    [HttpGet]
    [Route("/[controller]/[action]")]
    public IActionResult WriteExpenses() => View();

    [HttpPost]
    [Route("/[controller]/[action]")]
    public async Task<IActionResult> WriteExpenses(Expense expense)
    {
        if (expense == null)
        {
            return View();
        }
        string userID = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
        expense.UserID = userID;
        expense.Date = DateTime.Now;
        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();

        return RedirectToAction("Summary");
    }
}
