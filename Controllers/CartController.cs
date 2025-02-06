using Mailo.Data;
using Mailo.Data.Enums;
using Mailo.IRepo;
using Mailo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;

namespace Mailo.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartRepo _order;
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _db;

        public CartController(ICartRepo order, IUnitOfWork unitOfWork, AppDbContext db)
        {
            _order = order;
            _unitOfWork = unitOfWork;
            _db = db;
        }


        public async Task<IActionResult> Index()
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity?.Name);
            if (user == null) return RedirectToAction("Login", "Account");

            Order? cart = await _order.GetOrCreateCart(user);
            if (cart == null || cart.OrderProducts == null) return View();
            if (cart.OrderProducts == null)
            {
                cart.OrderProducts = new List<OrderProduct>();
            }
            else
            {
                var orderProducts = cart.OrderProducts?.AsQueryable();
                foreach(OrderProduct op in orderProducts){
                    if (op.Quantity > op.Variant.Quantity)
                    {
                        if (!string.IsNullOrEmpty(cart.PromoCodeUsed))
                        {

                            double discountAmount;
                            if (cart.PromoCode != null)
                            {
                                discountAmount = op.product.TotalPrice * (cart.PromoCode.DiscountPercentage / 100);
                            }
                            else
                            {
                                PromoCode? pc = await _db.PromoCodes.Where(pc => cart.PromoCodeUsed == pc.Code).FirstOrDefaultAsync();
                                discountAmount = op.product.TotalPrice * (pc.DiscountPercentage / 100);
                            }
                            cart.OrderPrice -= op.product.TotalPrice - ((op.Quantity- op.Variant.Quantity) * discountAmount);
                            cart.TotalPrice -= op.product.TotalPrice - ((op.Quantity - op.Variant.Quantity) * discountAmount);
                        }
                        else
                        {
                            cart.OrderPrice -= op.product.TotalPrice * (op.Quantity - op.Variant.Quantity);
                            cart.TotalPrice -= op.product.TotalPrice * (op.Quantity - op.Variant.Quantity);
                        }
                        cart.FinalPrice = cart.TotalPrice + cart.DeliveryFee;
                        _unitOfWork.orders.Update(cart);

                        op.Quantity = op.Variant.Quantity;
                        _unitOfWork.orderProducts.Update(op);
                        await _unitOfWork.CommitChangesAsync();
                            return RedirectToAction("Index");
                    }
                }
            }
            return View(cart);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity?.Name);
            var cart = await _order.GetOrCreateCart(user);
            if (cart != null)
            {
                cart.OrderProducts?.Clear();
                _unitOfWork.orders.Delete(cart);
            }
            return RedirectToAction("Index");
        }
        public IActionResult GetColorsForSize(int productId, int sizeId)
        {
            var colors = _db.ProductVariants
                .Where(v => v.ProductId == productId && v.SizeId == sizeId && v.Quantity > 0)
                .Select(v => new { colorId = v.ColorId, colorName = v.Color.ColorName })
                .Distinct()
                .ToList();

            return Json(colors);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddProduct(int productId, string color, string size, int quantity = 1)
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity?.Name);
            if (user == null)
            {
                TempData["ErrorMessage"] = "User not found. Please log in.";
                return RedirectToAction("Login", "Account");
            }
            Product? product = await _unitOfWork.products.GetByIDWithIncludes(productId, p => p.Variants);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Product not found.";
                Console.WriteLine("Product not found.");
                return BadRequest(TempData["ErrorMessage"]);
            }

            color = color.Trim().ToLower();
            size = size.Trim().ToLower();

            var variant = await _db.ProductVariants
                .Include(v => v.Color)
                .Include(v => v.Size)
                .FirstOrDefaultAsync(v => v.ProductId == productId &&
                                          v.ColorId.ToString() == color &&
                                          v.SizeId.ToString() == size);

            if (variant == null)
            {
                TempData["ErrorMessage"] = "Variant not found.";
                Console.WriteLine($"Variant not found for ProductId: {product.ID}, Color: {color}, Size: {size}"); // Debugging
                return BadRequest(TempData["ErrorMessage"]);
            }

            // Get or create the user's cart
            var cart = await _order.GetOrCreateCart(user);
            var deliveryFee = await _db.deliveries.Where(d => d.Name == user.Governorate).Select(d => d.DeliveryFee).FirstOrDefaultAsync();
            if(deliveryFee == null || deliveryFee == 0)
            {
                deliveryFee = 100;
            }
            if (cart == null)
            {
                cart = new Order
                {
                    UserID = user.ID,
                    OrderPrice = product.TotalPrice * quantity,
                    TotalPrice = product.TotalPrice * quantity,
                    FinalPrice = product.TotalPrice * quantity + deliveryFee,
                    DeliveryFee=deliveryFee,
                    OrderAddress = user.Address,
                    OrderProducts = new List<OrderProduct>()
                };
                _unitOfWork.orders.Insert(cart);
                await _unitOfWork.CommitChangesAsync();
                OrderProduct op = new OrderProduct
                {
                    ProductID = productId,
                    VariantID = variant.Id,
                    Variant=variant,
                    OrderID = cart.ID,
                    Quantity = quantity
                };
                cart.OrderProducts.Add(op);
                _db.SaveChanges();
                _unitOfWork.orders.Update(cart);

                TempData["SuccessMessage"] = "Product added to cart successfully.";
            }
            else
            {
                if (cart.OrderProducts.Any(op => op.ProductID == productId && op.VariantID == variant.Id))
                {
                    TempData["ErrorMessage"] = "Product with this variant is already in the cart.";
                    Console.WriteLine($"Product {product.ID} with variant {variant.Id} is already in the cart."); // Debugging
                    return BadRequest(TempData["ErrorMessage"]);
                }
                else{
                    OrderProduct op = new OrderProduct
                    {
                        ProductID = productId,
                        VariantID = variant.Id,
                        Variant = variant,
                        OrderID = cart.ID,
                        Quantity = quantity
                    };
                    cart.OrderProducts.Add(op);
                    _db.SaveChanges();
                    if (!string.IsNullOrEmpty(cart.PromoCodeUsed))
                {
                    double discountAmount;
                    if (cart.PromoCode != null)
                    {
                        discountAmount = product.TotalPrice * (cart.PromoCode.DiscountPercentage / 100);
                    }
                    else
                    {
                        PromoCode? pc = await _db.PromoCodes.Where(pc=>cart.PromoCodeUsed==pc.Code).FirstOrDefaultAsync();
                        discountAmount = product.TotalPrice * (pc.DiscountPercentage / 100);
                    }
                    cart.OrderPrice += product.TotalPrice * quantity*discountAmount;
                    cart.TotalPrice += product.TotalPrice * quantity*discountAmount;
                }
                else
                {

                    cart.OrderPrice += product.TotalPrice * quantity;
                    cart.TotalPrice += product.TotalPrice * quantity;
                }
                    _unitOfWork.orders.Update(cart);

                    TempData["SuccessMessage"] = "Product added to cart successfully.";
                }
            }
            // Check if the product with the same variant is already in the cart
            
            return RedirectToAction("Index", "Product");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveProduct(int productId, int variantId)
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity?.Name);
            var cart = await _order.GetOrCreateCart(user);
            if (cart == null) return View("Index");
            var orderProduct = cart.OrderProducts?.FirstOrDefault(op => op.ProductID == productId && op.VariantID == variantId);
            if (orderProduct != null)
            {
                if (cart.OrderProducts?.Count == 1)
                {
                    cart.OrderProducts.Clear();
                    _unitOfWork.orders.Delete(cart);
                    return View("Index");
                }
                var product = await _unitOfWork.products.GetByID(productId);
                
                if (!string.IsNullOrEmpty(cart.PromoCodeUsed))
                {

                    double discountAmount;
                    if (cart.PromoCode != null)
                    {
                        discountAmount = product.TotalPrice * (cart.PromoCode.DiscountPercentage / 100);
                    }
                    else
                    {
                        PromoCode? pc = await _db.PromoCodes.Where(pc => cart.PromoCodeUsed == pc.Code).FirstOrDefaultAsync();
                        discountAmount = product.TotalPrice * (pc.DiscountPercentage / 100);
                    }
                    cart.OrderPrice -= product.TotalPrice * orderProduct.Quantity - (discountAmount * orderProduct.Quantity);
                    cart.TotalPrice -= product.TotalPrice * orderProduct.Quantity - (discountAmount * orderProduct.Quantity);
                }
                else
                {
                    cart.OrderPrice -= product.TotalPrice * orderProduct.Quantity;
                    cart.TotalPrice -= product.TotalPrice * orderProduct.Quantity;
                }

                cart.OrderProducts?.Remove(orderProduct);
                _unitOfWork.orders.Update(cart);
                await _unitOfWork.CommitChangesAsync();
            }
            else
            {
                TempData["ErrorMessage"] = "Product not found";
                return BadRequest(TempData["ErrorMessage"]);
            }

            return RedirectToAction("Index");
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NewOrder()
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity?.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var existingOrderItem = await _order.GetOrCreateCart(user);

            if (existingOrderItem == null || (existingOrderItem.OrderStatus != OrderStatus.New))
            {
                TempData["ErrorMessage"] = "Cart is already ordered";
                return BadRequest(TempData["ErrorMessage"]);
            }
            if (existingOrderItem.OrderProducts != null && existingOrderItem.OrderProducts.Any())
            {
                if (existingOrderItem.PromoCodeUsed != null)
                {
                    var promo = await _db.PromoCodes.FirstOrDefaultAsync(p=>p.Code==existingOrderItem.PromoCodeUsed);
                    if (promo.ExpiryDate.Day <= DateOnly.FromDateTime(DateTime.Now).Day)
                    {
                        existingOrderItem.TotalPrice = existingOrderItem.OrderPrice;
                        existingOrderItem.PromoCodeUsed = null;
                        existingOrderItem.PromoCode = null;
                        _unitOfWork.orders.Update(existingOrderItem);
                        return RedirectToAction("Index");
                    }
                    else if (promo.DiscountPercentage != (existingOrderItem.OrderPrice - existingOrderItem.TotalPrice)/existingOrderItem.OrderPrice * 100)
                    {
                        existingOrderItem.TotalPrice = existingOrderItem.OrderPrice - existingOrderItem.OrderPrice * promo.DiscountPercentage / 100;
                        _unitOfWork.orders.Update(existingOrderItem);
                        return RedirectToAction("Index");
                    }
                    else if(promo == null)
                    {
                        existingOrderItem.TotalPrice = existingOrderItem.OrderPrice;
                        existingOrderItem.PromoCodeUsed = null;
                        existingOrderItem.PromoCode = null;
                        _unitOfWork.orders.Update(existingOrderItem);
                        return RedirectToAction("Index");
                    }
                }
                var varients = existingOrderItem.OrderProducts.Where(op => op.OrderID == existingOrderItem.ID)
                    .Select(op => op.Variant)
                    .ToList();
                var orderProducts = existingOrderItem.OrderProducts?.AsQueryable();
                foreach (OrderProduct op in orderProducts)
                {
                    if (op.Quantity > op.Variant.Quantity)
                    {
                        if (!string.IsNullOrEmpty(existingOrderItem.PromoCodeUsed))
                        {
                            double discountAmount = op.product.TotalPrice * (existingOrderItem.PromoCode.DiscountPercentage / 100);
                            existingOrderItem.OrderPrice -= op.product.TotalPrice * (op.Quantity - op.Variant.Quantity);
                            existingOrderItem.TotalPrice -= op.product.TotalPrice * (op.Quantity - op.Variant.Quantity);

                            op.Quantity = op.Variant.Quantity;
                        }
                    }
                }
                foreach (var v in varients)
                {

                    v.Quantity -= existingOrderItem.OrderProducts.FirstOrDefault(op => op.VariantID == v.Id).Quantity;
                }

                existingOrderItem.OrderStatus = OrderStatus.Pending;
                _unitOfWork.orders.Update(existingOrderItem);
                Payment payment = new Payment
                {
                    OrderId = existingOrderItem.ID,
                    UserID = user.ID,
                    TotalPrice = existingOrderItem.FinalPrice
                };
                existingOrderItem.Payment = payment;
                _unitOfWork.payments.Insert(payment);
                _unitOfWork.orders.Update(existingOrderItem);

                await SendOrderConfirmationEmail(user.Email, existingOrderItem);


                TempData["Success"] = "Cart Has Been Ordered Successfully";
                return RedirectToAction("GetOrders");
            }
            return View("Index");
        }
        public async Task<List<Order>> GetOrdersWithDetails(User user)
        {
            return await _db.Orders
      .Where(o => o.UserID == user.ID && o.OrderStatus!=OrderStatus.New && o.OrderStatus!=OrderStatus.Delivered)
      .Include(o => o.OrderProducts)
          .ThenInclude(op => op.Variant)
              .ThenInclude(v => v.Size)
      .Include(o => o.OrderProducts)
          .ThenInclude(op => op.Variant)
              .ThenInclude(v => v.Color).Include(o => o.OrderProducts)
          .ThenInclude(op => op.product)
      .ToListAsync();
        }

        [HttpGet]
        public async Task<IActionResult> GetOrders()
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity?.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = await GetOrdersWithDetails(user);

            if (orders != null && orders.Any())
            {
                return View(orders);
            }
            else
            {
                return View();
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int OrderId)
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity?.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var order = await _unitOfWork.orders.GetByIDWithIncludes(OrderId,
                order => order.employee,
                order => order.Payment,
                order => order.user,
                order => order.OrderProducts
             );
            if (order != null)
            {
                var orderProducts = await _db.OrderProducts.Include(op => op.product).Include(op => op.Variant).Where(op => op.OrderID == order.ID).ToListAsync();
                if (orderProducts != null && orderProducts.Any())
                {
                    var varients = orderProducts
                     .Select(op => op.Variant)
                     .ToList();
                    foreach (var v in varients)
                    {
                        v.Quantity += orderProducts.FirstOrDefault(op => op.VariantID == v.Id).Quantity;
                    }
                    _db.OrderProducts.RemoveRange(orderProducts);
                    _db.SaveChanges();
                }

                if (order.Payment != null)
                {
                    _unitOfWork.payments.Delete(order.Payment);
                }
                _unitOfWork.orders.Delete(order);
                return RedirectToAction("GetOrders");
            }
                return View("Index");
        }
        [HttpPost]
        public async Task<IActionResult> ApplyPromoCode(string promoCode)
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await _order.GetOrCreateCart(user);
            if (cart == null || !cart.OrderProducts.Any())
            {
                TempData["PromoMessage"] = "Your cart is empty.";
                TempData["PromoStatus"] = "Error";
                return View("Index");
            }

            if (!string.IsNullOrEmpty(cart.PromoCodeUsed))
            {
                if (cart.PromoCodeUsed == promoCode)
                {
                    TempData["PromoMessage"] = "This promo code has already been applied to your order.";
                }
                else
                {
                    TempData["PromoMessage"] = "A promo code has already been applied to this order. You cannot apply another one.";
                }
                TempData["PromoStatus"] = "Error";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(promoCode))
            {
                TempData["PromoMessage"] = "Please enter a promo code.";
                TempData["PromoStatus"] = "Error";
                return RedirectToAction("Index");
            }

            var promo = await _db.PromoCodes.FirstOrDefaultAsync(p => p.Code == promoCode && p.ExpiryDate >= DateTime.Now);
            if (promo == null)
            {
                TempData["PromoMessage"] = "Invalid or expired promo code.";
                TempData["PromoStatus"] = "Error";
                return RedirectToAction("Index");
            }

            double discountAmount = cart.OrderPrice * (promo.DiscountPercentage / 100);
            cart.TotalPrice -= discountAmount;
            cart.PromoCodeUsed = promo.Code;
            _unitOfWork.orders.Update(cart);

            TempData["PromoMessage"] = $"Promo code applied successfully! You saved {discountAmount:C}.";
            TempData["PromoStatus"] = "Success";

            return RedirectToAction("Index");
        }
        private async Task SendOrderConfirmationEmail(string email, Order order)
        {
            var subject = "Order Confirmation";
            var body = $"Thank you for your order ! </br> " +
                       $"  When the order status is shipped, you cannot cancel the order !<br/><br/>" +
                       $"<strong>Order Details:</strong><br/>";

            foreach (var item in order.OrderProducts)
            {
                var variant = await _db.ProductVariants
                    .Include(v => v.Size)
                    .Include(v => v.Color)
                    .FirstOrDefaultAsync(v => v.Id == item.VariantID);

                if (variant != null)
                {
                    body += $"<strong>Product:</strong> {item.product.Name}<br/>" +
                            $"<strong>Quantity:</strong> {item.Quantity}<br/>" +
                            $"<strong>Color:</strong> {variant.Color.ColorName}<br/>" +
                            $"<strong>Size:</strong> {variant.Size.SizeName}<br/>";
                }
            }

            body += $"<strong>Delivery Fee:</strong> {order.DeliveryFee}<br/>"+ $" <strong>Total Price:</strong> {order.FinalPrice:C}<br/>";

            var message = new MailMessage
            {
                From = new MailAddress("mailostoreee@gmail.com"),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(email);

            using (var smtp = new SmtpClient("smtp.gmail.com", 587))
            {
                smtp.Credentials = new NetworkCredential("mailostoreee@gmail.com", "zrck gmqn cwzh bveq");
                smtp.EnableSsl = true;
                await smtp.SendMailAsync(message);
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IncreaseQuantity(int productId, int variantId)
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity?.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await _order.GetOrCreateCart(user);
            var orderProduct = cart.OrderProducts?.FirstOrDefault(op => op.ProductID == productId && op.VariantID == variantId);

            if (orderProduct != null)
            {
                var variant = await _db.ProductVariants.FirstOrDefaultAsync(v=>v.Id==variantId);
             if(variant != null){
                    if (variant.Quantity > 0 && orderProduct.Quantity < variant.Quantity)
                    {
                        if (!string.IsNullOrEmpty(cart.PromoCodeUsed))
                        {

                            double discountAmount;
                            if (cart.PromoCode != null)
                                {
                                    discountAmount = orderProduct.product.TotalPrice * (cart.PromoCode.DiscountPercentage / 100);
                            }
                            else
                            {
                                PromoCode? pc = await _db.PromoCodes.Where(pc => cart.PromoCodeUsed == pc.Code).FirstOrDefaultAsync();
                                discountAmount = orderProduct.product.TotalPrice * (pc.DiscountPercentage / 100);
                            }
                            cart.OrderPrice += orderProduct.product.TotalPrice - discountAmount;
                            cart.TotalPrice += orderProduct.product.TotalPrice - discountAmount;
                        }
                        else
                        {
                            cart.OrderPrice += orderProduct.product.TotalPrice;
                            cart.TotalPrice += orderProduct.product.TotalPrice;
                        }
                        _unitOfWork.orders.Update(cart);
                        orderProduct.Quantity += 1;
                        _unitOfWork.orderProducts.Update(orderProduct);
                        await _unitOfWork.CommitChangesAsync();
                    }
                    else if (orderProduct.Quantity > variant.Quantity)
                    {
                        if (!string.IsNullOrEmpty(cart.PromoCodeUsed))
                        {

                            double discountAmount;
                            if (cart.PromoCode != null)
                            {
                                discountAmount = orderProduct.product.TotalPrice * (cart.PromoCode.DiscountPercentage / 100);
                            }
                            else
                            {
                                PromoCode? pc = await _db.PromoCodes.Where(pc => cart.PromoCodeUsed == pc.Code).FirstOrDefaultAsync();
                                discountAmount = orderProduct.product.TotalPrice * (pc.DiscountPercentage / 100);
                            }
                            cart.OrderPrice -= (orderProduct.product.TotalPrice * (orderProduct.Quantity - variant.Quantity)) - (discountAmount * (orderProduct.Quantity - variant.Quantity));
                            cart.TotalPrice -= (orderProduct.product.TotalPrice * (orderProduct.Quantity - variant.Quantity)) - (discountAmount * (orderProduct.Quantity - variant.Quantity));
                        }
                        else
                        {
                            cart.OrderPrice -= orderProduct.product.TotalPrice * (orderProduct.Quantity - variant.Quantity);
                            cart.TotalPrice -= orderProduct.product.TotalPrice * (orderProduct.Quantity - variant.Quantity);
                        }
                        _unitOfWork.orders.Update(cart);
                        orderProduct.Quantity = variant.Quantity;
                        _unitOfWork.orderProducts.Update(orderProduct);
                    }
                }
            }

            return RedirectToAction("Index");
        }
    
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DecreaseQuantity(int productId, int variantId)
        {
            User? user = await _unitOfWork.userRepo.GetUser(User.Identity?.Name);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var cart = await _order.GetOrCreateCart(user);
            var orderProduct = cart.OrderProducts?.FirstOrDefault(op => op.ProductID == productId && op.VariantID == variantId);

            if (orderProduct != null && orderProduct.Quantity > 1)
            {
                var variant = await _db.ProductVariants.FirstOrDefaultAsync(v => v.Id == variantId);
                if (variant != null)
                {
                    if (variant.Quantity >= orderProduct.Quantity)
                    {
                        if (!string.IsNullOrEmpty(cart.PromoCodeUsed))
                        {

                            double discountAmount;
                            if (cart.PromoCode != null)
                            {
                                discountAmount = orderProduct.product.TotalPrice * (cart.PromoCode.DiscountPercentage / 100);
                            }
                            else
                            {
                                PromoCode? pc = await _db.PromoCodes.Where(pc => cart.PromoCodeUsed == pc.Code).FirstOrDefaultAsync();
                                discountAmount = orderProduct.product.TotalPrice * (pc.DiscountPercentage / 100);
                            }
                            cart.OrderPrice -= orderProduct.product.TotalPrice - discountAmount;
                            cart.TotalPrice -= orderProduct.product.TotalPrice - discountAmount;
                        }
                        else
                        {
                            cart.OrderPrice -= orderProduct.product.TotalPrice;
                            cart.TotalPrice -= orderProduct.product.TotalPrice;
                        }
                        _unitOfWork.orders.Update(cart);
                        orderProduct.Quantity -= 1;
                        _unitOfWork.orderProducts.Update(orderProduct);
                        await _unitOfWork.CommitChangesAsync();
                    }
                    else if(variant.Quantity< orderProduct.Quantity)
                    {
                        if (!string.IsNullOrEmpty(cart.PromoCodeUsed))
                        {

                            double discountAmount;
                            if (cart.PromoCode != null)
                            {
                                discountAmount = orderProduct.product.TotalPrice * (cart.PromoCode.DiscountPercentage / 100);
                            }
                            else
                            {
                                PromoCode? pc = await _db.PromoCodes.Where(pc => cart.PromoCodeUsed == pc.Code).FirstOrDefaultAsync();
                                discountAmount = orderProduct.product.TotalPrice * (pc.DiscountPercentage / 100);
                            }
                            cart.OrderPrice -= (orderProduct.product.TotalPrice * (orderProduct.Quantity - variant.Quantity)) - (discountAmount * (orderProduct.Quantity - variant.Quantity));
                            cart.TotalPrice -= (orderProduct.product.TotalPrice * (orderProduct.Quantity - variant.Quantity)) - (discountAmount * (orderProduct.Quantity - variant.Quantity));
                        }
                        else
                        {
                            cart.OrderPrice -= orderProduct.product.TotalPrice * (orderProduct.Quantity - variant.Quantity);
                            cart.TotalPrice -= orderProduct.product.TotalPrice * (orderProduct.Quantity - variant.Quantity);
                        }
                        _unitOfWork.orders.Update(cart);
                        orderProduct.Quantity = variant.Quantity;
                        _unitOfWork.orderProducts.Update(orderProduct);
                    }
                    await _unitOfWork.CommitChangesAsync();
                }
            }

            return RedirectToAction("Index");
        }
    }
}