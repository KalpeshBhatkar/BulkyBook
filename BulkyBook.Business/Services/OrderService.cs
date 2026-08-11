using BulkyBook.Business.Services.IServices;
using BulkyBook.DataAccess.Data;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BulkyBook.Business.Services
{
    public class OrderService : IOrderService
    {
        private readonly ApplicationDbContext _context;

        public OrderService(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<OrderHeader> CreateOrderAsync(OrderHeader orderHeader)
        {
            _context.OrderHeaders.Add(orderHeader);
            await _context.SaveChangesAsync();

            return orderHeader;
        }
        public async Task<IEnumerable<OrderHeader>> GetAllOrderAsync(string? userId = null, string? status = null, bool includeUser = false, bool includeDetails = false)
        {
            var query = _context.OrderHeaders.AsQueryable();

            if (includeUser)
            {
                query = query.Include(o => o.ApplicationUser);
            }
            if (includeDetails)
            {
                query = query.Include(o => o.OrderDetails).ThenInclude(od => od.Product);
            }
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(u => u.ApplicationUserId == userId);
            }
            if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
            {
                if (status.ToLower() == "cancelled")
                {
                    query = query.Where(u => u.OrderStatus == SD.StatusCancelled || u.OrderStatus == SD.StatusRefunded);
                }
                else
                {
                    query = query.Where(u => u.OrderStatus.ToLower() == status.ToLower());
                }
            }
            return await query.ToListAsync();
        }
        public async Task<OrderHeader?> GetOrderByIdAsync(int id, bool includeUser = false, bool includeDetails = false)
        {
            var query = _context.OrderHeaders.AsQueryable();

            if (includeUser)
            {
                query = query.Include(o => o.ApplicationUser);
            }
            if (includeDetails)
            {
                query = query.Include(o => o.OrderDetails).ThenInclude(od => od.Product);
            }
            return await query.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task UpdateOrderAsync(OrderHeader orderHeader)
        {
            _context.OrderHeaders.Update(orderHeader);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateOrderStatusAsync(int id, string orderStatus, string? carrier = null, string? trackingNumber = null)
        {
            var orderHeader = await _context.OrderHeaders.FindAsync(id);
            if (orderHeader == null)
            {
                throw new KeyNotFoundException($"Order {id} not found");
            }
            orderHeader.OrderStatus = orderStatus;
            if (orderStatus == SD.StatusShipped)
            {
                orderHeader.ShippingDate = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(carrier))
                {
                    orderHeader.Carrier = carrier;
                }
                if (!string.IsNullOrEmpty(trackingNumber))
                {
                    orderHeader.TrackingNumber = trackingNumber;
                }
            }
            await _context.SaveChangesAsync();
        }

        public async Task UpdateStripePaymentAsync(int orderId, string sessionId, string paymentIntentId)
        {
            var order = await _context.OrderHeaders.FindAsync(orderId);
            if (order == null)
            {
                throw new KeyNotFoundException($"Order {orderId} not found");
            }

            if (!string.IsNullOrEmpty(sessionId))
            {
                order.SessionId = sessionId;
            }

            if (!string.IsNullOrEmpty(paymentIntentId))
            {
                order.PaymentIntentId = paymentIntentId;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> CancelOrderWithRefundAsync(int orderId)
        {
            var order = await _context.OrderHeaders.FindAsync(orderId);
            if (order == null)
            {
                throw new KeyNotFoundException($"Order {orderId} not found");
            }

            if (order.OrderStatus == SD.StatusShipped)
            {
                throw new InvalidOperationException("Cannot cancel orders that have already been shipped. Customer must initiate a return instead.");
            }

            // Check if already cancelled or refunded
            if (order.OrderStatus == SD.StatusCancelled || order.OrderStatus == SD.StatusRefunded)
            {
                throw new InvalidOperationException("This order has already been cancelled.");
            }

            bool refundIssued = false;
            if (!string.IsNullOrEmpty(order.PaymentIntentId) && (order.OrderStatus == SD.StatusApproved || order.OrderStatus == SD.StatusInProcess))
            {
                try
                {
                    //refund
                    var options = new RefundCreateOptions
                    {
                        PaymentIntent = order.PaymentIntentId,
                        Reason = RefundReasons.RequestedByCustomer
                    };
                    var service = new RefundService();
                    Refund refund = service.Create(options);

                    if (refund.Status == "succeeded" || refund.Status == "pending")
                    {
                        refundIssued = true;
                        order.OrderStatus = SD.StatusRefunded;
                    }
                }
                catch (StripeException ex)
                {
                    order.OrderStatus = SD.StatusCancelled;
                    await _context.SaveChangesAsync();
                    throw new InvalidOperationException($"Stripe refund failed: {ex.Message}. Order has been cancelled, but refund must be processed manually.", ex);

                }

            }
            else
            {
                order.OrderStatus = SD.StatusCancelled;
            }

            await _context.SaveChangesAsync();
            return refundIssued;
        }

        public async Task<string> CreateStripeCheckoutSessionAsync(OrderHeader orderHeader, IEnumerable<ShoppingCart> cartItems, string domain)
        {
            if (orderHeader == null)
            {
                throw new ArgumentNullException(nameof(orderHeader));
            }
            if (cartItems == null || !cartItems.Any())
            {
                throw new ArgumentNullException(nameof(cartItems));
            }

            var options = new Stripe.Checkout.SessionCreateOptions
            {
                SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={orderHeader.Id}",
                CancelUrl = domain + "customer/cart/index",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                Metadata = new Dictionary<string, string>
                        {
                            { "OrderId", orderHeader.Id.ToString() }
                        }
            };

            foreach (var item in cartItems)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100),
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.Count,
                };
                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);
            await UpdateStripePaymentAsync(orderHeader.Id, session.Id, session.PaymentIntentId);
            return session.Url;
        }
        public async Task<bool> VerifyStripePaymentAsync(OrderHeader orderHeader)
        {
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);
            if (session.PaymentStatus.ToLower() == "paid")
            {
                await UpdateStripePaymentAsync(orderHeader.Id, session.Id, session.PaymentIntentId);
                await UpdateOrderStatusAsync(orderHeader.Id, SD.StatusApproved);
                //TempData["success"] = "Payment completed successfully! Your order has been confirmed.";
                return true;
            }
            else
            {
                //TempData["warning"] = "Payment status is pending. Please contact support if you completed the payment.";
                return false;
            }
        }
    }
}
