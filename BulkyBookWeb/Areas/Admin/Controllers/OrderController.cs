using BulkyBook.Business.Services.IServices;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.RoleAdmin)]
    public class OrderController : Controller
    {
        private readonly IOrderService _orderService;

        [BindProperty]
        public OrderHeader OrderHeader { get; set; }
        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            return View();
        }

        [AllowAnonymous]
        public async Task<IActionResult> Details(int orderId)
        {
            OrderHeader = await _orderService.GetOrderByIdAsync(orderId, includeDetails: true, includeUser: true);
            return View(OrderHeader);
        }

        [HttpPost]
        [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
        public async Task<IActionResult> UpdateOrderDetails()
        {
            var orderHeaderFromDb = await _orderService.GetOrderByIdAsync(OrderHeader.Id, includeDetails: false, includeUser: false);
            orderHeaderFromDb.Name = OrderHeader.Name;
            orderHeaderFromDb.PhoneNumber = OrderHeader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderHeader.StreetAddress;
            orderHeaderFromDb.City = OrderHeader.City;
            orderHeaderFromDb.State = OrderHeader.State;
            orderHeaderFromDb.PostalCode = OrderHeader.PostalCode;
            if (!string.IsNullOrEmpty(OrderHeader.Carrier) && orderHeaderFromDb.OrderStatus == SD.StatusShipped)
            {
                orderHeaderFromDb.Carrier = OrderHeader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderHeader.TrackingNumber) && orderHeaderFromDb.OrderStatus == SD.StatusShipped)
            {
                orderHeaderFromDb.TrackingNumber = OrderHeader.TrackingNumber;
            }
            await _orderService.UpdateOrderAsync(orderHeaderFromDb);
            TempData["Success"] = "Order Details Updated Successfully.";

            return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.RoleAdmin + "," + SD.RoleEmployee)]
        public async Task<IActionResult> UpdateOrderStatus(string status)
        {
            var orderHeader = await _orderService.GetOrderByIdAsync(OrderHeader.Id, includeDetails: false, includeUser: false);
            if (orderHeader == null)
            {
                TempData["error"] = "Order not found.";
                return RedirectToAction(nameof(Index));
            }
            string successMessage;

            switch (status)
            {
                case SD.StatusInProcess:
                    await _orderService.UpdateOrderStatusAsync(orderHeader.Id, status);
                    successMessage = "Order Status Updated Successfully.";
                    break;
                case SD.StatusCancelled:
                case SD.StatusRefunded:
                    try
                    {
                        bool refundIssued = await _orderService.CancelOrderWithRefundAsync(OrderHeader.Id);
                        if (refundIssued)
                        {
                            successMessage = "Order cancelled and refund issued successfully. Funds will be returned to customer within 5-10 business days.";
                        }
                        else
                        {
                            successMessage = "Order cancelled successfully. (No payment was processed)";
                        }
                    }
                    catch (InvalidOperationException ex)
                    {
                        // Business rule violation (e.g., trying to cancel shipped order)
                        TempData["error"] = ex.Message;
                        return RedirectToAction(nameof(Details), new { orderId = OrderHeader.Id });
                    }
                    catch (Stripe.StripeException ex)
                    {
                        // Refund failed - order is still cancelled but admin needs to manually refund
                        TempData["error"] = $"Order cancelled but refund failed: {ex.Message}. Please process refund manually in Stripe Dashboard.";
                        return RedirectToAction(nameof(Details), new { orderId = OrderHeader.Id });
                    }
                    break;
                case SD.StatusShipped:
                    if(string.IsNullOrEmpty(OrderHeader.Carrier) || string.IsNullOrEmpty(OrderHeader.TrackingNumber))
                    {
                        TempData["error"] = "Carrier and Tracking Number are required to ship the order.";
                        return RedirectToAction(nameof(Details), new { orderId = orderHeader.Id });
                    }
                    await _orderService.UpdateOrderStatusAsync(orderHeader.Id, status, orderHeader.Carrier, orderHeader.TrackingNumber);
                    successMessage = "Order shipped Successfully.";
                    break;
                default:
                    successMessage = "Invalid Order Status.";
                    break;
            }

            TempData["Success"] = successMessage;

            return RedirectToAction(nameof(Details), new { orderId = orderHeader.Id });
        }
        #region API Calls
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll(string status)
        {
            string? userId = null;
            if (!User.IsInRole(SD.RoleAdmin) && !User.IsInRole(SD.RoleEmployee))
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Unauthorized();
                }
            }
            var orders = await _orderService.GetAllOrderAsync(userId, status);
            return Json(new { data = orders });
        }

        #endregion
    }
}
