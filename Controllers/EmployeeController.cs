﻿using System.Data;
using Mailo.Data;
using Mailo.Data.Enums;
using Mailo.IRepo;
using Mailo.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Mailo.Controllers
{
    public class EmployeeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly AppDbContext _db;
        public EmployeeController(IUnitOfWork unitOfWork, AppDbContext db)
        {
            _db = db;
            _unitOfWork = unitOfWork;
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            return View(await _unitOfWork.employees.GetAll());
        }
        [Authorize(Roles = "Admin")]
        public IActionResult New()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult New(Employee employee)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var mailAddress = new System.Net.Mail.MailAddress(employee.Email);
                }
                catch (FormatException)
                {
                    ModelState.AddModelError("", "The email address format is invalid.");
                    return View(employee);
                }

                // التحقق إذا كان البريد الإلكتروني موجودًا بالفعل
                if (_db.Users.Any(u => u.Email == employee.Email))
                {
                    ModelState.AddModelError("", "The email is already in use.");
                    return View(employee);
                }

                // التحقق إذا كان رقم الهاتف موجودًا بالفعل
                if (_db.Users.Any(u => u.PhoneNumber == employee.PhoneNumber))
                {
                    ModelState.AddModelError("", "The phone number is already in use.");
                    return View(employee);
                }
                _unitOfWork.employees.Insert(employee);
                TempData["Success"] = "Employee Has Been Added Successfully";
                return RedirectToAction("Index");
            }
            return View(employee);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            if (id != 0)
            {
                return View(await _unitOfWork.employees.GetByID(id));
            }
            return NotFound();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(Employee employee)
        {
            if (ModelState.IsValid)
            {
                _unitOfWork.employees.Update(employee);
                TempData["Success"] = "Employee Has Been Updated Successfully";
                return RedirectToAction("Index");
            }
            return View(employee);
        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id = 0)
        {
            if (id != 0)
            {
                return View(await _unitOfWork.employees.GetByID(id));
            }
            return NotFound();
        }
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(Employee employee)
        {

            if (employee != null)
            {
                var orders = await _db.Orders.Where(o => o.EmpID == employee.ID).ToListAsync();
                if(orders!=null){
                    foreach (Order order in orders)
                    {
                        order.EmpID = null;
                        _unitOfWork.orders.Update(order);
                    }
                }
                _unitOfWork.employees.Delete(employee);
                TempData["Success"] = "Employee Has Been Deleted Successfully";
                return RedirectToAction("Index");
            }
            return NotFound();

        }
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ViewOrdersAdmin()
        {
            var orders = await _db.Orders
                .Include(o => o.user)
                .Include(o => o.employee)
                .Include(o => o.Payment)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.product)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Variant)
                        .ThenInclude(v => v.Size)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Variant)
                        .ThenInclude(v => v.Color).ToListAsync();
            //   _unitOfWork.orders.GetAllWithIncludes(
            //   order=>order.employee,
            //   order=>order.OrderProducts,
            //   order=>order.Payment,
            //   order=>order.user
            //);
            if (orders == null || orders.Any(o => o == null))
            {
                TempData["ErrorMessage"] = "Orders list is null";
                return View();
            }

            // Fetch users separately and assign to each order
            foreach (var order in orders)
            {
                if (order.UserID != 0)
                {
                    order.user = await _unitOfWork.users.GetByIDWithIncludes(order.UserID); // Fetch and assign the user manually
                }
            }
            var available = orders
                .Where(o => o != null && (o.OrderStatus == OrderStatus.Pending || o.OrderStatus == OrderStatus.Shipped))
                .ToList();
            if (available == null || !available.Any()) // Check if available orders are found
            {
                TempData["ErrorMessage"] = "No available orders";
                return View("ViewOrders");
            }
            return View("ViewOrders", available);

        }

        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> ViewOrders()
        {
            var orders = await _db.Orders
                .Include(o => o.user)
                .Include(o => o.employee)
                .Include(o => o.Payment)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.product)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Variant)
                        .ThenInclude(v => v.Size)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Variant)
                        .ThenInclude(v => v.Color).ToListAsync();
             //   _unitOfWork.orders.GetAllWithIncludes(
             //   order=>order.employee,
             //   order=>order.OrderProducts,
             //   order=>order.Payment,
             //   order=>order.user
             //);
            if (orders == null || orders.Any(o => o == null))
            {
                TempData["ErrorMessage"] = "Orders list is null";
                return View();
            }

            // Fetch users separately and assign to each order
            foreach (var order in orders)
            {
                if (order.UserID != 0)
                {
                    order.user = await _unitOfWork.users.GetByIDWithIncludes(order.UserID); // Fetch and assign the user manually
                }
            }
            var available = orders
                .Where(o =>o.OrderStatus != OrderStatus.New && o.OrderStatus!=OrderStatus.Cancelled && o.EmpID == null)
                .ToList();
            if (available == null || !available.Any()) // Check if available orders are found
            {
                TempData["ErrorMessage"] = "No available orders";
                return View();
            }
            return View(available);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> AcceptOrder(Order order)
        {
            Employee? employee = await _db.Employees.Include(e => e.orders).FirstOrDefaultAsync(x => x.Email == User.Identity.Name);
            if (employee == null) return Unauthorized();
            order.EmpID = employee.ID;
            _unitOfWork.orders.Update(order);

            TempData["Success"] = "Order Has Been Accepted Successfully";
            return RedirectToAction("ViewRequiredOrders");
        }
        public async Task<IActionResult> ViewRequiredOrders()
        {
            Employee? employee = await _db.Employees.Include(e => e.orders).FirstOrDefaultAsync(x => x.Email == User.Identity.Name);
            if (employee == null) return Unauthorized();
            var orders = await _db.Orders
                .Include(o => o.user)
                .Include(o => o.employee)
                .Include(o => o.Payment)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.product)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Variant)
                        .ThenInclude(v => v.Size)
                .Include(o => o.OrderProducts)
                    .ThenInclude(op => op.Variant)
                        .ThenInclude(v => v.Color).ToListAsync();
            if (orders == null || orders.Any(o => o == null))
            {
                TempData["ErrorMessage"] = "Orders list is null";
                return BadRequest(TempData["ErrorMessage"]);
            }
            foreach (var order in orders)
            {
                if (order.UserID != 0)
                {
                    order.user = await _unitOfWork.users.GetByID(order.UserID);
                }
            }
            var available = orders.Where(o => o.EmpID == employee.ID).ToList();
            if (available == null || !available.Any())
            {
                TempData["ErrorMessage"] = "No available orders";
                return View();
            }
            return View(available);

        }
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> EditOrder(int OrderId)
        {
            var order =await _unitOfWork.orders.GetByIDWithIncludes(OrderId,
                order => order.employee,
                order => order.Payment,
                order => order.user,
                order => order.OrderProducts
             );
            return View(order);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Employee")]
        public async Task<IActionResult> EditOrder(int OrderId ,OrderStatus os,PaymentStatus ps)
        {
            var order = await _unitOfWork.orders.GetByIDWithIncludes(OrderId,
               order => order.employee,
               order => order.Payment,
               order => order.user,
               order => order.OrderProducts
            );
            if (ModelState.IsValid)
            {
                order.OrderStatus = os;
                if (os == OrderStatus.Delivered)
                {
                    order.Payment.PaymentStatus = PaymentStatus.Paid;
                }
                else
                {
                    order.Payment.PaymentStatus = ps;
                }
                _unitOfWork.orders.Update(order);
                TempData["Success"] = "Order Has Been Updated Successfully";
                return RedirectToAction("ViewRequiredOrders");

            }
            return View(order);
        }
    }
}
