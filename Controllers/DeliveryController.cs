using Mailo.Data;
using Mailo.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Mailo.Controllers
{
    public class DeliveryController : Controller
    {
        private readonly AppDbContext _context;

        public DeliveryController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var deliveries = _context.deliveries.ToList();
            return View(deliveries);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(Delivery delivery)
        {
            if (ModelState.IsValid)
            {
                var existingDelivery = _context.deliveries.FirstOrDefault(c => c.Name == delivery.Name);
                if (existingDelivery != null)
                {
                    ModelState.AddModelError("Name", "Delivery with this name already exists.");
                    return View(delivery);
                }

                _context.deliveries.Add(delivery);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(delivery);
        }

        public IActionResult Edit(int id)
        {
            var delivery = _context.deliveries.Find(id);
            if (delivery == null)
                return NotFound();

            return View(delivery);
        }

        [HttpPost]
        public IActionResult Edit(Delivery delivery)
        {
            if (ModelState.IsValid)
            {
                var existingDelivery = _context.deliveries.FirstOrDefault(c => c.Name == delivery.Name && c.Id != delivery.Id);
                if (existingDelivery != null)
                {
                    ModelState.AddModelError("Name", "Delivery with this name already exists.");
                    return View(delivery);
                }

                _context.deliveries.Update(delivery);
                _context.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(delivery);
        }

     
    }
}