using Mailo.Data;
using Mailo.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace Mailo.Controllers
{
    public class PromoCodeController : Controller
    {
        private readonly AppDbContext _context;

        public PromoCodeController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var promoCodes = _context.PromoCodes.ToList();
            return View(promoCodes);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Create(PromoCode promoCode)
        {
            if (_context.PromoCodes.Any(p => p.Code == promoCode.Code))
            {
                ModelState.AddModelError("Code", "This promo code already exists.");
            }

            if (ModelState.IsValid)
            {
                _context.PromoCodes.Add(promoCode);
                _context.SaveChanges();
                TempData["Success"] = "Promo code created successfully!";
                return RedirectToAction("Index");
            }

            return View(promoCode);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var promoCode = _context.PromoCodes.Find(id);
            if (promoCode == null)
            {
                TempData["Error"] = "Promo code not found!";
                return RedirectToAction("Index");
            }

            return View(promoCode);
        }

        [HttpPost]
        public IActionResult Edit(PromoCode promoCode)
        {
            if (_context.PromoCodes.Any(p => p.Code == promoCode.Code && p.Id != promoCode.Id))
            {
                ModelState.AddModelError("Code", "This promo code already exists.");
            }

            if (ModelState.IsValid)
            {
                _context.PromoCodes.Update(promoCode);
                _context.SaveChanges();
                TempData["Success"] = "Promo code updated successfully!";
                return RedirectToAction("Index");
            }

            return View(promoCode);
        }
        [HttpGet]

        public IActionResult Delete(int id)
        {
            var promoCode = _context.PromoCodes.Find(id);
            if (promoCode == null)
            {
                return NotFound();
            }

            return View(promoCode); // Pass the PromoCode to the Delete view
        }

        // POST: PromoCode/DeleteConfirmed/5
        [HttpPost, ActionName("Delete")]
        public IActionResult DeleteConfirmed(int id)
        {
            var promoCode = _context.PromoCodes.Find(id);
            if (promoCode != null)
            {
                _context.PromoCodes.Remove(promoCode);
                _context.SaveChanges();
                TempData["Success"] = "Promo code deleted successfully!";
            }
            else
            {
                TempData["Error"] = "Promo code not found!";
            }

            return RedirectToAction("Index");
        }

    }
}
