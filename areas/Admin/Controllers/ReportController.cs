using System.Linq;
using System.Web.Mvc;
using System.Data.Entity;
using PhoneStore_New.Areas.Admin.Models.ViewModels;
using PhoneStore_New.Models;
using System;
using System.Globalization;

namespace PhoneStore_New.Areas.Admin.Controllers
{
    [Authorize(Roles = "admin")]
    public class ReportController : Controller
    {
        private readonly PhoneStoreDBEntities db = new PhoneStoreDBEntities();

        public ActionResult Sales()
        {
            var reportData = db.OrderItems
                .Where(oi => oi.Order.Status == "completed")
                .GroupBy(oi => DbFunctions.TruncateTime(oi.Order.OrderDate))
                .Select(g => new
                {
                    OrderDay = g.Key.Value,
                    DailyRevenue = g.Sum(oi => oi.PriceAtOrder * oi.Quantity),
                    DailyCost = g.Sum(oi => (oi.Product.PurchasePrice ?? 0) * oi.Quantity)
                })
                .OrderByDescending(r => r.OrderDay)
                .ToList()
                .Select(r => new SalesReportItemViewModel
                {
                    OrderDay = r.OrderDay,
                    DailyRevenue = r.DailyRevenue,
                    DailyProfit = r.DailyRevenue - r.DailyCost
                })
                .ToList();

            var viewModel = new SalesReportViewModel
            {
                ReportItems = reportData,
                TotalRevenue = reportData.Sum(r => r.DailyRevenue),
                TotalProfit = reportData.Sum(r => r.DailyProfit)
            };

            return View(viewModel);
        }

        public ActionResult Bestsellers()
        {
            var bestsellers = db.OrderItems
                                .Include(oi => oi.Product)
                                .GroupBy(oi => oi.ProductId)
                                .Select(g => new BestsellerItemViewModel
                                {
                                    ProductId = g.Key,
                                    ProductName = g.Max(oi => oi.Product.Name),
                                    TotalSold = g.Sum(oi => oi.Quantity)
                                })
                                .OrderByDescending(r => r.TotalSold)
                                .Take(10)
                                .ToList();

            var viewModel = new BestsellerReportViewModel { Bestsellers = bestsellers };
            return View(viewModel);
        }

        // === SỬA ĐỔI QUAN TRỌNG: Sửa lại logic phân tích ngày tháng ===
        public ActionResult SalesDetail(string date)
        {
            DateTime reportDate;
            // Cố gắng chuyển đổi chuỗi ngày tháng theo định dạng chuẩn "yyyy-MM-dd"
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out reportDate))
            {
                TempData["Message"] = "Lỗi: Định dạng ngày không hợp lệ.";
                return RedirectToAction("Sales");
            }

            var orders = db.Orders
                            .Include(o => o.User)
                            .Where(o => o.Status == "completed" && DbFunctions.TruncateTime(o.OrderDate) == reportDate.Date)
                            .Select(o => new OrderAdminViewModel
                            {
                                OrderId = o.OrderId,
                                OrderDate = o.OrderDate,
                                TotalAmount = o.TotalAmount,
                                Status = o.Status,
                                CustomerName = o.User.FirstName + " " + o.User.LastName,
                                Username = o.User.Username
                            })
                            .ToList();

            ViewBag.ReportDate = reportDate.ToString("dd/MM/yyyy");
            return View(orders);
        }
    }
}